using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public sealed record CardSchedule(CardId CardId, Sm2State State);
}
