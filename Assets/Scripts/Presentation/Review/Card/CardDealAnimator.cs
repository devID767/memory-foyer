using System;
using DG.Tweening;
using MemoryFoyer.Presentation.Common;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class CardDealAnimator
    {
        private readonly RectTransform _cardRoot;
        private readonly UIAnimationConfig _config;

        private readonly Vector2 _originAnchoredPos;
        private readonly Vector3 _originScale;

        public CardDealAnimator(RectTransform cardRoot, UIAnimationConfig config)
        {
            _cardRoot = cardRoot;
            _config = config;

            _originAnchoredPos = _cardRoot.anchoredPosition;
            _originScale = _cardRoot.localScale;
        }

        public Sequence BuildAdvance(Action onPivot, CardExitDirection exit)
        {
            Vector2 dismissTarget = DismissTarget(exit);
            Vector2 dealStart = _originAnchoredPos + new Vector2(0f, -_config.CardDealOffsetY);

            Sequence seq = DOTween.Sequence();
            seq.Append(_cardRoot.DOAnchorPos(dismissTarget, _config.CardDismissDuration).SetEase(_config.CardDismissEase));
            seq.AppendInterval(_config.CardAdvanceGapDuration);
            seq.AppendCallback(() =>
            {
                onPivot();
                _cardRoot.anchoredPosition = dealStart;
            });
            seq.Append(_cardRoot.DOAnchorPos(_originAnchoredPos, _config.CardDealDuration).SetEase(_config.CardDealEase));
            return seq;
        }

        public Sequence BuildDismiss(CardExitDirection exit)
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(_cardRoot.DOAnchorPos(DismissTarget(exit), _config.CardDismissDuration).SetEase(_config.CardDismissEase));
            return seq;
        }

        private Vector2 DismissTarget(CardExitDirection exit)
        {
            return exit == CardExitDirection.Down
                ? _originAnchoredPos + new Vector2(0f, -_config.CardDismissOffsetY)
                : _originAnchoredPos + new Vector2(_config.CardDismissOffsetX, 0f);
        }

        public void ResetTransform()
        {
            _cardRoot.anchoredPosition = _originAnchoredPos;
            _cardRoot.localScale = _originScale;
        }
    }
}
