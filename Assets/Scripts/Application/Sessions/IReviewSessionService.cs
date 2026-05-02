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

        UniTask StartAsync(DeckId deckId, CancellationToken ct = default);
        void RevealCurrent();
        UniTask GradeAsync(ReviewGrade grade, CancellationToken ct = default);
        UniTask EndAsync(CancellationToken ct = default);
    }
}
