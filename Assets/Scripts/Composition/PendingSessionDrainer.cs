using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Persistence;
using UnityEngine;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    internal sealed class PendingSessionDrainer : IAsyncStartable
    {
        private readonly CachingScheduleStore _store;

        public PendingSessionDrainer(CachingScheduleStore store)
        {
            _store = store;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            try
            {
                await _store.DrainPendingAsync(cancellation);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Best-effort: pending entries stay on disk and retry on next app start.
                Debug.LogWarning($"[Composition] Pending drain failed: {ex.Message}");
            }
        }
    }
}
