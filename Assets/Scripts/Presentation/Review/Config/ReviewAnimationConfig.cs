using DG.Tweening;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Review Animation Config", fileName = "ReviewAnimationConfig")]
    public sealed class ReviewAnimationConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float _flipDuration = 0.35f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private Ease _flipEase = Ease.InOutCubic;

        public float FadeDuration => _fadeDuration;
        public float FlipDuration => _flipDuration;
        public Ease FadeEase => _fadeEase;
        public Ease FlipEase => _flipEase;
    }
}
