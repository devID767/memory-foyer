using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace MemoryFoyer.Presentation.Common
{
    public sealed class StaggeredFadeInAnimator
    {
        private readonly IReadOnlyList<RectTransform> _targets;
        private readonly UIAnimationConfig _config;

        public StaggeredFadeInAnimator(IReadOnlyList<RectTransform> targets, UIAnimationConfig config)
        {
            _targets = targets;
            _config = config;
        }

        public Sequence BuildEntrance()
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 0; i < _targets.Count; i++)
            {
                RectTransform target = _targets[i];
                Vector3 restScale = target.localScale;

                target.localScale = restScale * _config.EnterStartScale;

                float at = i * _config.StaggerDelay;
                seq.Insert(at, target.DOScale(restScale, _config.EnterDuration).SetEase(_config.EnterEase));
            }
            return seq;
        }
    }
}
