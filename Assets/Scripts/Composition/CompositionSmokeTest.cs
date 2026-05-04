using MessagePipe;
using MemoryFoyer.Application.Events;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Application.Sessions;
using UnityEngine;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    internal sealed class CompositionSmokeTest : IStartable
    {
        private readonly IReviewSessionService _session;
        private readonly IDeckRepository _decks;
        private readonly IScheduleStore _schedule;
        private readonly IPublisher<DeckSelectedEvent> _deckSelectedPublisher;

        public CompositionSmokeTest(
            IReviewSessionService session,
            IDeckRepository decks,
            IScheduleStore schedule,
            IPublisher<DeckSelectedEvent> deckSelectedPublisher)
        {
            _session = session;
            _decks = decks;
            _schedule = schedule;
            _deckSelectedPublisher = deckSelectedPublisher;
        }

        public void Start()
        {
            Debug.Assert(_schedule is CachingScheduleStore,
                $"[Composition] IScheduleStore expected CachingScheduleStore, got {_schedule.GetType().Name}");

            Debug.Log(
                $"[Composition] DI ok — session.State={_session.State}, " +
                $"decks={_decks.GetType().Name}, schedule={_schedule.GetType().Name}, " +
                $"publisher={_deckSelectedPublisher.GetType().Name}");
        }
    }
}
