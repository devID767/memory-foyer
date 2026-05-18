using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MemoryFoyer.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryFoyer.Presentation.Review
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReviewCardView : MonoBehaviour
    {
        private enum ReviewViewState
        {
            Hidden,
            FaceDown,
            Revealed,
            Animating,
        }

        [SerializeField] private FrontFaceView _frontFace = null!;
        [SerializeField] private BackFaceView _backFace = null!;
        [SerializeField] private Button _cardTapButton = null!;
        [SerializeField] private ReviewAnimationConfig _animationConfig = null!;
        [SerializeField] private UIAnimationConfig _uiAnimationConfig = null!;

        public event Action? RevealRequested;

        private ReviewViewState _state = ReviewViewState.Hidden;
        private Tween? _activeTween;
        private CardFlipAnimator _flipAnimator = null!;
        private CardDealAnimator _dealAnimator = null!;
        private RectTransform _rectTransform = null!;
        private CancellationTokenSource _lifetimeCts = null!;

        private void Awake()
        {
            _lifetimeCts = new CancellationTokenSource();
            _rectTransform = (RectTransform)transform;
            _flipAnimator = new CardFlipAnimator(_rectTransform, _animationConfig);
            _dealAnimator = new CardDealAnimator(_rectTransform, _uiAnimationConfig);
            _cardTapButton.onClick.AddListener(OnCardTapped);
        }

        private void OnValidate()
        {
            if (_frontFace == null)
            {
                Debug.LogError($"[ReviewView] '{name}': _frontFace is not assigned", this);
            }
            if (_backFace == null)
            {
                Debug.LogError($"[ReviewView] '{name}': _backFace is not assigned", this);
            }
            if (_cardTapButton == null)
            {
                Debug.LogError($"[ReviewView] '{name}': _cardTapButton is not assigned", this);
            }
            if (_animationConfig == null)
            {
                Debug.LogError($"[ReviewView] '{name}': _animationConfig is not assigned", this);
            }
            if (_uiAnimationConfig == null)
            {
                Debug.LogError($"[ReviewView] '{name}': _uiAnimationConfig is not assigned", this);
            }
        }

        private void OnDisable()
        {
            _activeTween?.Kill();
            _activeTween = null;
            if (_state == ReviewViewState.Hidden)
            {
                return;
            }
            _frontFace.SetVisible(false);
            _backFace.SetVisible(false);
            SetState(ReviewViewState.Hidden);
        }

        private void OnDestroy()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _activeTween?.Kill();
            _cardTapButton.onClick.RemoveListener(OnCardTapped);
        }

        public UniTask ShowAsync(FrontFaceData firstCard, CancellationToken ct)
        {
            if (_state != ReviewViewState.Hidden)
            {
                Debug.LogWarning($"[ReviewView] illegal Show from {_state}");
                return UniTask.CompletedTask;
            }
            _activeTween?.Kill();
            _activeTween = null;
            gameObject.SetActive(true);
            _frontFace.Bind(firstCard);
            _frontFace.SetVisible(true);
            _backFace.SetVisible(false);
            SetState(ReviewViewState.FaceDown);
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CancellationToken ct)
        {
            if (_state == ReviewViewState.Hidden)
            {
                return UniTask.CompletedTask;
            }
            _activeTween?.Kill();
            _activeTween = null;
            _frontFace.SetVisible(false);
            _backFace.SetVisible(false);
            SetState(ReviewViewState.Hidden);
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public async UniTask RevealBackAsync(BackFaceData data, CancellationToken ct)
        {
            if (!TryStartAnimation(ReviewViewState.FaceDown, out ReviewViewState rollback))
            {
                return;
            }

            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);
            CancellationToken linkedCt = linked.Token;

            Sequence seq = _flipAnimator.BuildFlip(onPivot: () =>
            {
                _frontFace.SetVisible(false);
                _backFace.Bind(data);
                _backFace.SetVisible(true);
            });
            await RunTrackedAsync(seq, linkedCt, onCancel: () =>
            {
                _flipAnimator.ResetScale();
                _backFace.SetVisible(false);
                _frontFace.SetVisible(true);
                SetState(rollback);
            });

            linkedCt.ThrowIfCancellationRequested();

            SetState(ReviewViewState.Revealed);
        }

        public async UniTask AdvanceToNextCardAsync(FrontFaceData data, CardExitDirection exit, CancellationToken ct)
        {
            if (!TryStartAnimation(ReviewViewState.Revealed, out ReviewViewState rollback))
            {
                return;
            }

            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);
            CancellationToken linkedCt = linked.Token;

            Sequence seq = _dealAnimator.BuildAdvance(onPivot: () =>
            {
                _backFace.SetVisible(false);
                _frontFace.Bind(data);
                _frontFace.SetVisible(true);
            }, exit);
            await RunTrackedAsync(seq, linkedCt, onCancel: () =>
            {
                _dealAnimator.ResetTransform();
                _frontFace.SetVisible(false);
                _backFace.SetVisible(true);
                SetState(rollback);
            });

            linkedCt.ThrowIfCancellationRequested();

            SetState(ReviewViewState.FaceDown);
        }

        public async UniTask DismissAsync(CardExitDirection exit, CancellationToken ct)
        {
            if (_state == ReviewViewState.Hidden)
            {
                return;
            }
            if (!TryStartDismiss(out ReviewViewState rollback))
            {
                return;
            }

            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);
            CancellationToken linkedCt = linked.Token;

            Sequence seq = _dealAnimator.BuildDismiss(exit);
            await RunTrackedAsync(seq, linkedCt, onCancel: () =>
            {
                _dealAnimator.ResetTransform();
                _frontFace.SetVisible(rollback == ReviewViewState.FaceDown);
                _backFace.SetVisible(rollback == ReviewViewState.Revealed);
                SetState(rollback);
            });

            linkedCt.ThrowIfCancellationRequested();

            _frontFace.SetVisible(false);
            _backFace.SetVisible(false);
            _dealAnimator.ResetTransform();
            SetState(ReviewViewState.Hidden);
            gameObject.SetActive(false);
        }

        private bool TryStartDismiss(out ReviewViewState rollback)
        {
            if (_state != ReviewViewState.Revealed && _state != ReviewViewState.FaceDown)
            {
                Debug.LogWarning($"[ReviewView] illegal transition {_state}→Animating (expected Revealed or FaceDown)");
                rollback = _state;
                return false;
            }
            rollback = _state;
            SetState(ReviewViewState.Animating);
            return true;
        }

        private bool TryStartAnimation(ReviewViewState expected, out ReviewViewState rollback)
        {
            if (_state != expected)
            {
                Debug.LogWarning($"[ReviewView] illegal transition {_state}→Animating (expected from {expected})");
                rollback = _state;
                return false;
            }
            rollback = expected;
            SetState(ReviewViewState.Animating);
            return true;
        }

        private async UniTask RunTrackedAsync(Tween tween, CancellationToken ct, Action? onCancel = null)
        {
            _activeTween?.Kill();
            _activeTween = tween;
            _ = tween.Play();

            bool cancelled = await tween
                .ToUniTask(TweenCancelBehaviour.Kill, ct)
                .SuppressCancellationThrow();
            _activeTween = null;

            if (cancelled)
            {
                onCancel?.Invoke();
            }
        }

        private void SetState(ReviewViewState state)
        {
            _state = state;
            _cardTapButton.interactable = state == ReviewViewState.FaceDown;
        }

        private void OnCardTapped()
        {
            if (_state != ReviewViewState.FaceDown)
            {
                return;
            }
            RevealRequested?.Invoke();
        }
    }
}
