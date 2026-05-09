using System;
using DG.Tweening;
using MemoryFoyer.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button _button = null!; // set in Inspector
        [SerializeField] private TMP_Text _nameLabel = null!; // set in Inspector
        [SerializeField] private TMP_Text _statsLabel = null!; // set in Inspector
        [SerializeField] private Image _iconImage = null!; // set in Inspector
        [SerializeField] private Image _paperImage = null!; // set in Inspector
        [SerializeField] private Image? _pinImage; // set in Inspector
        [SerializeField] private Sprite _faceSprite = null!; // set in Inspector
        [SerializeField] private Sprite _backSprite = null!; // set in Inspector

        [SerializeField] private string _statsFormat = "<size=140%><color=#{2}><b>{0}</b></color></size> <alpha=#88>due · {1} total";
        [SerializeField] private string _caughtUpLabel = "All caught up";

        private const float RestedIconLerp = 0.4f;

        public event Action<DeckId>? Clicked;

        private FoyerLayoutConfig? _config;
        private ArtPaletteConfig? _palette;
        private string _accentHex = "B05A2A";
        private string _restHex = "6E6657";
        private DeckId _currentId;
        private bool _bound;
        private bool _isInteractable;
        private float _restRotationZ;
        private float _restPositionY;
        private bool _restPositionCaptured;
        private Tween? _hoverTween;
        private RectTransform _rectTransform = null!;
        private bool _isPressed;
        private bool _isHovered;

        public void Configure(FoyerLayoutConfig config, ArtPaletteConfig palette)
        {
            _config = config;
            _palette = palette;
            _accentHex = ColorUtility.ToHtmlStringRGB(palette.Accent);
            _restHex = ColorUtility.ToHtmlStringRGB(palette.Rest);
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
            _hoverTween?.Kill();
        }

        public void SetRestRotation(float zDegrees)
        {
            _restRotationZ = zDegrees;
        }

        public void ResetRestPositionCapture()
        {
            _restPositionCaptured = false;
        }

        public void SetPin(Sprite pinSprite)
        {
            if (_pinImage == null)
            {
                return;
            }
            _pinImage.sprite = pinSprite;
        }

        public void Bind(DeckButtonModel model, Sprite? icon)
        {
            _currentId = model.Id;
            _bound = true;

            bool interactable = model.DueCount > 0;
            _isInteractable = interactable;
            _button.interactable = interactable;
            _paperImage.sprite = interactable ? _faceSprite : _backSprite;

            _nameLabel.text = model.DisplayName;
            _statsLabel.text = interactable
                ? string.Format(_statsFormat, model.DueCount, model.TotalCount, _accentHex)
                : $"<color=#{_restHex}>{_caughtUpLabel}</color>";

            _nameLabel.gameObject.SetActive(interactable);

            bool showIcon = interactable && icon != null;
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
            if (_palette == null)
            {
                return;
            }

            Color targetPaper = _isInteractable ? _palette.Paper : _palette.PaperRested;

            targetPaper.a = _paperImage.color.a;
            _paperImage.color = targetPaper;
        }

        private void ApplyIconTint()
        {
            if (_iconImage == null)
            {
                return;
            }

            _iconImage.color = _isInteractable
                ? Color.white
                : Color.Lerp(Color.white, Color.gray, RestedIconLerp);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable)
            {
                return;
            }
            if (!_restPositionCaptured)
            {
                _restPositionY = _rectTransform.localPosition.y;
                _restPositionCaptured = true;
            }
            _isHovered = true;
            RefreshState(_config != null ? _config.HoverDuration : 0.12f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshState(_config != null ? _config.HoverDuration : 0.12f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable)
            {
                return;
            }
            _isPressed = true;
            RefreshState(0.07f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                return;
            }
            _isPressed = false;
            RefreshState(0.12f);
        }

        private void RefreshState(float duration)
        {
            if (_config == null)
            {
                return;
            }

            _hoverTween?.Kill();
            _rectTransform.DOKill();

            float baseScale = _isHovered ? _config.HoverScale : 1f;
            float targetScale = _isPressed ? baseScale * 0.94f : baseScale;
            float targetRotation = _isHovered ? 0f : _restRotationZ;
            float targetY = _restPositionY + (_isHovered ? _config.HoverLiftAmount : 0f);

            Sequence seq = DOTween.Sequence();
            seq.Join(_rectTransform.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            seq.Join(_rectTransform.DOLocalRotate(new Vector3(0f, 0f, targetRotation), duration).SetEase(Ease.OutQuad));
            seq.Join(_rectTransform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad));

            seq.Play();
            _hoverTween = seq;
        }

        private void OnButtonClicked()
        {
            if (!_bound)
            {
                return;
            }

            Clicked?.Invoke(_currentId);
        }
    }
}
