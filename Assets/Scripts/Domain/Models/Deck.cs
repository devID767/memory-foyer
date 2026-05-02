using System.Collections.Generic;

namespace MemoryFoyer.Domain.Models
{
    public sealed record Deck(
        DeckId Id,
        string DisplayName,
        string Description,
        int NewCardsPerDay,
        IReadOnlyList<Card> Cards);
}
