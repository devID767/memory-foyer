using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Events
{
    public sealed record SessionStartedEvent(Guid SessionId, DeckId DeckId, int CardCount, DateTime StartedAt);
}
