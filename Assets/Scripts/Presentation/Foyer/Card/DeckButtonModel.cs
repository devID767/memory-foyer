using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed record DeckButtonModel(
        DeckId Id,
        string DisplayName,
        int DueCount,
        int TotalCount);
}
