using System.Threading;
using Cysharp.Threading.Tasks;

namespace MemoryFoyer.Application.Persistence
{
    public interface IPendingDrain
    {
        UniTask DrainPendingAsync(CancellationToken ct = default);
    }
}
