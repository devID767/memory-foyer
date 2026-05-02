using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Persistence
{
    /// <summary>
    /// Composite <see cref="IScheduleStore"/> that adds offline fallback and pending-upload
    /// queuing on top of an inner primary store (the future <c>HttpScheduleStore</c>).
    /// DI in Phase 4 binds the public <c>IScheduleStore</c> slot to this class and
    /// injects <c>HttpScheduleStore</c> into the <c>inner</c> constructor parameter.
    /// </summary>
    public sealed class CachingScheduleStore : IScheduleStore
    {
        private readonly IScheduleStore _inner;
        private readonly IScheduleCache _cache;
        private readonly IAnalyticsService _analytics;

        public CachingScheduleStore(
            IScheduleStore inner,
            IScheduleCache cache,
            IAnalyticsService analytics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public async UniTask<DeckSchedule> GetDeckScheduleAsync(
            DeckId deckId, CancellationToken ct = default)
        {
            try
            {
                DeckSchedule schedule = await _inner.GetDeckScheduleAsync(deckId, ct);
                _cache.Save(schedule);
                return schedule with { Source = ScheduleSource.Server };
            }
            catch (ScheduleStoreUnavailableException)
            {
                DeckSchedule? cached = _cache.Load(deckId);
                if (cached is not null)
                {
                    _analytics.TrackOfflineFallback("GetDeckSchedule");
                    return cached with { Source = ScheduleSource.Cache };
                }

                throw;
            }
        }

        public async UniTask<DeckSchedule> UploadSessionAsync(
            SessionResult result, CancellationToken ct = default)
        {
            _cache.AppendPending(result);

            try
            {
                DeckSchedule schedule = await _inner.UploadSessionAsync(result, ct);
                _cache.RemovePending(result.SessionId);
                _cache.Save(schedule);
                return schedule;
            }
            catch (ScheduleStoreUnavailableException)
            {
                _analytics.TrackOfflineFallback("UploadSession");
                throw;
            }
        }

        public UniTask<bool> IsServerReachableAsync(CancellationToken ct = default)
        {
            return _inner.IsServerReachableAsync(ct);
        }

        // Concrete-only operation: not on IScheduleStore per architecture.md §Contracts.
        // Called by ProjectLifetimeScope at startup (Phase 4) to flush queued sessions.
        public async UniTask DrainPendingAsync(CancellationToken ct = default)
        {
            foreach (SessionResult pending in _cache.LoadPending())
            {
                try
                {
                    DeckSchedule schedule = await _inner.UploadSessionAsync(pending, ct);
                    _cache.RemovePending(pending.SessionId);
                    _cache.Save(schedule);
                }
                catch (ScheduleStoreUnavailableException)
                {
                    // Transient failure — stop the loop, caller will retry on next reconnect.
                    return;
                }
                catch (ScheduleStoreContractException ex) when (ex.StatusCode == 409)
                {
                    // Mismatch (e.g. session already applied) — drop the pending entry and continue.
                    _analytics.TrackOfflineFallback($"DrainPending:409:session={pending.SessionId}");
                    _cache.RemovePending(pending.SessionId);
                }
            }
        }
    }
}
