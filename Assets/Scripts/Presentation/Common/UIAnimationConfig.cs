using DG.Tweening;
using UnityEngine;

namespace MemoryFoyer.Presentation.Common
{
    [CreateAssetMenu(menuName = "MemoryFoyer/UI Animation Config", fileName = "UIAnimationConfig")]
    public sealed class UIAnimationConfig : ScriptableObject
    {
        [Header("Screen transition")]
        [SerializeField, Range(0.05f, 0.5f)] private float _screenFadeInDuration = 0.2f;
        [SerializeField, Range(0.05f, 0.5f)] private float _screenFadeOutDuration = 0.15f;
        [SerializeField] private Ease _screenFadeEase = Ease.OutQuad;
        [SerializeField, Range(0.8f, 1f)] private float _screenStartScale = 0.97f;

        [Header("Staggered entrance")]
        [SerializeField, Range(0.01f, 0.2f)] private float _staggerDelay = 0.05f;
        [SerializeField, Range(0.05f, 0.5f)] private float _enterDuration = 0.22f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;
        [SerializeField, Range(0.1f, 1f)] private float _enterStartScale = 0.85f;

        [Header("Button feedback")]
        [SerializeField, Range(1f, 1.2f)] private float _buttonHoverScale = 1.06f;
        [SerializeField, Range(0.85f, 1f)] private float _buttonPressScale = 0.94f;
        [SerializeField, Range(0.05f, 0.3f)] private float _buttonHoverDuration = 0.1f;
        [SerializeField, Range(0.02f, 0.2f)] private float _buttonPressDuration = 0.06f;

        [Header("Review card deal / dismiss")]
        [SerializeField, Range(0.05f, 0.5f)] private float _cardDealDuration = 0.22f;
        [SerializeField] private Ease _cardDealEase = Ease.OutCubic;
        [SerializeField, Range(0f, 300f)] private float _cardDealOffsetY = 60f;
        [SerializeField, Range(0.05f, 0.5f)] private float _cardDismissDuration = 0.18f;
        [SerializeField] private Ease _cardDismissEase = Ease.InCubic;
        [SerializeField, Range(100f, 1500f)] private float _cardDismissOffsetX = 700f;
        [SerializeField, Range(100f, 1500f)] private float _cardDismissOffsetY = 900f;
        [SerializeField, Range(0f, 0.4f)] private float _cardAdvanceGapDuration = 0.07f;

        public float ScreenFadeInDuration => _screenFadeInDuration;
        public float ScreenFadeOutDuration => _screenFadeOutDuration;
        public Ease ScreenFadeEase => _screenFadeEase;
        public float ScreenStartScale => _screenStartScale;

        public float StaggerDelay => _staggerDelay;
        public float EnterDuration => _enterDuration;
        public Ease EnterEase => _enterEase;
        public float EnterStartScale => _enterStartScale;

        public float ButtonHoverScale => _buttonHoverScale;
        public float ButtonPressScale => _buttonPressScale;
        public float ButtonHoverDuration => _buttonHoverDuration;
        public float ButtonPressDuration => _buttonPressDuration;

        public float CardDealDuration => _cardDealDuration;
        public Ease CardDealEase => _cardDealEase;
        public float CardDealOffsetY => _cardDealOffsetY;
        public float CardDismissDuration => _cardDismissDuration;
        public Ease CardDismissEase => _cardDismissEase;
        public float CardDismissOffsetX => _cardDismissOffsetX;
        public float CardDismissOffsetY => _cardDismissOffsetY;
        public float CardAdvanceGapDuration => _cardAdvanceGapDuration;
    }
}
