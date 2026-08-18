namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        // =========================================================
        // LEFT MOVEMENT
        // =========================================================

        public void SetLeftMovementEnabled(bool enabled)
        {
            leftMovementEnabled = enabled;

            if (!enabled && UnityEngine.Vector2.Dot(_velocity, transform.right) < 0f)
            {
                ZeroVelocityAlong(transform.right);
            }
        }

        public void EnableLeftMovement() => SetLeftMovementEnabled(true);
        public void DisableLeftMovement() => SetLeftMovementEnabled(false);

        // =========================================================
        // RIGHT MOVEMENT
        // =========================================================

        public void SetRightMovementEnabled(bool enabled)
        {
            rightMovementEnabled = enabled;

            if (!enabled && UnityEngine.Vector2.Dot(_velocity, transform.right) > 0f)
            {
                ZeroVelocityAlong(transform.right);
            }
        }

        public void EnableRightMovement() => SetRightMovementEnabled(true);
        public void DisableRightMovement() => SetRightMovementEnabled(false);

        // =========================================================
        // SPEED
        // =========================================================

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier = UnityEngine.Mathf.Max(0f, multiplier);
        }

        public void ResetMoveSpeedMultiplier()
        {
            moveSpeedMultiplier = 1f;
        }

        // =========================================================
        // JUMP
        // =========================================================

        public void SetJumpEnabled(bool enabled)
        {
            jumpEnabled = enabled;

            if (!enabled)
                _jumpQueued = false;
        }

        public void EnableJump() => SetJumpEnabled(true);
        public void DisableJump() => SetJumpEnabled(false);

        // =========================================================
        // CROUCH
        // =========================================================

        public void SetCrouchEnabled(bool enabled)
        {
            crouchEnabled = enabled;
        }

        public void EnableCrouch() => SetCrouchEnabled(true);
        public void DisableCrouch() => SetCrouchEnabled(false);

        // =========================================================
        // COLLIDER CONTROL
        // =========================================================

        /// <summary>
        /// Enables or disables only the colliders assigned
        /// to Controlled Colliders.
        /// </summary>
        public void SetCollidersEnabled(bool enabled)
        {
            if (controlledColliders == null)
                return;

            foreach (var collider in controlledColliders)
            {
                if (collider != null)
                    collider.enabled = enabled;
            }
        }

        public void EnableColliders() => SetCollidersEnabled(true);
        public void DisableColliders() => SetCollidersEnabled(false);

        // =========================================================
        // ALL CONTROL
        // =========================================================

        public void SetControlEnabled(bool enabled)
        {
            leftMovementEnabled = enabled;
            rightMovementEnabled = enabled;
            gravityEnabled = enabled;
            jumpEnabled = enabled;
            crouchEnabled = enabled;

            if (!enabled)
            {
                _velocity = UnityEngine.Vector2.zero;
                _jumpQueued = false;
                StopMovementLoopSFX();
            }
        }

        public void EnableControl() => SetControlEnabled(true);
        public void DisableControl() => SetControlEnabled(false);
    }
}