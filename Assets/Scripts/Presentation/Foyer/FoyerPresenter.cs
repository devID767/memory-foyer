using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Foyer;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Presentation.Banners;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class FoyerPresenter : IAsyncStartable
    {
        private readonly IDeckRepository _deckRepository;
        private readonly IScheduleStore _scheduleStore;
        private readonly CachingScheduleStore _cachingScheduleStore;
        private readonly IPublisher<DeckSelectedEvent> _deckSelectedPublisher;
        private readonly ISubscriber<BackToFoyerRequested> _backToFoyerSubscriber;
        private readonly FoyerScreen _screen;
        private readonly OfflineBannerView _offlineBannerView;
        private readonly LoadingView _loadingView;

        public FoyerPresenter(
            IDeckRepository deckRepository,
            IScheduleStore scheduleStore,
            CachingScheduleStore cachingScheduleStore,
            IPublisher<DeckSelectedEvent> deckSelectedPublisher,
            ISubscriber<BackToFoyerRequested> backToFoyerSubscriber,
            FoyerScreen screen,
            OfflineBannerView offlineBannerView,
            LoadingView loadingView)
        {
            _deckRepository = deckRepository;
            _scheduleStore = scheduleStore;
            _cachingScheduleStore = cachingScheduleStore;
            _deckSelectedPublisher = deckSelectedPublisher;
            _backToFoyerSubscriber = backToFoyerSubscriber;
            _screen = screen;
            _offlineBannerView = offlineBannerView;
            _loadingView = loadingView;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            IDisposable backToFoyerSubscription = _backToFoyerSubscriber.Subscribe(
                _ => ProbeDrainAndRefreshAsync(cancellation).Forget());

            _screen.DeckClicked += OnDeckClicked;

            cancellation.Register(() =>
            {
                _screen.DeckClicked -= OnDeckClicked;
                backToFoyerSubscription.Dispose();
            });

            await ProbeDrainAndRefreshAsync(cancellation);
        }

        private async UniTask ProbeDrainAndRefreshAsync(CancellationToken ct)
        {
            // Foyer canvas reveals only after data is bound — loading view fronts the wait,
            // then its on-hidden callback flips the canvas on with fresh stats.
            _loadingView.Show(_screen.Show);

            bool reachable = await _scheduleStore.IsServerReachableAsync(ct);
            _offlineBannerView.SetVisible(!reachable);

            // Drain before refresh: parallel GETs would race the sequential drain POSTs.
            if (reachable)
            {
                await DrainPending(ct);
            }

            await RefreshAsync(ct);
            _loadingView.Hide();
        }

        private async UniTask DrainPending(CancellationToken cancellation)
        {
            try
            {
                await _cachingScheduleStore.DrainPendingAsync(cancellation);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Foyer] Pending drain failed: {ex.Message}");
            }
        }

        private void OnDeckClicked(DeckId deckId)
        {
            // Publish before Hide so any synchronous subscriber observes the "foyer visible"
            // state at publish-time.
            _deckSelectedPublisher.Publish(new DeckSelectedEvent(deckId));
            _screen.Hide();
        }

        private async UniTask RefreshAsync(CancellationToken ct)
        {
            IReadOnlyList<Deck> decks = await _deckRepository.GetAllAsync(ct);
            IReadOnlyList<DeckSummary> summaries = await _scheduleStore.GetDeckSummariesAsync(ct);

            Dictionary<string, DeckSummary> summaryById =
                new Dictionary<string, DeckSummary>(summaries.Count);
            foreach (DeckSummary summary in summaries)
            {
                summaryById[summary.Id.Value] = summary;
            }

            List<DeckListEntry> entries = new List<DeckListEntry>(decks.Count);
            foreach (Deck deck in decks)
            {
                if (summaryById.TryGetValue(deck.Id.Value, out DeckSummary? summary))
                {
                    entries.Add(new DeckListEntry(
                        deck.Id, summary.DisplayName, summary.DueCount, summary.TotalCount));
                }
                else
                {
                    entries.Add(new DeckListEntry(
                        deck.Id, deck.DisplayName, 0, deck.Cards.Count));
                }
            }

            IReadOnlyList<DeckListEntry> ordered = DeckOrdering.Order(entries);

            DeckButtonModel[] models = new DeckButtonModel[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                DeckListEntry entry = ordered[i];
                models[i] = new DeckButtonModel(
                    entry.Id, entry.DisplayName, entry.DueCount, entry.TotalCount);
            }

            _screen.Bind(models);
        }
    }
}
