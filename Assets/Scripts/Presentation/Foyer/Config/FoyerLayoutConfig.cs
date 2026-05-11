using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Foyer Layout Config", fileName = "FoyerLayoutConfig")]
    public sealed class FoyerLayoutConfig : ScriptableObject
    {
        [SerializeField] private DeckCardView _cardPrefab = null!; // set in Inspector
        [SerializeField, Min(0f)] private float _spacing = 50f;
        [SerializeField, Min(1)] private int _maxDecksToShow = 3;

        [Header("Card jitter")]
        [SerializeField, Range(0f, 10f)] private float _maxTiltDegrees = 3f;
        [SerializeField, Range(0f, 200f)] private float _arcHeight = 60f;
        [SerializeField, Range(0f, 30f)] private float _jitterAmount = 8f;

        [Header("Pin variants")]
        [SerializeField] private Sprite[] _pinVariants = System.Array.Empty<Sprite>();

        [Header("Card hover")]
        [SerializeField, Range(1f, 1.2f)] private float _hoverScale = 1.05f;
        [SerializeField, Range(0.05f, 0.5f)] private float _hoverDuration = 0.12f;
        [SerializeField, Range(0f, 40f)] private float _hoverLiftAmount = 12f;

        [Header("Card Press")]
        [SerializeField, Range(0.85f, 1f)] private float _pressScale = 0.94f;
        [SerializeField, Range(0.02f, 0.3f)] private float _pressDuration = 0.07f;
        [SerializeField, Range(0f, 1f)] private float _restedIconTintAmount = 0.4f;

        public DeckCardView CardPrefab => _cardPrefab;
        public float Spacing => _spacing;
        public int MaxDecksToShow => _maxDecksToShow;
        public float MaxTiltDegrees => _maxTiltDegrees;
        public float ArcHeight => _arcHeight;
        public float JitterAmount => _jitterAmount;
        public Sprite[] PinVariants => _pinVariants;
        public float HoverScale => _hoverScale;
        public float HoverDuration => _hoverDuration;
        public float HoverLiftAmount => _hoverLiftAmount;
        public float PressScale => _pressScale;
        public float PressDuration => _pressDuration;
        public float RestedIconTintAmount => _restedIconTintAmount;
    }
}
