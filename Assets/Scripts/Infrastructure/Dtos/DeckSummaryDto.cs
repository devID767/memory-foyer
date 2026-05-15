using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class DeckSummaryDto
    {
        public string deckId = "";
        public string displayName = "";
        public int dueCount;
        public int newCount;
        public int totalCount;
    }
}
