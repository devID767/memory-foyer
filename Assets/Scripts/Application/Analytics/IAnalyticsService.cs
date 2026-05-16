using System;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Application.Analytics
{
    public interface IAnalyticsService
    {
        void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount);
        void TrackCardReviewed(Guid sessionId, CardId cardId, ReviewGrade grade, DateTime nextDueAt);
        void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration);
        void TrackOfflineFallback(string operation);
    }
}
