using System;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Domain.Models
{
    public sealed record Sm2State(
        int Repetitions,
        double EaseFactor,
        int IntervalDays,
        DateTime DueAt,
        LearningStage Stage,
        int LearningStepIndex);
}
