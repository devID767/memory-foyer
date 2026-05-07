using System;
using DG.Tweening;
using MemoryFoyer.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _button = null!; // set in Inspector
        [SerializeField] private TMP_Text _nameLabel = null!; // set in Inspector
        [SerializeField] private TMP_Text _statsLabel = null!; // set in Inspector
        [SerializeField] private Image _iconImage = null!; // set in Inspector
        [SerializeField] private Image _paperImage = null!; // set in Inspector
        [SerializeField] private Sprite _faceSprite = null!; // set in Inspector
        [SerializeField] private Sprite _backSprite = null!; // set in Inspector
        [SerializeField] private Shadow? _dropShadow;

        [SerializeField] private string _statsFormat = "<size=140%><color=#B05A2A><b>{0}</b></color></size> <alpha=#88>due · {1} total";
        [SerializeField] private string _caughtUpLabel = "All caught up";

        public event Action<DeckId>? Clicked;

        private FoyerLayoutConfig? _config;
        private DeckId _currentId;
        private bool _bound;
        private float _restRotationZ;
        private Vector2 _baseShadowDistance;
        private Tween? _hoverTween;

        public void Configure(FoyerLayoutConfig config)
        {
            _config = config;
        }

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
            if (_dropShadow != null)
            {
                _baseShadowDistance = _dropShadow.effectDistance;
            }
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
            AnimateHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateHover(false);
        }

        private void AnimateHover(bool hovered)
        {
            if (_config == null)
            {
                return;
            }

            _hoverTween?.Kill();

            float duration = _config.HoverDuration;
            float targetScale = hovered ? _config.HoverScale : 1f;
            float targetRotation = hovered ? 0f : _restRotationZ;

            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, targetRotation), duration).SetEase(Ease.OutQuad));

            if (_dropShadow != null)
            {
                Vector2 shadowTarget = hovered ? _baseShadowDistance * 2f : _baseShadowDistance;
                seq.Join(DOTween.To(() => _dropShadow.effectDistance, v => _dropShadow.effectDistance = v, shadowTarget, duration));
            }

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
