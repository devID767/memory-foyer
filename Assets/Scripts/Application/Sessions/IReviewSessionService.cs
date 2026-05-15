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
        /// <summary>
        /// 1-based position of the card currently shown among the initial <see cref="Total"/>
        /// (distinct cards cleared + 1). Bounded to 1..Total; an Again does not advance it.
        /// 0 when not <see cref="SessionState.Playing"/>.
        /// </summary>
        int Position { get; }

        UniTask StartAsync(DeckId deckId, CancellationToken ct = default);
        void RevealCurrent();
        UniTask GradeAsync(ReviewGrade grade, CancellationToken ct = default);
        UniTask EndAsync(CancellationToken ct = default);
        UniTask CommitAsync(CancellationToken ct = default);
    }
}
