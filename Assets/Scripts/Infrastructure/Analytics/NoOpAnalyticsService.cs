using System;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Infrastructure.Analytics
{
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount)
        {
        }

        public void TrackCardReviewed(Guid sessionId, CardId cardId, ReviewGrade grade, DateTime nextDueAt)
        {
        }

        public void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration)
        {
        }

        public void TrackOfflineFallback(string operation)
        {
        }
    }
}
