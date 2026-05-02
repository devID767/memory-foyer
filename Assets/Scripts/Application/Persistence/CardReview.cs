using System;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Application.Persistence
{
    public sealed record CardReview(CardId CardId, ReviewGrade Grade, DateTime ReviewedAt);
}
