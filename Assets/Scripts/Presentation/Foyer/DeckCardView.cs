using System;
using DG.Tweening;
using MemoryFoyer.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckCardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler
    {
        [SerializeField] private TMP_Text _nameLabel = null!; // set in Inspector
        [SerializeField] private TMP_Text _statsLabel = null!; // set in Inspector
        [SerializeField] private Image _iconImage = null!; // set in Inspector
        [SerializeField] private Image _paperImage = null!; // set in Inspector
        [SerializeField] private Image? _pinImage; // set in Inspector
        [SerializeField] private Sprite _faceSprite = null!; // set in Inspector
        [SerializeField] private Sprite _backSprite = null!; // set in Inspector

        private const string StatsFormat = "<size=200%><color=#{2}><b>{0}</b></color></size> <color=#{3}>due · of {1}</color>";
        private const string CaughtUpLabel = "All caught up";

        public event Action<DeckId>? Clicked;

        private FoyerLayoutConfig _config = null!;
        private ArtPaletteConfig _palette = null!;
        private string _accentHex = string.Empty;
        private string _restHex = string.Empty;
        private string _inkHex = string.Empty;
        private RectTransform _rectTransform = null!;

        private DeckId _currentId;
        private bool _interactable;
        private RestPose _rest;
        private bool _isPressed;
        private bool _isHovered;
        private Tween? _activeTween;

        private readonly struct RestPose
        {
            public Vector2 Position { get; }
            public float RotationZ { get; }

            public RestPose(Vector2 position, float rotationZ)
            {
                Position = position;
                RotationZ = rotationZ;
            }
        }

        public void Configure(FoyerLayoutConfig config, ArtPaletteConfig palette)
        {
            _config = config;
            _palette = palette;
            _accentHex = ColorUtility.ToHtmlStringRGB(palette.Accent);
            _restHex = ColorUtility.ToHtmlStringRGB(palette.Rest);
            _inkHex = ColorUtility.ToHtmlStringRGB(palette.Ink);
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }

        public void ApplyLayout(Vector2 restPosition, float restRotationZ, Sprite? pin)
        {
            _rest = new RestPose(restPosition, restRotationZ);

            _rectTransform.localPosition = new Vector3(restPosition.x, restPosition.y, 0f);
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, restRotationZ);
            _rectTransform.localScale = Vector3.one;

            if (_pinImage != null && pin != null)
            {
                _pinImage.sprite = pin;
            }

            _isHovered = false;
            _isPressed = false;
        }

        public void Bind(DeckButtonModel model, Sprite? icon)
        {
            _currentId = model.Id;
            _interactable = model.DueCount > 0;

            _paperImage.sprite = _interactable ? _faceSprite : _backSprite;

            _nameLabel.text = model.DisplayName;
            _statsLabel.text = _interactable
                ? string.Format(StatsFormat, model.DueCount, model.TotalCount, _accentHex, _inkHex)
                : $"<color=#{_restHex}>{CaughtUpLabel}</color>";

            _nameLabel.gameObject.SetActive(_interactable);

            bool showIcon = _interactable && icon != null;
            _iconImage.gameObject.SetActive(showIcon);
            if (showIcon)
            {
                _iconImage.sprite = icon!;
            }

            ApplyPaperTint();
            ApplyIconTint();
        }

        private void ApplyPaperTint()
        {
            Color targetPaper = _interactable ? _palette.Paper : _palette.PaperRested;
            targetPaper.a = _paperImage.color.a;
            _paperImage.color = targetPaper;
        }

        private void ApplyIconTint()
        {
            _iconImage.color = _interactable
                ? Color.white
                : Color.Lerp(Color.white, Color.gray, _config.RestedIconTintAmount);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }
            _isHovered = true;
            RefreshState(_config.HoverDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isHovered && !_isPressed)
            {
                return;
            }
            _isHovered = false;
            _isPressed = false;
            RefreshState(_config.HoverDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }
            _isPressed = true;
            RefreshState(_config.PressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                return;
            }
            _isPressed = false;
            RefreshState(_config.PressDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }
            Clicked?.Invoke(_currentId);
        }

        private void RefreshState(float duration)
        {
            _activeTween?.Kill();

            float baseScale = _isHovered ? _config.HoverScale : 1f;
            float targetScale = _isPressed ? baseScale * _config.PressScale : baseScale;
            float targetRotation = _isHovered ? 0f : _rest.RotationZ;
            float targetY = _rest.Position.y + (_isHovered ? _config.HoverLiftAmount : 0f);

            Sequence seq = DOTween.Sequence();
            seq.Join(_rectTransform.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            seq.Join(_rectTransform.DOLocalRotate(new Vector3(0f, 0f, targetRotation), duration).SetEase(Ease.OutQuad));
            seq.Join(_rectTransform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad));

            seq.Play();
            _activeTween = seq;
        }
    }
}
