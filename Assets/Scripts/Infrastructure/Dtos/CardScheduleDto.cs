using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class CardScheduleDto
    {
        public string cardId = "";
        public int reps = 0;
        public double easeFactor = 0.0;
        public int intervalDays = 0;
        public string dueAt = "";
        public string stage = "";
        public int learningStep = 0;
    }
}
