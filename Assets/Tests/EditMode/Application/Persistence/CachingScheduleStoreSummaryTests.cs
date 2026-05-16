using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using MemoryFoyer.Application.Analytics;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Scheduling;

namespace MemoryFoyer.Tests.EditMode.Application.Persistence
{
    [TestFixture]
    public sealed class CachingScheduleStoreSummaryTests
    {
        private static readonly IReadOnlyList<DeckSummary> ServerSummaries = new[]
        {
            new DeckSummary(new DeckId("capitals"), "Capitals", 12, 10, 44),
        };

        private static readonly IReadOnlyList<DeckSummary> CachedSummaries = new[]
        {
            new DeckSummary(new DeckId("capitals"), "Capitals", 3, 0, 44),
        };

        [Test]
        public void GetDeckSummariesAsync_OnSuccess_CachesAndReturnsServerResult()
        {
            FakeCache cache = new FakeCache();
            FakeAnalytics analytics = new FakeAnalytics();
            CachingScheduleStore store = new CachingScheduleStore(
                new FakeInnerStore { Summaries = ServerSummaries }, cache, analytics);

            IReadOnlyList<DeckSummary> result =
                store.GetDeckSummariesAsync().GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(ServerSummaries));
            Assert.That(cache.SavedSummaries, Is.EqualTo(ServerSummaries));
            Assert.That(analytics.OfflineFallbacks, Is.Empty);
        }

        [Test]
        public void GetDeckSummariesAsync_WhenUnavailableWithCache_ReturnsCachedAndTracksFallback()
        {
            FakeCache cache = new FakeCache { StoredSummaries = CachedSummaries };
            FakeAnalytics analytics = new FakeAnalytics();
            CachingScheduleStore store = new CachingScheduleStore(
                new FakeInnerStore { ThrowUnavailable = true }, cache, analytics);

            IReadOnlyList<DeckSummary> result =
                store.GetDeckSummariesAsync().GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo(CachedSummaries));
            Assert.That(analytics.OfflineFallbacks, Is.EqualTo(new[] { "GetDeckSummaries" }));
        }

        [Test]
        public void GetDeckSummariesAsync_WhenUnavailableWithoutCache_Rethrows()
        {
            FakeCache cache = new FakeCache { StoredSummaries = null };
            CachingScheduleStore store = new CachingScheduleStore(
                new FakeInnerStore { ThrowUnavailable = true }, cache, new FakeAnalytics());

            Assert.Throws<ScheduleStoreUnavailableException>(() =>
                store.GetDeckSummariesAsync().GetAwaiter().GetResult());
        }

        // ----- Fakes --------------------------------------------------------

        private sealed class FakeInnerStore : IScheduleStore
        {
            public IReadOnlyList<DeckSummary> Summaries = Array.Empty<DeckSummary>();
            public bool ThrowUnavailable;

            public UniTask<IReadOnlyList<DeckSummary>> GetDeckSummariesAsync(CancellationToken ct = default)
            {
                if (ThrowUnavailable)
                {
                    throw new ScheduleStoreUnavailableException("offline");
                }
                return UniTask.FromResult(Summaries);
            }

            public UniTask<DeckSchedule> GetDeckScheduleAsync(DeckId deckId, CancellationToken ct = default)
                => throw new NotImplementedException();

            public UniTask EnqueuePendingAsync(SessionResult result, CancellationToken ct = default)
                => UniTask.CompletedTask;

            public UniTask<DeckSchedule> UploadSessionAsync(SessionResult result, CancellationToken ct = default)
                => throw new NotImplementedException();

            public UniTask<bool> IsServerReachableAsync(CancellationToken ct = default)
                => UniTask.FromResult(!ThrowUnavailable);
        }

        private sealed class FakeCache : IScheduleCache
        {
            public IReadOnlyList<DeckSummary>? StoredSummaries;
            public IReadOnlyList<DeckSummary>? SavedSummaries;

            public void SaveDeckSummaries(IReadOnlyList<DeckSummary> summaries)
                => SavedSummaries = summaries;

            public IReadOnlyList<DeckSummary>? LoadDeckSummaries() => StoredSummaries;

            public void Save(DeckSchedule schedule) { }
            public DeckSchedule? Load(DeckId deckId) => null;
            public bool Has(DeckId deckId) => false;
            public void AppendPending(SessionResult result) { }
            public void RemovePending(Guid sessionId) { }
            public IReadOnlyList<SessionResult> LoadPending() => Array.Empty<SessionResult>();
        }

        private sealed class FakeAnalytics : IAnalyticsService
        {
            public List<string> OfflineFallbacks { get; } = new();

            public void TrackSessionStarted(Guid sessionId, DeckId deckId, int cardCount) { }
            public void TrackCardReviewed(Guid sessionId, CardId cardId, ReviewGrade grade, DateTime nextDueAt) { }
            public void TrackSessionFinished(Guid sessionId, int reviewedCount, TimeSpan duration) { }
            public void TrackOfflineFallback(string operation) => OfflineFallbacks.Add(operation);
        }
    }
}
