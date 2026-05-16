using System;
using System.Collections.Generic;
using MessagePipe;
using NUnit.Framework;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Application.Analytics
{
    [TestFixture]
    public sealed class SessionTelemetryListenerTests
    {
        private static readonly DateTime Now =
            new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        private static readonly DeckId Deck = new("capitals");

        [Test]
        public void SessionStartedEvent_ForwardsToTrackSessionStarted()
        {
            Harness h = new();
            Guid sessionId = Guid.NewGuid();

            h.SessionStarted.Emit(new SessionStartedEvent(sessionId, Deck, 7, Now));

            Assert.That(h.Analytics.SessionsStarted.Count, Is.EqualTo(1));
            Assert.That(h.Analytics.SessionsStarted[0], Is.EqualTo((sessionId, Deck, 7)));
        }

        [Test]
        public void CardReviewedEvent_ForwardsToTrackCardReviewed()
        {
            Harness h = new();
            Guid sessionId = Guid.NewGuid();
            CardId cardId = new("c1");
            DateTime due = Now.AddDays(3);

            h.CardReviewed.Emit(new CardReviewedEvent(sessionId, cardId, ReviewGrade.Good, due));

            Assert.That(h.Analytics.CardsReviewed.Count, Is.EqualTo(1));
            Assert.That(h.Analytics.CardsReviewed[0], Is.EqualTo((sessionId, cardId, ReviewGrade.Good, due)));
        }

        [Test]
        public void SessionUploadCompletedEvent_WhenSuccess_ForwardsToTrackSessionFinished()
        {
            Harness h = new();
            Guid sessionId = Guid.NewGuid();
            TimeSpan duration = TimeSpan.FromMinutes(4);

            h.SessionUploadCompleted.Emit(
                new SessionUploadCompletedEvent(sessionId, Deck, true, 5, duration));

            Assert.That(h.Analytics.SessionsFinished.Count, Is.EqualTo(1));
            Assert.That(h.Analytics.SessionsFinished[0], Is.EqualTo((sessionId, 5, duration)));
        }

        [Test]
        public void SessionUploadCompletedEvent_WhenFailure_DoesNotTrackSessionFinished()
        {
            Harness h = new();

            h.SessionUploadCompleted.Emit(
                new SessionUploadCompletedEvent(Guid.NewGuid(), Deck, false, 5, TimeSpan.FromMinutes(4)));

            Assert.That(h.Analytics.SessionsFinished.Count, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_UnsubscribesFromAllEvents()
        {
            Harness h = new();

            h.Listener.Dispose();
            h.SessionStarted.Emit(new SessionStartedEvent(Guid.NewGuid(), Deck, 1, Now));
            h.CardReviewed.Emit(new CardReviewedEvent(Guid.NewGuid(), new CardId("c1"), ReviewGrade.Again, Now));
            h.SessionUploadCompleted.Emit(
                new SessionUploadCompletedEvent(Guid.NewGuid(), Deck, true, 1, TimeSpan.Zero));

            Assert.That(h.Analytics.SessionsStarted.Count, Is.EqualTo(0));
            Assert.That(h.Analytics.CardsReviewed.Count, Is.EqualTo(0));
            Assert.That(h.Analytics.SessionsFinished.Count, Is.EqualTo(0));
        }

        // ----- Harness ------------------------------------------------------

        private sealed class Harness
        {
            public FakeSubscriber<SessionStartedEvent> SessionStarted { get; } = new();
            public FakeSubscriber<CardReviewedEvent> CardReviewed { get; } = new();
            public FakeSubscriber<SessionUploadCompletedEvent> SessionUploadCompleted { get; } = new();
            public FakeAnalytics Analytics { get; } = new();
            public SessionTelemetryListener Listener { get; }

            public Harness()
            {
                Listener = new SessionTelemetryListener(
                    SessionStarted, CardReviewed, SessionUploadCompleted, Analytics);
            }
        }

        // ----- Fakes --------------------------------------------------------

        private sealed class FakeSubscriber<T> : ISubscriber<T>
        {
            private IMessageHandler<T>? _handler;

            public IDisposable Subscribe(IMessageHandler<T> handler, params MessageHandlerFilter<T>[] filters)
            {
                _handler = handler;
                return new Unsubscriber(this);
            }

            public void Emit(T message) => _handler?.Handle(message);

            private sealed class Unsubscriber : IDisposable
            {
                private readonly FakeSubscriber<T> _owner;
                public Unsubscriber(FakeSubscriber<T> owner) { _owner = owner; }
                public void Dispose() => _owner._handler = null;
            }
        }

        private sealed class FakeAnalytics : IAnalyticsService
        {
            public List<(Guid sessionId, DeckId deckId, int cardCount)> SessionsStarted { get; } = new();
            public List<(Guid sessionId, CardId cardId, ReviewGrade grade, DateTime nextDueAt)> CardsReviewed { get; } = new();
            public List<(Guid sessionId, int reviewedCount, TimeSpan duration)> SessionsFinished { get; } = new();
            public List<string> OfflineFallbacks { get; } = new();

            public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount)
                => SessionsStarted.Add((sessionId, deckId, cardCount));

            public void TrackCardReviewed(Guid sessionId, CardId cardId, ReviewGrade grade, DateTime nextDueAt)
                => CardsReviewed.Add((sessionId, cardId, grade, nextDueAt));

            public void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration)
                => SessionsFinished.Add((sessionId, reviewedCount, duration));

            public void TrackOfflineFallback(string operation)
                => OfflineFallbacks.Add(operation);
        }
    }
}
