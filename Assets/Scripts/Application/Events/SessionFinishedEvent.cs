using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Events
{
    public sealed record SessionFinishedEvent(Guid SessionId, DeckId DeckId, int ReviewedCount, bool UploadedSuccessfully);
}
