using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Scheduling;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    /// <summary>
    /// Aggregator for all review leaf views. Forwards leaf events upward and proxies
    /// presenter calls downward to the appropriate leaf.
    ///
    /// Forwarder rule exception: <see cref="Show"/> synchronously resets stale leaf
    /// visibility (hides grade buttons and summary) before activating the canvas. This
    /// prevents a previous session's grade buttons or summary from flashing on a
    /// re-entered session. Show/Hide are the screen's own canvas-visibility
    /// responsibility and may NOT await leaf operations or call into services.
    /// </summary>
    public sealed class ReviewScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _canvasRoot = null!; // set in Inspector
        [SerializeField] private ReviewCardView _card = null!; // set in Inspector
        [SerializeField] private GradeButtonsView _grades = null!; // set in Inspector
        [SerializeField] private SummaryView _summary = null!; // set in Inspector
        [SerializeField] private TopStripView _topStrip = null!; // set in Inspector

        public event Action? RevealRequested;
        public event Action<ReviewGrade>? GradeSubmitted;
        public event Action? ReturnRequested;

        private void Awake()
        {
            _card.RevealRequested += OnRevealRequested;
            _grades.GradeSubmitted += OnGradeSubmitted;
            _summary.ReturnRequested += OnReturnRequested;
        }

        private void OnDestroy()
        {
            _card.RevealRequested -= OnRevealRequested;
            _grades.GradeSubmitted -= OnGradeSubmitted;
            _summary.ReturnRequested -= OnReturnRequested;
        }

        public void Show()
        {
            // Reset stale leaf visibility so a re-entered session starts from a clean
            // baseline — documented exception to the forwarder rule (see class summary).
            _summary.Hide();
            _grades.Hide();
            _canvasRoot.SetActive(true);
        }

        public void Hide()
        {
            _canvasRoot.SetActive(false);
        }

        public void SetDeckName(string name)
        {
            _topStrip.SetDeckName(name);
        }

        public void SetProgress(int current, int total)
        {
            _topStrip.SetProgress(current, total);
        }

        public UniTask ShowCardAsync(FrontFaceData first, CancellationToken ct)
        {
            _topStrip.gameObject.SetActive(true);
            _summary.Hide();
            _grades.Hide();
            return _card.ShowAsync(first, ct);
        }

        public UniTask RevealBackAsync(BackFaceData back, CancellationToken ct)
        {
            return _card.RevealBackAsync(back, ct);
        }

        public UniTask AdvanceToNextCardAsync(FrontFaceData next, CancellationToken ct)
        {
            return _card.AdvanceToNextCardAsync(next, ct);
        }

        public UniTask HideCardAsync(CancellationToken ct)
        {
            return _card.HideAsync(ct);
        }

        public void ShowGrades()
        {
            _grades.Show();
        }

        public void HideGrades()
        {
            _grades.Hide();
        }

        public void ShowSummary(int reviewedCount)
        {
            _topStrip.gameObject.SetActive(false);
            _grades.Hide();
            _summary.Show(reviewedCount);
        }

        private void OnRevealRequested()
        {
            RevealRequested?.Invoke();
        }

        private void OnGradeSubmitted(ReviewGrade grade)
        {
            GradeSubmitted?.Invoke(grade);
        }

        private void OnReturnRequested()
        {
            ReturnRequested?.Invoke();
        }
    }
}
