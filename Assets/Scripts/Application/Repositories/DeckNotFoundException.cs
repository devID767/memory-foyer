using System;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Repositories
{
    public sealed class DeckNotFoundException : Exception
    {
        public DeckId DeckId { get; }

        public DeckNotFoundException(DeckId deckId)
            : base($"Deck not found: {deckId.Value}")
        {
            DeckId = deckId;
        }
    }
}
