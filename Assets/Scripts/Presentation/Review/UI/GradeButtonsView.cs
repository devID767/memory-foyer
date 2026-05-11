using System;
using MemoryFoyer.Domain.Scheduling;
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

        public event Action<ReviewGrade>? GradeSubmitted;

        private void Awake()
        {
            _againButton.onClick.AddListener(OnAgainClicked);
            _hardButton.onClick.AddListener(OnHardClicked);
            _goodButton.onClick.AddListener(OnGoodClicked);
            _easyButton.onClick.AddListener(OnEasyClicked);
        }

        private void OnDestroy()
        {
            _againButton.onClick.RemoveListener(OnAgainClicked);
            _hardButton.onClick.RemoveListener(OnHardClicked);
            _goodButton.onClick.RemoveListener(OnGoodClicked);
            _easyButton.onClick.RemoveListener(OnEasyClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
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
