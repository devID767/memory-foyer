using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Domain.Time;

namespace MemoryFoyer.Application.Sessions
{
    public sealed class ReviewSessionService : IReviewSessionService
    {
        private readonly IDeckRepository _deckRepository;
        private readonly IScheduleStore _scheduleStore;
        private readonly IClock _clock;
        private readonly IAnalyticsService _analytics;
        private readonly IPublisher<SessionStartedEvent> _sessionStartedPublisher;
        private readonly IPublisher<CardReviewedEvent> _cardReviewedPublisher;
        private readonly IPublisher<SessionReviewedEvent> _sessionReviewedPublisher;
        private readonly IPublisher<SessionUploadCompletedEvent> _sessionUploadCompletedPublisher;

        private readonly List<QueueEntry> _queue = new();
        private List<CardReview> _reviews = new();
        private SessionState _state = SessionState.Idle;
        private Guid _sessionId;
        private DeckId _deckId;
        private DateTime _startedAt;
        private int _total;
        private SessionResult? _pendingResult;

        public ReviewSessionService(
            IDeckRepository deckRepository,
            IScheduleStore scheduleStore,
            IClock clock,
            IAnalyticsService analytics,
            IPublisher<SessionStartedEvent> sessionStartedPublisher,
            IPublisher<CardReviewedEvent> cardReviewedPublisher,
            IPublisher<SessionReviewedEvent> sessionReviewedPublisher,
            IPublisher<SessionUploadCompletedEvent> sessionUploadCompletedPublisher)
        {
            _deckRepository = deckRepository ?? throw new ArgumentNullException(nameof(deckRepository));
            _scheduleStore = scheduleStore ?? throw new ArgumentNullException(nameof(scheduleStore));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _sessionStartedPublisher = sessionStartedPublisher ?? throw new ArgumentNullException(nameof(sessionStartedPublisher));
            _cardReviewedPublisher = cardReviewedPublisher ?? throw new ArgumentNullException(nameof(cardReviewedPublisher));
            _sessionReviewedPublisher = sessionReviewedPublisher ?? throw new ArgumentNullException(nameof(sessionReviewedPublisher));
            _sessionUploadCompletedPublisher = sessionUploadCompletedPublisher ?? throw new ArgumentNullException(nameof(sessionUploadCompletedPublisher));
        }

        public SessionState State => _state;
        public int Total => _total;
        public int Remaining => _state == SessionState.Playing ? _queue.Count : 0;
        public int Position => _state == SessionState.Playing && _queue.Count > 0
            ? _total - _queue.Count + 1
            : 0;

        public ReviewCard? CurrentCard
        {
            get
            {
                if (_state != SessionState.Playing || _queue.Count == 0)
                {
                    return null;
                }

                QueueEntry head = _queue[0];
                return new ReviewCard(head.CardId, head.Front, head.Back, head.State);
            }
        }

        public async UniTask StartAsync(DeckId deckId, CancellationToken ct = default)
        {
            // Per architecture.md: reentrancy is guarded — only Idle can start. Error is
            // documented as "terminal until StartAsync is called again", so we permit the
            // Error → Idle reset here as the explicit recovery path.
            if (_state == SessionState.Error)
            {
                _state = SessionState.Idle;
            }

            if (_state != SessionState.Idle)
            {
                throw new InvalidOperationException(
                    $"Cannot start session: state is {_state}. Expected Idle.");
            }

            _state = SessionState.Loading;
            _deckId = deckId;

            try
            {
                Deck deck = await _deckRepository.GetDeckAsync(deckId, ct);
                DeckSchedule schedule = await _scheduleStore.GetDeckScheduleAsync(deckId, ct);

                Dictionary<CardId, Sm2State> stateByCard = schedule.Cards
                    .ToDictionary(card => card.CardId, card => card.State);

                DateTime now = _clock.UtcNow;

                // Cards absent from the schedule are skipped: per GDD §5 the server is the
                // authority on which cards are due (it applies the per-day new-card cap and
                // returns the released schedule for the deck — released-new + non-new only).
                IEnumerable<QueueEntry> due = deck.Cards
                    .Where(card => stateByCard.TryGetValue(card.Id, out Sm2State s) && s.DueAt <= now)
                    .Select(card => new QueueEntry(card.Id, card.Front, card.Back, stateByCard[card.Id]))
                    .OrderBy(entry => entry.State.DueAt)
                    .ThenBy(entry => entry.CardId.Value, StringComparer.Ordinal);

                _queue.Clear();
                _queue.AddRange(due);

                _reviews = new List<CardReview>();
                _sessionId = Guid.NewGuid();
                _startedAt = now;
                _total = _queue.Count;
                _state = SessionState.Playing;
            }
            catch
            {
                _state = SessionState.Error;
                throw;
            }

            _sessionStartedPublisher.Publish(new SessionStartedEvent(_sessionId, _deckId, _total, _startedAt));
            _analytics.TrackSessionStarted(_sessionId, _deckId, _total);
        }

