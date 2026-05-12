using System;
using DG.Tweening;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class CardFlipAnimator
    {
        private readonly RectTransform _cardRoot;
        private readonly ReviewAnimationConfig _config;

        private readonly float _originScaleX;

        public CardFlipAnimator(RectTransform cardRoot, ReviewAnimationConfig config)
        {
            _cardRoot = cardRoot;
            _config = config;

            _originScaleX = _cardRoot.localScale.x;
        }

        public Sequence BuildFlip(Action onPivot)
        {
            float half = _config.FlipDuration * 0.5f;

            Sequence seq = DOTween.Sequence();
            seq.Append(_cardRoot.DOScaleX(0f, half).SetEase(_config.FlipEase));
            seq.AppendCallback(() => onPivot());
            seq.Append(_cardRoot.DOScaleX(_originScaleX, half).SetEase(_config.FlipEase));
            return seq;
        }

        public void ResetScale()
        {
            Vector3 scale = _cardRoot.localScale;
            scale.x = 1f;
            _cardRoot.localScale = scale;
        }
    }
}
