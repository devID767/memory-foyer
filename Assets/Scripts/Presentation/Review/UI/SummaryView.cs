using System;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Presentation.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class SummaryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _reviewedLabel = null!;
        [SerializeField] private Button _returnButton = null!;
        [SerializeField] private UIButtonFeedback _returnFeedback = null!;
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        public event Action? ReturnRequested;

        private CanvasTransition _transition = null!;

        private void Awake()
        {
            _returnButton.onClick.AddListener(OnReturnClicked);
        }

        private void OnDestroy()
        {
            _returnButton.onClick.RemoveListener(OnReturnClicked);
        }

        public void Configure(UIAnimationConfig config)
        {
            _returnFeedback.Configure(config);
            _transition = new CanvasTransition(_canvasGroup, (RectTransform)transform, config);
        }

        public void Show(int reviewedCount)
        {
            _reviewedLabel.text = $"{reviewedCount} reviewed";
            _transition.FadeInAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void Hide()
        {
            _transition.Kill();
            gameObject.SetActive(false);
        }

        private void OnReturnClicked()
        {
            ReturnRequested?.Invoke();
        }
    }
}
