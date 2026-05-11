using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class SummaryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _reviewedLabel = null!;
        [SerializeField] private Button _returnButton = null!;

        public event Action? ReturnRequested;

        private void Awake()
        {
            _returnButton.onClick.AddListener(OnReturnClicked);
        }

        private void OnDestroy()
        {
            _returnButton.onClick.RemoveListener(OnReturnClicked);
        }

        public void Show(int reviewedCount)
        {
            _reviewedLabel.text = $"{reviewedCount} reviewed";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnReturnClicked()
        {
            ReturnRequested?.Invoke();
        }
    }
}
