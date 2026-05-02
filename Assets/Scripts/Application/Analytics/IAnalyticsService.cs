using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Analytics
{
    public interface IAnalyticsService
    {
        void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount);
        void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration);
        void TrackOfflineFallback(string operation);
    }
}
