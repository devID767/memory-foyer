using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Banners
{
    public sealed class ErrorBannerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root = null!;          // set in Inspector — the rotated stamp container
        [SerializeField] private CanvasGroup _canvasGroup = null!;     // set in Inspector — on _root
        [SerializeField] private TMP_Text _headlineLabel = null!;      // set in Inspector — UPPERCASE in TMP component
        [SerializeField] private TMP_Text _secondaryLabel = null!;     // set in Inspector — italic in TMP component

        [SerializeField] private float _showDurationSec = 0.12f;
        [SerializeField] private float _hideDurationSec = 0.18f;
        [SerializeField] private float _showStartScale = 1.15f;
        [SerializeField] private float _hideRiseDistance = 8f;

        private Vector2 _rootAnchoredOrigin;
        private Sequence? _activeTween;

        private void Awake()
        {
            _rootAnchoredOrigin = _root.anchoredPosition;
            _canvasGroup.alpha = 0f;
            _root.gameObject.SetActive(false);
        }

        public async UniTask Show(string headline, string secondary, CancellationToken ct = default)
        {
            _headlineLabel.text = headline;
            _secondaryLabel.text = secondary;

            _activeTween?.Kill();

            _root.anchoredPosition = _rootAnchoredOrigin;
            _root.localScale = new Vector3(_showStartScale, _showStartScale, 1f);
            _canvasGroup.alpha = 0f;
            _root.gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _showDurationSec).SetEase(Ease.OutQuad))
                .Join(_root.DOScale(1f, _showDurationSec).SetEase(Ease.OutBack));
            _activeTween = seq;

            _ = seq.Play();
            await seq.ToUniTask(TweenCancelBehaviour.Kill, ct);
            _activeTween = null;
        }

        public async UniTask Hide(CancellationToken ct = default)
        {
            if (!_root.gameObject.activeSelf)
            {
                return;
            }

            _activeTween?.Kill();

            float riseTargetY = _rootAnchoredOrigin.y + _hideRiseDistance;
            Sequence seq = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, _hideDurationSec).SetEase(Ease.InQuad))
                .Join(_root.DOAnchorPosY(riseTargetY, _hideDurationSec).SetEase(Ease.InQuad));
            _activeTween = seq;

            _ = seq.Play();
            try
            {
                await seq.ToUniTask(TweenCancelBehaviour.Kill, ct);
            }
            finally
            {
                _activeTween = null;
                _root.gameObject.SetActive(false);
                _root.anchoredPosition = _rootAnchoredOrigin;
                _canvasGroup.alpha = 0f;
            }
        }
    }
}
