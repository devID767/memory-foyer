using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class SessionResultDto
    {
        public string sessionId = "";
        public string deckId = "";
        public CardReviewDto[] reviews = Array.Empty<CardReviewDto>();
    }
}
