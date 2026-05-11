using System;
using MemoryFoyer.Domain.Scheduling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class InputSystemReviewInputSource : MonoBehaviour, IReviewInputSource
    {
        [SerializeField] private InputActionReference _revealAction = null!;
        [SerializeField] private InputActionReference _grade1Action = null!;
        [SerializeField] private InputActionReference _grade2Action = null!;
        [SerializeField] private InputActionReference _grade3Action = null!;
        [SerializeField] private InputActionReference _grade4Action = null!;
        [SerializeField] private InputActionReference _closeAction = null!;

        public event Action? RevealPressed;
        public event Action<ReviewGrade>? GradePressed;
        public event Action? ClosePressed;

        private void OnEnable()
        {
            EnableAndSubscribe(_revealAction, OnReveal);
            EnableAndSubscribe(_grade1Action, OnGrade1);
            EnableAndSubscribe(_grade2Action, OnGrade2);
            EnableAndSubscribe(_grade3Action, OnGrade3);
            EnableAndSubscribe(_grade4Action, OnGrade4);
            EnableAndSubscribe(_closeAction, OnClose);
        }

        private void OnDisable()
        {
            DisableAndUnsubscribe(_revealAction, OnReveal);
            DisableAndUnsubscribe(_grade1Action, OnGrade1);
            DisableAndUnsubscribe(_grade2Action, OnGrade2);
            DisableAndUnsubscribe(_grade3Action, OnGrade3);
            DisableAndUnsubscribe(_grade4Action, OnGrade4);
            DisableAndUnsubscribe(_closeAction, OnClose);
        }

        private static void EnableAndSubscribe(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || actionRef.action == null)
            {
                return;
            }
            actionRef.action.Enable();
            actionRef.action.performed += callback;
        }

        private static void DisableAndUnsubscribe(InputActionReference actionRef, Action<InputAction.CallbackContext> callback)
        {
            if (actionRef == null || actionRef.action == null)
            {
                return;
            }
            actionRef.action.performed -= callback;
            actionRef.action.Disable();
        }

        private void OnReveal(InputAction.CallbackContext ctx)
        {
            RevealPressed?.Invoke();
        }

        private void OnGrade1(InputAction.CallbackContext ctx)
        {
            GradePressed?.Invoke(ReviewGrade.Again);
        }

        private void OnGrade2(InputAction.CallbackContext ctx)
        {
            GradePressed?.Invoke(ReviewGrade.Hard);
        }

        private void OnGrade3(InputAction.CallbackContext ctx)
        {
            GradePressed?.Invoke(ReviewGrade.Good);
        }

        private void OnGrade4(InputAction.CallbackContext ctx)
        {
            GradePressed?.Invoke(ReviewGrade.Easy);
        }

        private void OnClose(InputAction.CallbackContext ctx)
        {
            ClosePressed?.Invoke();
        }
    }
}
