using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Application.Repositories;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Infrastructure.ScriptableObjects;

namespace MemoryFoyer.Infrastructure.Repositories
{
    public sealed class ScriptableObjectDeckRepository : IDeckRepository
    {
        private readonly Dictionary<DeckId, Deck> _byId;
        private readonly IReadOnlyList<Deck> _all;

        public ScriptableObjectDeckRepository(IReadOnlyList<DeckAsset> assets)
        {
            _byId = new Dictionary<DeckId, Deck>(assets.Count);
            Deck[] all = new Deck[assets.Count];
            for (int i = 0; i < assets.Count; i++)
            {
                Deck deck = Map(assets[i]);
                _byId[deck.Id] = deck;
                all[i] = deck;
            }

            _all = all;
        }

        public UniTask<Deck> GetDeckAsync(DeckId deckId, CancellationToken ct = default)
        {
            return UniTask.FromResult(_byId[deckId]);
        }

        public UniTask<IReadOnlyList<Deck>> GetAllAsync(CancellationToken ct = default)
        {
            return UniTask.FromResult(_all);
        }

        private static Deck Map(DeckAsset asset)
        {
            Card[] cards = new Card[asset.Cards.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = Map(asset.Cards[i]);
            }

            return new Deck(
                Id: new DeckId(asset.DeckId),
                DisplayName: asset.DisplayName,
                Description: asset.Description,
                NewCardsPerDay: asset.NewCardsPerDay,
                Cards: cards);
        }

        private static Card Map(CardData data) =>
            new Card(new CardId(data.CardId), data.Front, data.Back);
    }
}
