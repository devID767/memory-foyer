using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Scheduling;
using MemoryFoyer.Presentation.Common;
using UnityEngine;
using VContainer;

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
        [SerializeField] private CanvasGroup _canvasGroup = null!; // set in Inspector — on _canvasRoot
        [SerializeField] private RectTransform _canvasRect = null!; // set in Inspector — on _canvasRoot
        [SerializeField] private ReviewCardView _card = null!; // set in Inspector
        [SerializeField] private GradeButtonsView _grades = null!; // set in Inspector
        [SerializeField] private SummaryView _summary = null!; // set in Inspector
        [SerializeField] private TopStripView _topStrip = null!; // set in Inspector

        public event Action? RevealRequested;
        public event Action<ReviewGrade>? GradeSubmitted;
        public event Action? ReturnRequested;

        private CanvasTransition _transition = null!;

        [Inject]
        public void Construct(UIAnimationConfig uiConfig)
        {
            _transition = new CanvasTransition(_canvasGroup, _canvasRect, uiConfig);
            _grades.Configure(uiConfig);
            _summary.Configure(uiConfig);
        }

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
            _transition.FadeInAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void Hide()
        {
            _transition.Kill();
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

        public UniTask AdvanceToNextCardAsync(FrontFaceData next, CardExitDirection exit, CancellationToken ct)
        {
            return _card.AdvanceToNextCardAsync(next, exit, ct);
        }

        public UniTask HideCardAsync(CancellationToken ct)
        {
            return _card.HideAsync(ct);
        }

        public UniTask DismissCardAsync(CardExitDirection exit, CancellationToken ct)
        {
            return _card.DismissAsync(exit, ct);
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
