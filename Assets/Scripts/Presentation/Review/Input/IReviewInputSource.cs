using System;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Presentation.Review
{
    public interface IReviewInputSource
    {
        event Action? RevealPressed;
        event Action<ReviewGrade>? GradePressed;
        event Action? ClosePressed;
    }
}
