using System;
using UnityEngine;
using Slafurry.System.InputHub;
using Slafurry.System.Audio;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects the closest interactable within range and triggers it when
    /// the Interact input is pressed. Plain IInteractable targets fire
    /// immediately on tap. IHoldInteractable targets require the input to
    /// be held for their HoldDuration, with progress reported each frame
    /// and cancellation handled if released early or the target changes.
    /// Attach to the player root; assign a child Transform (InteractCheck)
    /// as the detection origin.
    /// </summary>
    public class PlayerInteract : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform interactPoint;

        [Header("Settings")]
        [SerializeField] private float interactRadius = 1.2f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[8];

        private IHoldInteractable _holdTarget;
        private float _holdElapsed;

        public IInteractable CurrentTarget { get; private set; }
        public bool IsHolding => _holdTarget != null;

        /// <summary>Fired the instant an interaction actually resolves — an immediate tap, or a hold reaching 100%. Used by PlayerAnimationStateMachine to trigger the Interact/Crouch_Interact clips.</summary>
        public event Action OnInteracted;

        private void Start()
        {
            Controls.OnInteractPressed += HandleInteractPressed;
            Controls.OnInteractReleased += HandleInteractReleased;
        }

        private void OnDisable()
        {
            Controls.OnInteractPressed -= HandleInteractPressed;
            Controls.OnInteractReleased -= HandleInteractReleased;
        }

        private void FixedUpdate()
        {
            IInteractable previousTarget = CurrentTarget;
            CurrentTarget = FindClosestInteractable();

            // Target changed out from under an active hold (moved away,
            // something closer took priority) — cancel it rather than let
            // it silently keep progressing toward the old target.
            if (_holdTarget != null && !ReferenceEquals(CurrentTarget, previousTarget))
                CancelHold();

            if (_holdTarget != null)
                TickHold();
        }

        private void TickHold()
        {
            _holdElapsed += Time.fixedDeltaTime;
            float duration = Mathf.Max(_holdTarget.HoldDuration, 0.01f);
            float progress = Mathf.Clamp01(_holdElapsed / duration);

            _holdTarget.OnHoldProgress(progress);

            if (progress >= 1f)
            {
                var completed = _holdTarget;
                _holdTarget = null;
                completed.Interact();
                Audio.PlaySFX2D(PlayerSFX.Category, PlayerSFX.Interact);
                OnInteracted?.Invoke();
            }
        }

        private void CancelHold()
        {
            _holdTarget.OnHoldCanceled();
            _holdTarget = null;
        }

        private IInteractable FindClosestInteractable()
        {
            int count = Physics2D.OverlapCircleNonAlloc(interactPoint.position, interactRadius, _overlapBuffer, interactableLayer);

            IInteractable closest = null;
            float closestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (!_overlapBuffer[i].TryGetComponent(out IInteractable interactable))
                    continue;

                float distSqr = ((Vector2)_overlapBuffer[i].transform.position - (Vector2)interactPoint.position).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    closest = interactable;
                }
            }

            return closest;
        }

        private void HandleInteractPressed()
        {
            if (CurrentTarget == null) return;

            if (CurrentTarget is IHoldInteractable holdInteractable)
            {
                _holdTarget = holdInteractable;
                _holdElapsed = 0f;
            }
            else
            {
                CurrentTarget.Interact();
                Audio.PlaySFX2D(PlayerSFX.Category, PlayerSFX.Interact);
                OnInteracted?.Invoke();
            }
        }

        private void HandleInteractReleased()
        {
            if (_holdTarget != null)
                CancelHold();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || interactPoint == null) return;

            Gizmos.color = CurrentTarget != null ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(interactPoint.position, interactRadius);
        }
    }
}