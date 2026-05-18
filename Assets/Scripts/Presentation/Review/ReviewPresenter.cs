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

        private ReviewUiState _state = ReviewUiState.Idle;
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

            return UniTask.CompletedTask;
        }

        private static async UniTask<bool> PlayAsync(
            Func<CancellationToken, UniTask> animation, CancellationToken ct)
        {
            try
            {
                await animation(ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private void OnDeckSelected(DeckId deckId)
        {
            RunOnDeckSelectedAsync(deckId).Forget();
        }

        private async UniTaskVoid RunOnDeckSelectedAsync(DeckId deckId)
        {
            if (_state != ReviewUiState.Idle)
            {
                return;
            }

            if (_session.State != SessionState.Idle && _session.State != SessionState.Error)
            {
                return;
            }

            _state = ReviewUiState.Opening;
            CancellationToken ct = _lifetimeCt;

            _loadingView.Show(_screen.Show);

            try
            {
                Deck deck = await _deckRepository.GetDeckAsync(deckId, ct);
                _screen.SetDeckName(deck.DisplayName);
                await _session.StartAsync(deckId, ct);
            }
            catch (OperationCanceledException)
            {
                _state = ReviewUiState.Idle;
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _loadingView.Hide(runCallback: false);
                _state = ReviewUiState.Idle;
                _backToFoyerPublisher.Publish(new BackToFoyerRequested());
                return;
            }

            _loadingView.Hide();

            ReviewCard? current = _session.CurrentCard;
            if (_session.State == SessionState.Playing && current is not null)
            {
                _screen.SetProgress(_session.Position, _session.Total);
                var firstFace = new FrontFaceData(current.Front);
                if (!await PlayAsync(c => _screen.ShowCardAsync(firstFace, c), ct))
                {
                    return;
                }
                _state = ReviewUiState.Front;
            }
            else
            {
                _screen.ShowSummary(0);
                _state = ReviewUiState.Summary;
            }
        }

        private void OnRevealRequested()
        {
            RunRevealAsync().Forget();
        }

        private async UniTaskVoid RunRevealAsync()
        {
            if (_state != ReviewUiState.Front)
            {
                return;
            }

            ReviewCard? card = _session.CurrentCard;
            if (card is null)
            {
                return;
            }

            _session.RevealCurrent();
            CancellationToken ct = _lifetimeCt;

            var backFace = new BackFaceData(card.Front, card.Back);
            _state = ReviewUiState.Revealing;
            if (!await PlayAsync(c => _screen.RevealBackAsync(backFace, c), ct))
            {
                return;
            }
            _state = ReviewUiState.Back;
            _screen.ShowGrades();
        }

        private void OnGradeSubmitted(ReviewGrade grade)
        {
            RunGradeAsync(grade).Forget();
        }

        private async UniTaskVoid RunGradeAsync(ReviewGrade grade)
        {
            if (_state != ReviewUiState.Back)
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
                _state = ReviewUiState.Back;
                return;
            }

            ReviewCard? next = _session.CurrentCard;
            if (_session.State == SessionState.Playing && next is not null)
            {
                _screen.SetProgress(_session.Position, _session.Total);

                CardExitDirection exit = grade == ReviewGrade.Again
                    ? CardExitDirection.Down
                    : CardExitDirection.Right;

                var nextFace = new FrontFaceData(next.Front);
                _state = ReviewUiState.Advancing;
                if (!await PlayAsync(c => _screen.AdvanceToNextCardAsync(nextFace, exit, c), ct))
                {
                    return;
                }
                _state = ReviewUiState.Front;
            }
            else if (_session.State == SessionState.Reviewed)
            {
                _state = ReviewUiState.Advancing;
                if (!await PlayAsync(c => _screen.DismissCardAsync(CardExitDirection.Right, c), ct))
                {
                    return;
                }
                _screen.ShowSummary(_pendingReviewedCount);
                _state = ReviewUiState.Summary;
            }
        }

        private void OnSessionReviewed(SessionReviewedEvent evt)
        {
            _pendingReviewedCount = evt.ReviewedCount;
        }

        private void OnReturnRequested()
        {
            RunReturnAsync().Forget();
        }

        private async UniTaskVoid RunReturnAsync()
        {
            switch (_state)
            {
                case ReviewUiState.Front:
                case ReviewUiState.Back:
                    await EndCurrentReviewAsync(_lifetimeCt);
                    return;
                case ReviewUiState.Summary:
                    if (_session.State == SessionState.Reviewed)
                    {
                        await CommitAndExitAsync(_lifetimeCt);
                    }
                    else
                    {
                        ExitToFoyer();
                    }
                    return;
                default:
                    return;
            }
        }

        private async UniTask EndCurrentReviewAsync(CancellationToken ct)
        {
            ReviewUiState entry = _state;

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
                _state = entry;
                return;
            }

            _state = ReviewUiState.Exiting;
            if (!await PlayAsync(c => _screen.DismissCardAsync(CardExitDirection.Down, c), ct))
            {
                return;
            }
            _screen.ShowSummary(_pendingReviewedCount);
            _state = ReviewUiState.Summary;
        }

        private async UniTask CommitAndExitAsync(CancellationToken ct)
        {
            _screen.Hide();
            _loadingView.Show();

            _state = ReviewUiState.Exiting;
            try
            {
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

            ExitToFoyer();
        }

        private void ExitToFoyer()
        {
            _state = ReviewUiState.Idle;
            _screen.Hide();
            _backToFoyerPublisher.Publish(new BackToFoyerRequested());
        }
    }
}
