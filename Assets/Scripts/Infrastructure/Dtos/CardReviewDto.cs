using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class CardReviewDto
    {
        public string cardId = "";
        /// <summary>Integer grade per GDD §4: valid values are 0, 3, 4, 5.</summary>
        public int grade = 0;
        public string reviewedAt = "";
    }
}
