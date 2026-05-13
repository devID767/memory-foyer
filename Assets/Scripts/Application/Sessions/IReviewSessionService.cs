using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Application.Sessions
{
    public interface IReviewSessionService
    {
        SessionState State { get; }
        ReviewCard? CurrentCard { get; }
        int Remaining { get; }
        int Total { get; }
        /// <summary>Count of <see cref="GradeAsync"/> calls in the current session, including Again.</summary>
        int ReviewsCompleted { get; }

        UniTask StartAsync(DeckId deckId, CancellationToken ct = default);
        void RevealCurrent();
        UniTask GradeAsync(ReviewGrade grade, CancellationToken ct = default);
        UniTask EndAsync(CancellationToken ct = default);
        UniTask CommitAsync(CancellationToken ct = default);
    }
}
