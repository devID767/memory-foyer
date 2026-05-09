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
        private ArtPaletteConfig _palette = null!;
        private IReadOnlyDictionary<DeckId, Sprite> _icons = null!;

        private readonly List<DeckCardView> _cards = new();

        public event Action<DeckId>? DeckClicked;

        [Inject]
        public void Construct(FoyerLayoutConfig config, ArtPaletteConfig palette, IReadOnlyDictionary<DeckId, Sprite> icons)
        {
            _config = config;
            _palette = palette;
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

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].gameObject.SetActive(i < count);
            }

            LayoutCards(count);

            for (int i = 0; i < count; i++)
            {
                DeckButtonModel model = models[i];
                DeckCardView card = _cards[i];

                card.ResetRestPositionCapture();
                Sprite[] pins = _config.PinVariants;
                if (pins != null && pins.Length > 0)
                {
                    card.SetPin(pins[i % pins.Length]);
                }

                Sprite? icon = _icons.TryGetValue(model.Id, out Sprite sprite) ? sprite : null;
                card.Bind(model, icon);
            }
        }

        private void LayoutCards(int count)
        {
            if (count <= 0)
            {
                return;
            }

            float spacing = _config.Spacing;

            float totalWidth = 0f;
            for (int i = 0; i < count; i++)
            {
                totalWidth += ((RectTransform)_cards[i].transform).rect.width;
            }
            totalWidth += spacing * (count - 1);

            float cursorX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RectTransform cardRt = (RectTransform)_cards[i].transform;
                // Pivot is centered (0.5, 0.5) on the card prefab — place anchor on card center.
                float width = cardRt.rect.width;
                float x = cursorX + width * 0.5f;
                float y = ComputeYOffset(i, count);

                cardRt.localPosition = new Vector3(x, y, 0f);
                cardRt.localScale = Vector3.one;

                float tilt = ComputeTilt(i, count);
                cardRt.localRotation = Quaternion.Euler(0f, 0f, tilt);
                _cards[i].SetRestRotation(tilt);

                cursorX += width + spacing;
            }
        }

        private void EnsureCardCount(int count)
        {
            while (_cards.Count < count)
            {
                DeckCardView card = Instantiate(_config.CardPrefab, _cardsRoot);
                card.Configure(_config, _palette);
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

        private float ComputeYOffset(int index, int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            float arc = 0f;
            if (count > 1 && _config.ArcHeight > 0f)
            {
                float t = (float)index / (count - 1) * 2f - 1f;
                arc = (1f - t * t) * _config.ArcHeight;
            }

            float jitter = 0f;
            if (_config.JitterAmount > 0f)
            {
                jitter = (Hash01(index, salt: 17) - 0.5f) * _config.JitterAmount;
            }

            return arc + jitter;
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
