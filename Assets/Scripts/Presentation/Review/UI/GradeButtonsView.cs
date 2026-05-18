using System;
using System.Collections.Generic;
using DG.Tweening;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class GradeButtonsView : MonoBehaviour
    {
        [SerializeField] private Button _againButton = null!;
        [SerializeField] private Button _hardButton = null!;
        [SerializeField] private Button _goodButton = null!;
        [SerializeField] private Button _easyButton = null!;
        [SerializeField] private UIButtonFeedback[] _buttonFeedback = Array.Empty<UIButtonFeedback>();

        public event Action<ReviewGrade>? GradeSubmitted;

        private UIAnimationConfig _config = null!;
        private Tween? _entranceTween;

        private void Awake()
        {
            _againButton.onClick.AddListener(OnAgainClicked);
            _hardButton.onClick.AddListener(OnHardClicked);
            _goodButton.onClick.AddListener(OnGoodClicked);
            _easyButton.onClick.AddListener(OnEasyClicked);
        }

        private void OnDestroy()
        {
            _entranceTween?.Kill();
            _againButton.onClick.RemoveListener(OnAgainClicked);
            _hardButton.onClick.RemoveListener(OnHardClicked);
            _goodButton.onClick.RemoveListener(OnGoodClicked);
            _easyButton.onClick.RemoveListener(OnEasyClicked);
        }

        public void Configure(UIAnimationConfig config)
        {
            _config = config;
            foreach (UIButtonFeedback feedback in _buttonFeedback)
            {
                feedback.Configure(config);
            }
        }

        public void Show()
        {
            _entranceTween?.Kill();
            gameObject.SetActive(true);

            List<RectTransform> buttons = new(4)
            {
                (RectTransform)_againButton.transform,
                (RectTransform)_hardButton.transform,
                (RectTransform)_goodButton.transform,
                (RectTransform)_easyButton.transform,
            };
            _entranceTween = new StaggeredFadeInAnimator(buttons, _config).BuildEntrance().Play();
        }

        public void Hide()
        {
            _entranceTween?.Kill();
            _againButton.transform.localScale = Vector3.one;
            _hardButton.transform.localScale = Vector3.one;
            _goodButton.transform.localScale = Vector3.one;
            _easyButton.transform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }

        private void OnAgainClicked()
        {
            GradeSubmitted?.Invoke(ReviewGrade.Again);
        }

        private void OnHardClicked()
        {
            GradeSubmitted?.Invoke(ReviewGrade.Hard);
        }

        private void OnGoodClicked()
        {
            GradeSubmitted?.Invoke(ReviewGrade.Good);
        }

        private void OnEasyClicked()
        {
            GradeSubmitted?.Invoke(ReviewGrade.Easy);
        }
    }
}
