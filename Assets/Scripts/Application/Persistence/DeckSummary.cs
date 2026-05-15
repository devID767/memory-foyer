using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public sealed record DeckSummary(
        DeckId Id,
        string DisplayName,
        int DueCount,
        int NewCount,
        int TotalCount);
}
