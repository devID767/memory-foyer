using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class DeckScheduleDto
    {
        public string deckId = "";
        public CardScheduleDto[] cards = Array.Empty<CardScheduleDto>();
    }
}
