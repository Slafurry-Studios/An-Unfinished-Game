using System;
using TMPro;
using UnityEngine;
using Slafurry.System.InputHub;
using Slafurry.System.Audio;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects the closest interactable within range and triggers it when
    /// the Interact input is pressed.
    ///
    /// Plain IInteractable targets fire immediately on tap.
    /// IHoldInteractable targets require the input to be held for their
    /// HoldDuration, with progress reported each frame.
    ///
    /// The interaction prompt is displayed through TMP_Text and is taken
    /// directly from the current IInteractable target.
    ///
    /// Attach to the player root and assign a child Transform
    /// (InteractCheck) as the detection origin.
    /// </summary>
    public class PlayerInteract : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform interactPoint;
        [SerializeField] private TMP_Text interactPrompt;

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

        /// <summary>
        /// Fired the instant an interaction actually resolves —
        /// either an immediate tap or a hold reaching 100%.
        /// </summary>
        public event Action OnInteracted;

        private void Start()
        {
            Controls.OnInteractPressed += HandleInteractPressed;
            Controls.OnInteractReleased += HandleInteractReleased;

            UpdatePrompt(null);
        }

        private void OnDisable()
        {
            Controls.OnInteractPressed -= HandleInteractPressed;
            Controls.OnInteractReleased -= HandleInteractReleased;

            CancelHold();
            UpdatePrompt(null);
        }

        private void FixedUpdate()
        {
            IInteractable previousTarget = CurrentTarget;

            CurrentTarget = FindClosestInteractable();

            // Update prompt whenever the target changes.
            if (!ReferenceEquals(CurrentTarget, previousTarget))
            {
                UpdatePrompt(CurrentTarget);

                // Target changed while holding.
                // Cancel the old interaction rather than continuing it.
                if (_holdTarget != null)
                    CancelHold();
            }

            if (_holdTarget != null)
                TickHold();
        }

        private void TickHold()
        {
            if (_holdTarget == null)
                return;

            _holdElapsed += Time.fixedDeltaTime;

            float duration = Mathf.Max(_holdTarget.HoldDuration, 0.01f);
            float progress = Mathf.Clamp01(_holdElapsed / duration);

            _holdTarget.OnHoldProgress(progress);

            if (progress >= 1f)
            {
                IHoldInteractable completed = _holdTarget;

                _holdTarget = null;
                _holdElapsed = 0f;

                completed.Interact();

                Audio.PlaySFX2D(
                    PlayerSFX.Category,
                    PlayerSFX.Interact
                );

                OnInteracted?.Invoke();
            }
        }

        private void CancelHold()
        {
            if (_holdTarget == null)
                return;

            _holdTarget.OnHoldCanceled();

            _holdTarget = null;
            _holdElapsed = 0f;
        }

        private void HandleInteractPressed()
        {
            if (CurrentTarget == null)
                return;

            // Prevent starting another hold while one is already active.
            if (_holdTarget != null)
                return;

            if (CurrentTarget is IHoldInteractable holdInteractable)
            {
                _holdTarget = holdInteractable;
                _holdElapsed = 0f;

                // Immediately report 0% progress.
                _holdTarget.OnHoldProgress(0f);
            }
            else
            {
                CurrentTarget.Interact();

                Audio.PlaySFX2D(
                    PlayerSFX.Category,
                    PlayerSFX.Interact
                );

                OnInteracted?.Invoke();
            }
        }

        private void HandleInteractReleased()
        {
            if (_holdTarget != null)
                CancelHold();
        }

        private IInteractable FindClosestInteractable()
        {
            if (interactPoint == null)
                return null;

            int count = Physics2D.OverlapCircleNonAlloc(
                interactPoint.position,
                interactRadius,
                _overlapBuffer,
                interactableLayer
            );

            IInteractable closest = null;
            float closestDistSqr = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _overlapBuffer[i];

                if (collider == null)
                    continue;

                if (!collider.TryGetComponent(
                        out IInteractable interactable))
                    continue;

                float distSqr =
                    ((Vector2)collider.transform.position -
                     (Vector2)interactPoint.position).sqrMagnitude;

                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    closest = interactable;
                }
            }

            return closest;
        }

        private void UpdatePrompt(IInteractable target)
        {
            if (interactPrompt == null)
                return;

            if (target == null)
            {
                interactPrompt.gameObject.SetActive(false);
                return;
            }

            interactPrompt.gameObject.SetActive(true);
            interactPrompt.text = target.Prompt;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo || interactPoint == null)
                return;

            Gizmos.color =
                CurrentTarget != null
                    ? Color.green
                    : Color.yellow;

            Gizmos.DrawWireSphere(
                interactPoint.position,
                interactRadius
            );
        }
    }
}