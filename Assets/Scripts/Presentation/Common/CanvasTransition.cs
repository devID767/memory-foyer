using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace MemoryFoyer.Presentation.Common
{
    public sealed class CanvasTransition
    {
        private readonly CanvasGroup _canvasGroup;
        private readonly RectTransform _root;
        private readonly UIAnimationConfig _config;
        private readonly Vector3 _originScale;

        private Sequence? _active;

        public CanvasTransition(CanvasGroup canvasGroup, RectTransform root, UIAnimationConfig config)
        {
            _canvasGroup = canvasGroup;
            _root = root;
            _config = config;
            _originScale = root.localScale;
        }

        public async UniTask FadeInAsync(CancellationToken ct)
        {
            _active?.Kill();

            _canvasGroup.alpha = 0f;
            _root.localScale = _originScale * _config.ScreenStartScale;
            _root.gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _config.ScreenFadeInDuration).SetEase(_config.ScreenFadeEase))
                .Join(_root.DOScale(_originScale, _config.ScreenFadeInDuration).SetEase(_config.ScreenFadeEase));
            _active = seq;

            _ = seq.Play();
            try
            {
                await seq.ToUniTask(TweenCancelBehaviour.Kill, ct);
            }
            finally
            {
                _active = null;
            }
        }

        public void Kill()
        {
            _active?.Kill();
            _active = null;
        }
    }
}
