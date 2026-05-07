using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;
using UnityEngine;
using VContainer;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckSelectionView : MonoBehaviour
    {
        [SerializeField] private Transform _cardsRoot = null!; // set in Inspector

        private FoyerLayoutConfig _config = null!;
        private IReadOnlyDictionary<DeckId, Sprite> _icons = null!;

        private readonly List<DeckCardView> _cards = new();

        public event Action<DeckId>? DeckClicked;

        [Inject]
        public void Construct(FoyerLayoutConfig config, IReadOnlyDictionary<DeckId, Sprite> icons)
        {
            _config = config;
            _icons = icons;
        }

        private void OnDestroy()
        {
            foreach (DeckCardView card in _cards)
            {
                card.Clicked -= OnChildClicked;
            }
            _cards.Clear();
        }

        public void Bind(IReadOnlyList<DeckButtonModel> models)
        {
            int count = Math.Min(models.Count, _config.MaxDecksToShow);

            EnsureCardCount(count);

            for (int i = 0; i < count; i++)
            {
                DeckButtonModel model = models[i];
                DeckCardView card = _cards[i];

                RectTransform cardRt = (RectTransform)card.transform;
                cardRt.localPosition = new Vector3(cardRt.localPosition.x, cardRt.localPosition.y, 0f);
                cardRt.localScale = Vector3.one;

                float tilt = ComputeTilt(i, count);
                cardRt.localRotation = Quaternion.Euler(0f, 0f, tilt);

                card.SetRestRotation(tilt);
                card.gameObject.SetActive(true);

                Sprite? icon = _icons.TryGetValue(model.Id, out Sprite sprite) ? sprite : null;
                card.Bind(model, icon);
            }

            for (int i = count; i < _cards.Count; i++)
            {
                _cards[i].gameObject.SetActive(false);
            }
        }

        private void EnsureCardCount(int count)
        {
            while (_cards.Count < count)
            {
                DeckCardView card = Instantiate(_config.CardPrefab, _cardsRoot);
                card.Configure(_config);
                card.Clicked += OnChildClicked;
                _cards.Add(card);
            }
        }

        private void OnChildClicked(DeckId deckId)
        {
            DeckClicked?.Invoke(deckId);
        }

        private float ComputeTilt(int index, int count)
        {
            float max = _config.MaxTiltDegrees;
            if (max <= 0f || count <= 1)
            {
                return 0f;
            }

            // Deterministic alternating tilt: outer cards tilt outward, middle stays flat.
            float t = (float)index / (count - 1) * 2f - 1f;
            return -t * max;
        }
    }
}
