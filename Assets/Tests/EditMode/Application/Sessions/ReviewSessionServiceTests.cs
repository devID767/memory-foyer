using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Application.Sessions;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Domain.Time;

namespace MemoryFoyer.Tests.EditMode.Application.Sessions
{
    [TestFixture]
    public sealed class ReviewSessionServiceTests
    {
        private static readonly DateTime Now =
            new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        private static readonly DeckId Deck = new("capitals");

        // ----- Test cases ---------------------------------------------------

        [Test]
        public void StartAsync_FromIdle_TransitionsToPlayingAndPublishesSessionStartedEvent()
        {
            Fixture f = Build(cardCount: 3);

            f.Service.StartAsync(Deck).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Playing));
            Assert.That(f.Service.Total, Is.EqualTo(3));
            Assert.That(f.Service.Remaining, Is.EqualTo(3));
            Assert.That(f.SessionStarted.Published.Count, Is.EqualTo(1));
            Assert.That(f.SessionStarted.Published[0].DeckId, Is.EqualTo(Deck));
            Assert.That(f.SessionStarted.Published[0].CardCount, Is.EqualTo(3));
            Assert.That(f.SessionStarted.Published[0].StartedAt, Is.EqualTo(Now));
            Assert.That(f.Analytics.SessionsStarted.Count, Is.EqualTo(1));
        }

        [Test]
        public void StartAsync_WhenAlreadyStarted_ThrowsInvalidOperationException()
        {
            Fixture f = Build(cardCount: 3);
            f.Service.StartAsync(Deck).GetAwaiter().GetResult();

            Assert.Throws<InvalidOperationException>(() =>
                f.Service.StartAsync(Deck).GetAwaiter().GetResult());
        }

        [Test]
        public void GradeAsync_OnNonEmptyQueue_PublishesCardReviewedEvent_AndAdvances()
        {
            Fixture f = Build(cardCount: 3);
            f.Service.StartAsync(Deck).GetAwaiter().GetResult();
            CardId firstId = f.Service.CurrentCard!.CardId;

            f.Service.GradeAsync(ReviewGrade.Good).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Playing));
            Assert.That(f.Service.Remaining, Is.EqualTo(2));
            Assert.That(f.Service.CurrentCard!.CardId, Is.Not.EqualTo(firstId));
            Assert.That(f.CardReviewed.Published.Count, Is.EqualTo(1));
            Assert.That(f.CardReviewed.Published[0].CardId, Is.EqualTo(firstId));
            Assert.That(f.CardReviewed.Published[0].Grade, Is.EqualTo(ReviewGrade.Good));
        }

        [Test]
        public void GradeAsync_Again_RequeuesCardAtPhysicalEndOfQueue()
        {
            Fixture f = Build(cardCount: 2);
            f.Service.StartAsync(Deck).GetAwaiter().GetResult();
            CardId firstId = f.Service.CurrentCard!.CardId;
            CardId secondId = new("c2");

            f.Service.GradeAsync(ReviewGrade.Again).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Playing));
            Assert.That(f.Service.Remaining, Is.EqualTo(2));
            Assert.That(f.Service.CurrentCard!.CardId, Is.EqualTo(secondId));

            f.Service.GradeAsync(ReviewGrade.Good).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Playing));
            Assert.That(f.Service.Remaining, Is.EqualTo(1));
            Assert.That(f.Service.CurrentCard!.CardId, Is.EqualTo(firstId));
        }

        [Test]
        public void GradeAsync_OnLastCard_TransitionsToUploading_ThenIdle_AndPublishesSessionFinishedTrue()
        {
            Fixture f = Build(cardCount: 1);
            f.Service.StartAsync(Deck).GetAwaiter().GetResult();

            f.Service.GradeAsync(ReviewGrade.Good).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Idle));
            Assert.That(f.Service.Remaining, Is.EqualTo(0));
            Assert.That(f.Store.UploadedResults.Count, Is.EqualTo(1));
            Assert.That(f.SessionFinished.Published.Count, Is.EqualTo(1));
            Assert.That(f.SessionFinished.Published[0].UploadedSuccessfully, Is.True);
            Assert.That(f.SessionFinished.Published[0].ReviewedCount, Is.EqualTo(1));
            Assert.That(f.Analytics.SessionsFinished.Count, Is.EqualTo(1));
        }

        [Test]
        public void GradeAsync_UploadFailure_TransitionsToError_AndPublishesSessionFinishedFalse()
        {
            Fixture f = Build(cardCount: 1);
            f.Store.ThrowOnUpload = true;
            f.Service.StartAsync(Deck).GetAwaiter().GetResult();

            f.Service.GradeAsync(ReviewGrade.Good).GetAwaiter().GetResult();

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Error));
            Assert.That(f.SessionFinished.Published.Count, Is.EqualTo(1));
            Assert.That(f.SessionFinished.Published[0].UploadedSuccessfully, Is.False);
            Assert.That(f.SessionFinished.Published[0].ReviewedCount, Is.EqualTo(1));
            Assert.That(f.Analytics.SessionsFinished.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartAsync_ScheduleStoreFailureWithEmptyCache_TransitionsToError()
        {
            Fixture f = Build(cardCount: 3);
            f.Store.ThrowOnGet = true;

            Assert.Throws<ScheduleStoreUnavailableException>(() =>
                f.Service.StartAsync(Deck).GetAwaiter().GetResult());

            Assert.That(f.Service.State, Is.EqualTo(SessionState.Error));
            Assert.That(f.SessionStarted.Published.Count, Is.EqualTo(0));
        }

        // ----- Fixture builder ---------------------------------------------

        private static Fixture Build(int cardCount)
        {
            List<Card> cards = new();
            List<CardSchedule> schedule = new();
            for (int i = 1; i <= cardCount; i++)
            {
                CardId id = new($"c{i}");
                cards.Add(new Card(id, $"Front {i}", $"Back {i}"));
                Sm2State state = new(
                    Repetitions: 0,
                    EaseFactor: 2.5,
                    IntervalDays: 0,
                    // Earlier-indexed cards are MORE overdue, so the queue order matches insertion.
                    DueAt: Now.AddMinutes(-(cardCount + 1 - i)),
                    Stage: LearningStage.New,
                    LearningStepIndex: 0);
                schedule.Add(new CardSchedule(id, state));
            }

            Deck deck = new(Deck, "Capitals", "", 10, cards);
            DeckSchedule deckSchedule = new(Deck, schedule, Now, ScheduleSource.Server);

            FakeDeckRepository repo = new(deck);
            FakeScheduleStore store = new(deckSchedule);
            FakeClock clock = new(Now);
            FakeAnalytics analytics = new();
            FakePublisher<SessionStartedEvent> started = new();
            FakePublisher<CardReviewedEvent> reviewed = new();
            FakePublisher<SessionFinishedEvent> finished = new();

            ReviewSessionService service = new(
                repo, store, clock, analytics, started, reviewed, finished);

            return new Fixture(service, store, analytics, started, reviewed, finished);
        }

        private sealed record Fixture(
            ReviewSessionService Service,
            FakeScheduleStore Store,
            FakeAnalytics Analytics,
            FakePublisher<SessionStartedEvent> SessionStarted,
            FakePublisher<CardReviewedEvent> CardReviewed,
            FakePublisher<SessionFinishedEvent> SessionFinished);

        // ----- Fakes --------------------------------------------------------

        private sealed class FakeDeckRepository : IDeckRepository
        {
            private readonly Deck _deck;
            public FakeDeckRepository(Deck deck) { _deck = deck; }

            public UniTask<Deck> GetDeckAsync(DeckId deckId, CancellationToken ct = default)
                => UniTask.FromResult(_deck);

            public UniTask<IReadOnlyList<Deck>> GetAllAsync(CancellationToken ct = default)
                => UniTask.FromResult<IReadOnlyList<Deck>>(new List<Deck> { _deck });
        }

        private sealed class FakeScheduleStore : IScheduleStore
        {
            private readonly DeckSchedule _schedule;
            public bool ThrowOnGet;
            public bool ThrowOnUpload;
            public List<SessionResult> UploadedResults { get; } = new();

            public FakeScheduleStore(DeckSchedule schedule) { _schedule = schedule; }

            public UniTask<DeckSchedule> GetDeckScheduleAsync(DeckId deckId, CancellationToken ct = default)
            {
                if (ThrowOnGet)
                {
                    throw new ScheduleStoreUnavailableException("get failed");
                }
                return UniTask.FromResult(_schedule);
            }

            public UniTask<DeckSchedule> UploadSessionAsync(SessionResult result, CancellationToken ct = default)
            {
                if (ThrowOnUpload)
                {
                    throw new ScheduleStoreUnavailableException("upload failed");
                }
                UploadedResults.Add(result);
                return UniTask.FromResult(_schedule);
            }

            public UniTask<bool> IsServerReachableAsync(CancellationToken ct = default)
                => UniTask.FromResult(!ThrowOnGet);

            public UniTask DrainPendingAsync(CancellationToken ct = default) => UniTask.CompletedTask;
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTime utcNow) { UtcNow = utcNow; }
            public DateTime UtcNow { get; set; }
        }

        private sealed class FakeAnalytics : IAnalyticsService
        {
            public List<(Guid sessionId, DeckId deckId, int cardCount)> SessionsStarted { get; } = new();
            public List<(Guid sessionId, int reviewedCount, TimeSpan duration)> SessionsFinished { get; } = new();
            public List<string> OfflineFallbacks { get; } = new();

            public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount)
                => SessionsStarted.Add((sessionId, deckId, cardCount));

            public void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration)
                => SessionsFinished.Add((sessionId, reviewedCount, duration));

            public void TrackOfflineFallback(string operation)
                => OfflineFallbacks.Add(operation);
        }

        private sealed class FakePublisher<T> : IPublisher<T>
        {
            public List<T> Published { get; } = new();
            public void Publish(T message) => Published.Add(message);
        }
    }
}
