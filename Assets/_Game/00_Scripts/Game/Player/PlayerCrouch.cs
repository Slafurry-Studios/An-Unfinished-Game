using UnityEngine;
using Slafurry.System.InputHub;

namespace Slafurry.Player
{
    /// <summary>
    /// Handles crouch state: toggling between standing/crouch colliders,
    /// refusing to stand up if HeadCheck reports a ceiling above, and
    /// keeping WallCheck's detection band in sync with the active collider.
    /// Crouch state can only change while grounded — this avoids the
    /// collider swapping mid-air, which Ground/WallCheck aren't designed
    /// to handle gracefully.
    /// </summary>
    public class PlayerCrouch : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GroundCheck groundCheck;
        [SerializeField] private HeadCheck headCheck;
        [SerializeField] private WallCheck wallCheck;
        [SerializeField] private Collider2D standingCollider;
        [SerializeField] private Collider2D crouchCollider;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = 0.5f;

        private bool _crouchHeld;

        public bool IsCrouching { get; private set; }
        public float SpeedMultiplier => IsCrouching ? crouchSpeedMultiplier : 1f;

        private void Start()
        {
            Controls.OnCrouchStarted += HandleCrouchStarted;
            Controls.OnCrouchCanceled += HandleCrouchCanceled;
        }

        private void OnDisable()
        {
            Controls.OnCrouchStarted -= HandleCrouchStarted;
            Controls.OnCrouchCanceled -= HandleCrouchCanceled;
        }

        private void HandleCrouchStarted() => _crouchHeld = true;
        private void HandleCrouchCanceled() => _crouchHeld = false;

        private void FixedUpdate()
        {
            if (!groundCheck.IsGrounded)
                return; // freeze current crouch state while airborne

            bool wantsToStand = !_crouchHeld;
            bool blockedFromStanding = wantsToStand && headCheck.IsBlocked;

            bool shouldCrouch = _crouchHeld || blockedFromStanding;

            if (shouldCrouch != IsCrouching)
                ApplyCrouchState(shouldCrouch);
        }

        private void ApplyCrouchState(bool crouching)
        {
            IsCrouching = crouching;

            standingCollider.enabled = !crouching;
            crouchCollider.enabled = crouching;

            wallCheck.SetCrouching(crouching);
        }

        /// <summary>
        /// Attempts to immediately force the player out of a crouch (e.g.
        /// right before jumping, so the player never becomes airborne while
        /// still in the crouch collider). Fails if HeadCheck reports a
        /// ceiling blocking the stand-up.
        /// </summary>
        /// <returns>True if now standing (or was already standing).</returns>
        public bool TryStandUp()
        {
            if (!IsCrouching)
                return true;

            if (headCheck.IsBlocked)
                return false;

            ApplyCrouchState(false);
            return true;
        }
    }
}