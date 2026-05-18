using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Application.Sessions;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Presentation.Banners;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class ReviewPresenter : IAsyncStartable
    {
        private readonly IReviewSessionService _session;
        private readonly IDeckRepository _deckRepository;
        private readonly ISubscriber<DeckSelectedEvent> _deckSelectedSubscriber;
        private readonly ISubscriber<SessionReviewedEvent> _sessionReviewedSubscriber;
        private readonly IPublisher<BackToFoyerRequested> _backToFoyerPublisher;
        private readonly ReviewScreen _screen;
        private readonly LoadingView _loadingView;
        private readonly ErrorBannerView _errorBannerView;
        private readonly IReviewInputSource _input;

        private const float ErrorAutoReturnSeconds = 2f;

        // Guards re-entry while a card animation (RevealBackAsync / AdvanceToNextCardAsync)
        // is in flight — prevents spammed input from starting overlapping async chains.
        private bool _busy;
        private bool _revealed;
        private int _pendingReviewedCount;
        private CancellationToken _lifetimeCt;

        public ReviewPresenter(
            IReviewSessionService session,
            IDeckRepository deckRepository,
            ISubscriber<DeckSelectedEvent> deckSelectedSubscriber,
            ISubscriber<SessionReviewedEvent> sessionReviewedSubscriber,
            IPublisher<BackToFoyerRequested> backToFoyerPublisher,
            ReviewScreen screen,
            LoadingView loadingView,
            ErrorBannerView errorBannerView,
            IReviewInputSource input)
        {
            _session = session;
            _deckRepository = deckRepository;
            _deckSelectedSubscriber = deckSelectedSubscriber;
            _sessionReviewedSubscriber = sessionReviewedSubscriber;
            _backToFoyerPublisher = backToFoyerPublisher;
            _screen = screen;
            _loadingView = loadingView;
            _errorBannerView = errorBannerView;
            _input = input;
        }

        public UniTask StartAsync(CancellationToken cancellation)
        {
            _lifetimeCt = cancellation;

            IDisposable deckSelectedSub = _deckSelectedSubscriber.Subscribe(
                e => OnDeckSelected(e.DeckId));
            IDisposable sessionReviewedSub = _sessionReviewedSubscriber.Subscribe(
                e => OnSessionReviewed(e));

            _screen.RevealRequested += OnRevealRequested;
            _screen.GradeSubmitted += OnGradeSubmitted;
            _screen.ReturnRequested += OnReturnRequested;
            _input.RevealPressed += OnRevealRequested;
            _input.GradePressed += OnGradeSubmitted;
            _input.ClosePressed += OnReturnRequested;

            cancellation.Register(() =>
            {
                _screen.RevealRequested -= OnRevealRequested;
                _screen.GradeSubmitted -= OnGradeSubmitted;
                _screen.ReturnRequested -= OnReturnRequested;
                _input.RevealPressed -= OnRevealRequested;
                _input.GradePressed -= OnGradeSubmitted;
                _input.ClosePressed -= OnReturnRequested;
                deckSelectedSub.Dispose();
                sessionReviewedSub.Dispose();
            });

            // Review screen starts hidden; FoyerPresenter owns the initial canvas state.
            return UniTask.CompletedTask;
        }

        private void OnDeckSelected(DeckId deckId)
        {
            RunOnDeckSelectedAsync(deckId).Forget();
        }

        private async UniTaskVoid RunOnDeckSelectedAsync(DeckId deckId)
        {
            if (_session.State != SessionState.Idle && _session.State != SessionState.Error)
            {
                return;
            }

            if (_busy)
            {
                return;
            }

            CancellationToken ct = _lifetimeCt;

            // Review canvas is gated behind the loading view: _screen.Show is wired as the
            // on-hidden callback so the canvas only reveals after the session is ready.
            _loadingView.Show(_screen.Show);

            try
            {
                Deck deck = await _deckRepository.GetDeckAsync(deckId, ct);
                _screen.SetDeckName(deck.DisplayName);
                await _session.StartAsync(deckId, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (DeckNotFoundException ex)
            {
                Debug.LogException(ex);
                _loadingView.Hide(runCallback: false);
                _backToFoyerPublisher.Publish(new BackToFoyerRequested());
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _loadingView.Hide(runCallback: false);
                _backToFoyerPublisher.Publish(new BackToFoyerRequested());
                return;
            }

            _loadingView.Hide();

            ReviewCard? current = _session.CurrentCard;
            if (_session.State == SessionState.Playing && current is not null)
            {
                _screen.SetProgress(_session.Position, _session.Total);
                try
                {
                    await _screen.ShowCardAsync(new FrontFaceData(current.Front), ct);
                    _revealed = false;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            else
            {
                // Zero cards due — go straight to summary.
                _screen.ShowSummary(0);
            }
        }

        private void OnRevealRequested()
        {
            RunRevealAsync().Forget();
        }

        private async UniTaskVoid RunRevealAsync()
        {
            if (_busy || _revealed || _session.State != SessionState.Playing || _session.CurrentCard is null)
            {
                return;
            }

            ReviewCard card = _session.CurrentCard;
            _session.RevealCurrent();

            CancellationToken ct = _lifetimeCt;

            try
            {
                _busy = true;
                await _screen.RevealBackAsync(new BackFaceData(card.Front, card.Back), ct);
                _revealed = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _busy = false;
            }

            _screen.ShowGrades();
        }

        private void OnGradeSubmitted(ReviewGrade grade)
        {
            RunGradeAsync(grade).Forget();
        }

        private async UniTaskVoid RunGradeAsync(ReviewGrade grade)
        {
            if (_busy || !_revealed || _session.State != SessionState.Playing)
            {
                return;
            }

            CancellationToken ct = _lifetimeCt;

            _screen.HideGrades();

            try
            {
                await _session.GradeAsync(grade, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return;
            }

            ReviewCard? next = _session.CurrentCard;
            if (_session.State == SessionState.Playing && next is not null)
            {
                _screen.SetProgress(_session.Position, _session.Total);

                CardExitDirection exit = grade == ReviewGrade.Again
                    ? CardExitDirection.Down
                    : CardExitDirection.Right;
                try
                {
                    _busy = true;
                    await _screen.AdvanceToNextCardAsync(new FrontFaceData(next.Front), exit, ct);
                    _revealed = false;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    _busy = false;
                }
            }
            else if (_session.State == SessionState.Reviewed)
            {
                // Last card graded (cleared — Again re-queues and keeps Playing, so never here):
                // GradeAsync transitioned to Reviewed and OnSessionReviewed captured the count.
                // Flick the cleared last card off to the right, then reveal the summary.
                try
                {
                    _busy = true;
                    await _screen.DismissCardAsync(CardExitDirection.Right, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    _busy = false;
                }
                _screen.ShowSummary(_pendingReviewedCount);
                _revealed = false;
            }
        }

        private void OnSessionReviewed(SessionReviewedEvent evt)
        {
            // Published synchronously inside GradeAsync/EndAsync. Only capture the count here;
            // the async grade/end flow dismisses the card then calls ShowSummary.
            _pendingReviewedCount = evt.ReviewedCount;
        }

        private void OnReturnRequested()
        {
            RunReturnAsync().Forget();
        }

        private async UniTaskVoid RunReturnAsync()
        {
            if (_busy)
            {
                return;
            }

            CancellationToken ct = _lifetimeCt;

            switch (_session.State)
            {
                case SessionState.Playing:
                    await EndCurrentReviewAsync(ct);
                    return;
                case SessionState.Reviewed:
                    await CommitAndExitAsync(ct);
                    return;
                case SessionState.Idle:
                case SessionState.Error:
                    // Zero-cards-due summary (state stayed Idle, no Reviewed transition),
                    // or user dismissing after a previous Error left the session unrecoverable.
                    ExitToFoyer();
                    return;
                default:
                    // Loading / Uploading: ignore (race; should not normally be reachable).
                    return;
            }
        }

        private async UniTask EndCurrentReviewAsync(CancellationToken ct)
        {
            try
            {
                await _session.EndAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return;
            }

            // EndAsync transitioned to Reviewed; OnSessionReviewed captured the count.
            // Session abandoned via Esc — sink the still-visible card downward (back into the
            // deck), then reveal the summary. User dismisses to commit.
            try
            {
                _busy = true;
                await _screen.DismissCardAsync(CardExitDirection.Down, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _busy = false;
            }
            _screen.ShowSummary(_pendingReviewedCount);
        }

        private async UniTask CommitAndExitAsync(CancellationToken ct)
        {
            // Hide summary and arm the loading cover BEFORE awaiting the upload — without
            // this, the summary lingers on top of the loading view until POST completes.
            _screen.Hide();
            _loadingView.Show();

            try
            {
                _busy = true;
                await _session.CommitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ScheduleStoreContractException ex)
            {
                Debug.LogWarning($"[ReviewPresenter] schedule store rejected session (status={ex.StatusCode}): {ex.Message}");
                _loadingView.Hide(runCallback: false);
                try
                {
                    await _errorBannerView.Show("Couldn't sync now — will retry", "returning to foyer", ct);
                    await UniTask.Delay(TimeSpan.FromSeconds(ErrorAutoReturnSeconds), cancellationToken: ct);
                    await _errorBannerView.Hide(ct);
                }
                catch (OperationCanceledException) { return; }
                ExitToFoyer();
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _loadingView.Hide(runCallback: false);
                ExitToFoyer();
                return;
            }
            finally
            {
                _busy = false;
            }

            ExitToFoyer();
        }

        private void ExitToFoyer()
        {
            _screen.Hide();
            _backToFoyerPublisher.Publish(new BackToFoyerRequested());
        }
    }
}
