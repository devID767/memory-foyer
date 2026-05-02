using System;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Application.Events
{
    public sealed record CardReviewedEvent(Guid SessionId, CardId CardId, ReviewGrade Grade, DateTime NextDueAt);
}
