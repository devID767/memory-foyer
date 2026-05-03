using System.Threading;
using Cysharp.Threading.Tasks;

namespace MemoryFoyer.Application.Http
{
    public interface IHttpClient
    {
        UniTask<TResponse> GetAsync<TResponse>(string path, CancellationToken ct = default);
        UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    }
}
