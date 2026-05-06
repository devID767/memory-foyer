using System;
using MemoryFoyer.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button = null!; // set in Inspector
        [SerializeField] private TMP_Text _nameLabel = null!; // set in Inspector
        [SerializeField] private TMP_Text _statsLabel = null!; // set in Inspector
        [SerializeField] private Image _disabledOverlay = null!; // set in Inspector

        [SerializeField] private string _statsFormat = "{0} due · {1} total";
        [SerializeField] private string _caughtUpLabel = "All caught up";
        [SerializeField, Range(0f, 1f)] private float _disabledOverlayAlpha = 0.4f;

        public event Action<DeckId>? Clicked;

        private DeckId _currentId;
        private bool _bound;

        private void Awake()
        {
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnButtonClicked);
        }

        public void Bind(DeckButtonModel model)
        {
            _currentId = model.Id;
            _bound = true;

            _nameLabel.text = model.DisplayName;

            bool interactable = model.DueCount > 0;
            _button.interactable = interactable;

            _statsLabel.text = interactable
                ? string.Format(_statsFormat, model.DueCount, model.TotalCount)
                : _caughtUpLabel;

            Color overlayColor = _disabledOverlay.color;
            overlayColor.a = interactable ? 0f : _disabledOverlayAlpha;
            _disabledOverlay.color = overlayColor;
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
