using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public sealed record SessionResult(
        Guid SessionId,
        DeckId DeckId,
        IReadOnlyList<CardReview> Reviews);
}
