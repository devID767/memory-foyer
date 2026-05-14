using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Banners
{
    public sealed class LoadingView : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null!;             // set in Inspector
        [SerializeField] private TMP_Text _label = null!;              // set in Inspector
        [SerializeField] private string _baseText = "Loading";
        [SerializeField] private float _showDelaySeconds = 0.05f;
        [SerializeField] private float _dotIntervalSeconds = 0.35f;
        [SerializeField] private int _maxDots = 3;

        private Tween? _delayedShow;
        private Sequence? _animation;
        private Action? _pendingOnHidden;
        private int _dots;

        private void Awake()
        {
            _root.SetActive(false);
            _label.text = _baseText;
        }

        private void OnDestroy()
        {
            _delayedShow?.Kill();
            _animation?.Kill();
            _delayedShow = null;
            _animation = null;
        }

        public void Show(Action? onAfterHidden = null)
        {
            _pendingOnHidden = onAfterHidden;

            // Already running (delay window or animating) — keep state, only swap the callback.
            // Prevents a re-trigger gap when one async stage hands off to the next.
            if (_delayedShow != null || _animation != null)
            {
                return;
            }

            // Delayed activation: if Hide() arrives within _showDelaySeconds, the view
            // never visually appears — prevents a blink on fast async operations.
            Tween delay = DOVirtual.DelayedCall(_showDelaySeconds, ActivateAndAnimate, ignoreTimeScale: true);
            _delayedShow = delay;
            _ = delay.Play();
        }

        public void Hide(bool runCallback = true)
        {
            _delayedShow?.Kill();
            _animation?.Kill();
            _delayedShow = null;
            _animation = null;
            _root.SetActive(false);

            Action? cb = _pendingOnHidden;
            _pendingOnHidden = null;
            if (runCallback)
            {
                cb?.Invoke();
            }
        }

        private void ActivateAndAnimate()
        {
            _delayedShow = null;
            _dots = 0;
            UpdateLabel();
            _root.SetActive(true);

            Sequence seq = DOTween.Sequence()
                .AppendInterval(_dotIntervalSeconds)
                .AppendCallback(AdvanceDots)
                .SetLoops(-1)
                .SetUpdate(true); // unscaled time — animates regardless of Time.timeScale.
            _animation = seq;
            _ = seq.Play();
        }

        private void AdvanceDots()
        {
            _dots = (_dots % _maxDots) + 1;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            _label.text = _baseText + new string('.', _dots);
        }
    }
}
