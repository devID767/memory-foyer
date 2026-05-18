using System;
using System.Collections.Generic;
using DG.Tweening;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Presentation.Common;
using UnityEngine;
using VContainer;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckSelectionView : MonoBehaviour
    {
        [SerializeField] private Transform _cardsRoot = null!; // set in Inspector

        private FoyerLayoutConfig _config = null!;
        private ArtPaletteConfig _palette = null!;
        private UIAnimationConfig _uiConfig = null!;
        private IReadOnlyDictionary<DeckId, Sprite> _icons = null!;

        private readonly List<DeckCardView> _cards = new();
        private Tween? _entranceTween;

        public event Action<DeckId>? DeckClicked;

        [Inject]
        public void Construct(FoyerLayoutConfig config, ArtPaletteConfig palette, UIAnimationConfig uiConfig, IReadOnlyDictionary<DeckId, Sprite> icons)
        {
            _config = config;
            _palette = palette;
            _uiConfig = uiConfig;
            _icons = icons;
        }

        private void OnDestroy()
        {
            _entranceTween?.Kill();
            foreach (DeckCardView card in _cards)
            {
                card.Clicked -= OnChildClicked;
            }
            _cards.Clear();
        }

        public void Bind(IReadOnlyList<DeckButtonModel> models)
        {
            _entranceTween?.Kill();

            int count = Math.Min(models.Count, _config.MaxDecksToShow);
            EnsureCardCount(count);

            float totalWidth = ComputeTotalWidth(count);
            float cursorX = -totalWidth * 0.5f;

            List<RectTransform> entering = new(count);

            for (int i = 0; i < _cards.Count; i++)
            {
                DeckCardView card = _cards[i];
                bool active = i < count;
                card.gameObject.SetActive(active);

                if (!active)
                {
                    continue;
                }

                DeckButtonModel model = models[i];
                Sprite? icon = _icons.TryGetValue(model.Id, out Sprite sprite) ? sprite : null;

                float width = ((RectTransform)card.transform).rect.width;
                float x = cursorX + width * 0.5f;
                float y = ComputeYOffset(i, count);
                float tilt = ComputeTilt(i, count);
                Sprite? pin = PickPin(i);

                card.Bind(model, icon);
                card.ApplyLayout(new Vector2(x, y), tilt, pin);
                entering.Add((RectTransform)card.transform);

                cursorX += width + _config.Spacing;
            }

            if (entering.Count > 0)
            {
                _entranceTween = new StaggeredFadeInAnimator(entering, _uiConfig).BuildEntrance().Play();
            }
        }

        private float ComputeTotalWidth(int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                total += ((RectTransform)_cards[i].transform).rect.width;
            }
            return total + _config.Spacing * (count - 1);
        }

        private Sprite? PickPin(int index)
        {
            Sprite[] pins = _config.PinVariants;
            if (pins == null || pins.Length == 0)
            {
                return null;
            }
            return pins[index % pins.Length];
        }

        private void EnsureCardCount(int count)
        {
            while (_cards.Count < count)
            {
                DeckCardView card = Instantiate(_config.CardPrefab, _cardsRoot);
                card.gameObject.SetActive(false);
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
                jitter = (Hash01(index) - 0.5f) * _config.JitterAmount;
            }

            return arc + jitter;
        }

        private static float Hash01(int index)
        {
            unchecked
            {
                uint x = (uint)(index * 374761393 + 17 * 668265263);
                x = (x ^ (x >> 13)) * 1274126177u;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
