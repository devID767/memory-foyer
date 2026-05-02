using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public interface IScheduleCache
    {
        void Save(DeckSchedule schedule);
        DeckSchedule? Load(DeckId deckId);
        bool Has(DeckId deckId);
        void AppendPending(SessionResult result);
        void RemovePending(Guid sessionId);
        IReadOnlyList<SessionResult> LoadPending();
    }
}
