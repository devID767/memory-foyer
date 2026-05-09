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
        private readonly List<float> _baseYs = new();

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
                float baseY = _baseYs[i];
                float yOffset = ComputeYOffset(i, count);
                cardRt.localPosition = new Vector3(cardRt.localPosition.x, baseY + yOffset, 0f);
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
                _baseYs.Add(card.transform.localPosition.y);
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

        private float ComputeYOffset(int index, int count)
        {
            float max = _config.MaxYOffset;
            if (max <= 0f || count <= 0)
            {
                return 0f;
            }

            return (Hash01(index, salt: 17) * 2f - 1f) * max;
        }

        private static float Hash01(int index, int salt)
        {
            unchecked
            {
                uint x = (uint)(index * 374761393 + salt * 668265263);
                x = (x ^ (x >> 13)) * 1274126177u;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
