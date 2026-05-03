using System;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Domain.Models;
using UnityEngine;

namespace MemoryFoyer.Infrastructure.Analytics
{
    public sealed class ConsoleAnalyticsService : IAnalyticsService
    {
        public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount)
        {
            Debug.Log($"[Analytics] SessionStarted sessionId={sessionId} deckId={deckId.Value} cards={cardCount}");
        }

        public void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration)
        {
            Debug.Log($"[Analytics] SessionFinished sessionId={sessionId} reviewed={reviewedCount} duration={duration}");
        }

        public void TrackOfflineFallback(string operation)
        {
            Debug.Log($"[Analytics] OfflineFallback operation={operation}");
        }
    }
}
