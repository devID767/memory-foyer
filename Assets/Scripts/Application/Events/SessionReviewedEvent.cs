using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Events
{
    public sealed record SessionReviewedEvent(Guid SessionId, DeckId DeckId, int ReviewedCount);
}
