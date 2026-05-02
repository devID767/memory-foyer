using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Repositories
{
    public interface IDeckRepository
    {
        UniTask<Deck> GetDeckAsync(DeckId deckId, CancellationToken ct = default);
        UniTask<IReadOnlyList<Deck>> GetAllAsync(CancellationToken ct = default);
    }
}
