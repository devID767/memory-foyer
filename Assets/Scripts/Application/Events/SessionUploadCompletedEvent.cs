using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Events
{
    public sealed record SessionUploadCompletedEvent(
        Guid SessionId, DeckId DeckId, bool Success, int ReviewedCount, TimeSpan Duration);
}
