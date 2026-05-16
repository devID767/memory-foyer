using System;
using MessagePipe;
using MemoryFoyer.Application.Events;

namespace MemoryFoyer.Application.Analytics
{
    public sealed class SessionTelemetryListener : IDisposable
    {
        private readonly IAnalyticsService _analytics;
        private readonly IDisposable[] _subscriptions;

        public SessionTelemetryListener(
            ISubscriber<SessionStartedEvent> sessionStartedSubscriber,
            ISubscriber<CardReviewedEvent> cardReviewedSubscriber,
            ISubscriber<SessionUploadCompletedEvent> sessionUploadCompletedSubscriber,
            IAnalyticsService analytics)
        {
            if (sessionStartedSubscriber == null) { throw new ArgumentNullException(nameof(sessionStartedSubscriber)); }
            if (cardReviewedSubscriber == null) { throw new ArgumentNullException(nameof(cardReviewedSubscriber)); }
            if (sessionUploadCompletedSubscriber == null) { throw new ArgumentNullException(nameof(sessionUploadCompletedSubscriber)); }
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));

            _subscriptions = new[]
            {
                sessionStartedSubscriber.Subscribe(OnSessionStarted),
                cardReviewedSubscriber.Subscribe(OnCardReviewed),
                sessionUploadCompletedSubscriber.Subscribe(OnSessionUploadCompleted),
            };
        }

        private void OnSessionStarted(SessionStartedEvent e)
        {
            _analytics.TrackSessionStarted(e.SessionId, e.DeckId, e.CardCount);
        }

        private void OnCardReviewed(CardReviewedEvent e)
        {
            _analytics.TrackCardReviewed(e.SessionId, e.CardId, e.Grade, e.NextDueAt);
        }

        private void OnSessionUploadCompleted(SessionUploadCompletedEvent e)
        {
            if (e.Success)
            {
                _analytics.TrackSessionFinished(e.SessionId, e.ReviewedCount, e.Duration);
            }
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
