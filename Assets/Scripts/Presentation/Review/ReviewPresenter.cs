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
        private readonly ErrorBannerView _errorBannerView;
        private readonly IReviewInputSource _input;

        private const float ErrorAutoReturnSeconds = 2f;

        // Guards re-entry while a card animation (RevealBackAsync / AdvanceToNextCardAsync)
        // is in flight — prevents spammed input from starting overlapping async chains.
        private bool _busy;
        private bool _revealed;
        private CancellationToken _lifetimeCt;

        public ReviewPresenter(
            IReviewSessionService session,
            IDeckRepository deckRepository,
            ISubscriber<DeckSelectedEvent> deckSelectedSubscriber,
            ISubscriber<SessionReviewedEvent> sessionReviewedSubscriber,
            IPublisher<BackToFoyerRequested> backToFoyerPublisher,
            ReviewScreen screen,
            ErrorBannerView errorBannerView,
            IReviewInputSource input)
        {
            _session = session;
            _deckRepository = deckRepository;
            _deckSelectedSubscriber = deckSelectedSubscriber;
            _sessionReviewedSubscriber = sessionReviewedSubscriber;
            _backToFoyerPublisher = backToFoyerPublisher;
            _screen = screen;
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

            try
            {
                Deck deck = await _deckRepository.GetDeckAsync(deckId, ct);
                _screen.SetDeckName(deck.DisplayName);
                _screen.Show(); // Show resets stale summary/grade visibility (fix #3).
                await _session.StartAsync(deckId, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (DeckNotFoundException ex)
            {
                Debug.LogException(ex);
                _backToFoyerPublisher.Publish(new BackToFoyerRequested());
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _backToFoyerPublisher.Publish(new BackToFoyerRequested());
                return;
            }

            ReviewCard? current = _session.CurrentCard;
            if (_session.State == SessionState.Playing && current is not null)
            {
                _screen.SetProgress(1, _session.Total);
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
                // Progress numerator is completed grades + 1 (fix #1). May exceed Total
                // denominator on ReviewGrade.Again — cosmetic; denominator is initial queue size.
                _screen.SetProgress(_session.ReviewsCompleted + 1, _session.Total);

                try
                {
                    _busy = true;
                    await _screen.AdvanceToNextCardAsync(new FrontFaceData(next.Front), ct);
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
            // If session is no longer Playing, GradeAsync transitioned to Reviewed and published
            // SessionReviewedEvent synchronously; OnSessionReviewed already called ShowSummary.
        }

        private void OnSessionReviewed(SessionReviewedEvent evt)
        {
            _screen.ShowSummary(evt.ReviewedCount);
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
            // EndAsync transitioned to Reviewed and published SessionReviewedEvent;
            // OnSessionReviewed showed the Summary. User dismisses to commit.
        }

        private async UniTask CommitAndExitAsync(CancellationToken ct)
        {
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
