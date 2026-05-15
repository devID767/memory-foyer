using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class DeckSummaryListDto
    {
        public DeckSummaryDto[] decks = Array.Empty<DeckSummaryDto>();
    }
}
