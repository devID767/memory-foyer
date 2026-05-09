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
        [SerializeField] private Sprite _faceSprite = null!; // set in Inspector
        [SerializeField] private Sprite _backSprite = null!; // set in Inspector

        [SerializeField] private string _statsFormat = "<size=140%><color=#B05A2A><b>{0}</b></color></size> <alpha=#88>due · {1} total";
        [SerializeField] private string _caughtUpLabel = "All caught up";

        public event Action<DeckId>? Clicked;

        private FoyerLayoutConfig? _config;
        private DeckId _currentId;
        private bool _bound;
        private float _restRotationZ;
        private Tween? _hoverTween;
        private bool _isPressed;
        private bool _isHovered;

        public void Configure(FoyerLayoutConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
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

        public void Bind(DeckButtonModel model, Sprite? icon)
        {
            _currentId = model.Id;
            _bound = true;

            bool interactable = model.DueCount > 0;
            _button.interactable = interactable;
            _paperImage.sprite = interactable ? _faceSprite : _backSprite;

            _nameLabel.text = model.DisplayName;
            _statsLabel.text = interactable
                ? string.Format(_statsFormat, model.DueCount, model.TotalCount)
                : _caughtUpLabel;

            // Title hidden on back-state; stats label doubles as the "All caught up" line.
            _nameLabel.gameObject.SetActive(interactable);

            bool showIcon = interactable && icon != null;
            _iconImage.gameObject.SetActive(showIcon);
            if (showIcon)
            {
                _iconImage.sprite = icon!;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable)
            {
                return;
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

            float baseScale = _isHovered ? _config.HoverScale : 1f;
            float targetScale = _isPressed ? baseScale * 0.94f : baseScale;
            float targetRotation = _isHovered ? 0f : _restRotationZ;

            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, targetRotation), duration).SetEase(Ease.OutQuad));

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
