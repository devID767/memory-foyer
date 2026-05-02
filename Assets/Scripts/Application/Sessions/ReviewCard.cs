using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Sessions
{
    public sealed record ReviewCard(CardId CardId, string Front, string Back, Sm2State State);
}
