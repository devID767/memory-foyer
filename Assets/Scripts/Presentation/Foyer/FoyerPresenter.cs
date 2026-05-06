using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Domain.Time;
using MessagePipe;
using VContainer.Unity;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class FoyerPresenter : IAsyncStartable
    {
        private readonly IDeckRepository _deckRepository;
        private readonly IScheduleStore _scheduleStore;
        private readonly IClock _clock;
        private readonly IPublisher<DeckSelectedEvent> _deckSelectedPublisher;
        private readonly ISubscriber<SessionFinishedEvent> _sessionFinishedSubscriber;
        private readonly DeckSelectionView _deckSelectionView;
        private readonly OfflineBannerView _offlineBannerView;

        public FoyerPresenter(
            IDeckRepository deckRepository,
            IScheduleStore scheduleStore,
            IClock clock,
            IPublisher<DeckSelectedEvent> deckSelectedPublisher,
            ISubscriber<SessionFinishedEvent> sessionFinishedSubscriber,
            DeckSelectionView deckSelectionView,
            OfflineBannerView offlineBannerView)
        {
            _deckRepository = deckRepository;
            _scheduleStore = scheduleStore;
            _clock = clock;
            _deckSelectedPublisher = deckSelectedPublisher;
            _sessionFinishedSubscriber = sessionFinishedSubscriber;
            _deckSelectionView = deckSelectionView;
            _offlineBannerView = offlineBannerView;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            IDisposable sessionFinishedSubscription = _sessionFinishedSubscriber.Subscribe(
                _ => RefreshAsync(cancellation).Forget());

            _deckSelectionView.DeckClicked += OnDeckClicked;

            cancellation.Register(() =>
            {
                _deckSelectionView.DeckClicked -= OnDeckClicked;
                sessionFinishedSubscription.Dispose();
            });

            bool reachable = await _scheduleStore.IsServerReachableAsync(cancellation);
            _offlineBannerView.SetVisible(!reachable);

            await RefreshAsync(cancellation);
        }

        private void OnDeckClicked(DeckId deckId)
        {
            _deckSelectedPublisher.Publish(new DeckSelectedEvent(deckId));
        }

        private async UniTask RefreshAsync(CancellationToken ct)
        {
            IReadOnlyList<Deck> decks = await _deckRepository.GetAllAsync(ct);

            DeckSchedule[] schedules = await UniTask.WhenAll(
                decks.Select(d => _scheduleStore.GetDeckScheduleAsync(d.Id, ct)));

            DeckButtonModel[] models = new DeckButtonModel[decks.Count];
            for (int i = 0; i < decks.Count; i++)
            {
                Deck deck = decks[i];
                DeckSchedule schedule = schedules[i];
                int dueCount = schedule.Cards.Count(c => c.State.DueAt <= _clock.UtcNow);
                models[i] = new DeckButtonModel(deck.Id, deck.DisplayName, dueCount, deck.Cards.Count);
            }

            _deckSelectionView.Bind(models);
        }
    }
}
