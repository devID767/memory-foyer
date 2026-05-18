using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MemoryFoyer.Presentation.Common
{
    public sealed class UIButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private UIAnimationConfig? _config;
        private RectTransform _rectTransform = null!;
        private Vector3 _restScale;
        private bool _isHovered;
        private bool _isPressed;
        private Tween? _activeTween;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _restScale = _rectTransform.localScale;
        }

        public void Configure(UIAnimationConfig config)
        {
            _config = config;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_config == null)
            {
                return;
            }

            float scale = 1f;
            float duration = _config.ButtonHoverDuration;
            if (_isHovered)
            {
                scale = _config.ButtonHoverScale;
            }
            if (_isPressed)
            {
                scale *= _config.ButtonPressScale;
                duration = _config.ButtonPressDuration;
            }

            _activeTween?.Kill();
            _activeTween = _rectTransform
                .DOScale(_restScale * scale, duration)
                .SetEase(Ease.OutQuad)
                .Play();
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }
    }
}