        public void RevealCurrent()
        {
            if (_state != SessionState.Playing)
            {
                throw new InvalidOperationException(
                    $"Cannot reveal: state is {_state}. Expected Playing.");
            }
            // Front-to-back is purely a view concern; this method exists to satisfy the
            // contract and reserve a hook for future analytics on time-to-reveal.
        }

        public async UniTask GradeAsync(ReviewGrade grade, CancellationToken ct = default)
        {
            if (_state != SessionState.Playing)
            {
                throw new InvalidOperationException(
                    $"Cannot grade: state is {_state}. Expected Playing.");
            }

            if (_queue.Count == 0)
            {
                throw new InvalidOperationException("Cannot grade: queue is empty.");
            }

            QueueEntry head = _queue[0];
            DateTime now = _clock.UtcNow;
            Sm2State newState = Sm2Algorithm.Schedule(head.State, grade, now);

            _reviews.Add(new CardReview(head.CardId, grade, now));
            _cardReviewedPublisher.Publish(new CardReviewedEvent(_sessionId, head.CardId, grade, newState.DueAt));

            _queue.RemoveAt(0);
            if (grade == ReviewGrade.Again)
            {
                _queue.Add(head with { State = newState });
            }

            if (_queue.Count == 0)
            {
                await FinishReviewing(ct);
            }
        }

        public async UniTask EndAsync(CancellationToken ct = default)
        {
            if (_state != SessionState.Playing)
            {
                throw new InvalidOperationException(
                    $"Cannot end: state is {_state}. Expected Playing.");
            }

            await FinishReviewing(ct);
        }

        public async UniTask CommitAsync(CancellationToken ct = default)
        {
            if (_state != SessionState.Reviewed)
            {
                throw new InvalidOperationException(
                    $"Cannot commit: state is {_state}. Expected Reviewed.");
            }
            if (_pendingResult is null)
            {
                throw new InvalidOperationException("Cannot commit: no pending result.");
            }

            SessionResult result = _pendingResult;
            Guid sessionId = result.SessionId;
            DeckId deckId = result.DeckId;
            int reviewedCount = result.Reviews.Count;
            DateTime startedAt = _startedAt;

            _state = SessionState.Uploading;

            try
            {
                await _scheduleStore.UploadSessionAsync(result, ct);
            }
            catch (ScheduleStoreUnavailableException)
            {
                // Pending session is already on disk (FinishReviewing wrote it via
                // EnqueuePendingAsync). Surface the failure via the event so the foyer can
                // reflect it; the next-launch drain replays the pending entry.
                _state = SessionState.Error;
                _sessionUploadCompletedPublisher.Publish(new SessionUploadCompletedEvent(sessionId, deckId, false));
                return;
            }
            catch
            {
                _state = SessionState.Error;
                throw;
            }

            // Reassign rather than Clear: the just-uploaded SessionResult holds a reference
            // to the old list, so mutating it would alter what the inner store received.
            _queue.Clear();
            _reviews = new List<CardReview>();
            _total = 0;
            _pendingResult = null;
            _state = SessionState.Idle;

            _sessionUploadCompletedPublisher.Publish(new SessionUploadCompletedEvent(sessionId, deckId, true));
            _analytics.TrackSessionFinished(sessionId, reviewedCount, _clock.UtcNow - startedAt);
        }

        private async UniTask FinishReviewing(CancellationToken ct)
        {
            // Snapshot reviews into a SessionResult held by the service; CommitAsync consumes
            // it later. The pending-cache write happens here so a crash between Summary-shown
            // and dismiss still has the data on disk for the next-launch drain.
            SessionResult result = new(_sessionId, _deckId, _reviews);
            _pendingResult = result;

            try
            {
                await _scheduleStore.EnqueuePendingAsync(result, ct);
            }
            catch
            {
                _pendingResult = null;
                _state = SessionState.Error;
                throw;
            }

            _state = SessionState.Reviewed;
            _sessionReviewedPublisher.Publish(new SessionReviewedEvent(_sessionId, _deckId, _reviews.Count));
        }

        private sealed record QueueEntry(CardId CardId, string Front, string Back, Sm2State State);
    }
}
