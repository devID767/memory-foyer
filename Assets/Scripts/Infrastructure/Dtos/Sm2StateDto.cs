using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    /// <summary>
    /// Internal mapper-only helper mirroring the flat SM-2 fields.
    /// Not used directly as a JSON root — absorbed into CardScheduleDto.
    /// </summary>
    [Serializable]
    internal sealed class Sm2StateDto
    {
        public int reps = 0;
        public double easeFactor = 0.0;
        public int intervalDays = 0;
        public string dueAt = "";
        public string stage = "";
        public int learningStep = 0;
    }
}
