using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Foyer Layout Config", fileName = "FoyerLayoutConfig")]
    public sealed class FoyerLayoutConfig : ScriptableObject
    {
        [SerializeField] private DeckCardView _cardPrefab = null!; // set in Inspector
        [SerializeField, Min(0f)] private float _spacing = 0.25f;
        [SerializeField, Min(1)] private int _maxDecksToShow = 3;

        [Header("Card jitter")]
        [SerializeField, Range(0f, 10f)] private float _maxTiltDegrees = 3f;

        [Header("Card hover")]
        [SerializeField, Range(1f, 1.2f)] private float _hoverScale = 1.05f;
        [SerializeField, Range(0.05f, 0.5f)] private float _hoverDuration = 0.12f;

        public DeckCardView CardPrefab => _cardPrefab;
        public float Spacing => _spacing;
        public int MaxDecksToShow => _maxDecksToShow;
        public float MaxTiltDegrees => _maxTiltDegrees;
        public float HoverScale => _hoverScale;
        public float HoverDuration => _hoverDuration;
    }
}
