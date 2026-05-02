using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public sealed record DeckSchedule(
        DeckId DeckId,
        IReadOnlyList<CardSchedule> Cards,
        DateTime FetchedAt,
        ScheduleSource Source);
}
