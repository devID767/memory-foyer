using System;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Infrastructure.Analytics
{
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount)
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
