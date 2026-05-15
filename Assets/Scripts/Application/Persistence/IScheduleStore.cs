using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    public interface IScheduleStore
    {
        UniTask<IReadOnlyList<DeckSummary>> GetDeckSummariesAsync(CancellationToken ct = default);
        UniTask<DeckSchedule> GetDeckScheduleAsync(DeckId deckId, CancellationToken ct = default);
        UniTask EnqueuePendingAsync(SessionResult result, CancellationToken ct = default);
        UniTask<DeckSchedule> UploadSessionAsync(SessionResult result, CancellationToken ct = default);
        UniTask<bool> IsServerReachableAsync(CancellationToken ct = default);
    }
}
