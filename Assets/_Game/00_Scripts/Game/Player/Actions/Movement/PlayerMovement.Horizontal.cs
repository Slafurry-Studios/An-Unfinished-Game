using UnityEngine;

namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        // =========================================================
        // HORIZONTAL MOVEMENT (relative to gravity, not world X)
        // =========================================================

        private void ApplyHorizontalMovement()
        {
            float input = _moveInput;

            if (input < 0f && !leftMovementEnabled)
                input = 0f;

            if (input > 0f && !rightMovementEnabled)
                input = 0f;

            bool isCrouching = crouch != null && crouch.IsCrouching;

            float crouchMultiplier = crouch != null ? crouch.SpeedMultiplier : 1f;

            float targetSpeed =
                input *
                moveSpeed *
                moveSpeedMultiplier *
                crouchMultiplier;

            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;

            float rate;

            if (IsGrounded)
            {
                rate = accelerating ? groundAcceleration : groundDeceleration;
            }
            else
            {
                rate = accelerating ? airAcceleration : airDeceleration;
            }

            // "Right" is relative to the player's current orientation, which
            // is kept in sync with gravityDirection by ApplyGravityOrientation().
            Vector2 rightAxis = transform.right;

            float alongGravity = Vector2.Dot(_velocity, gravityDirection);
            float alongRight = Vector2.Dot(_velocity, rightAxis);

            alongRight = Mathf.MoveTowards(
                alongRight,
                targetSpeed,
                rate * Time.fixedDeltaTime
            );

            if (alongRight > 0f && wallCheck != null && wallCheck.IsTouchingRight)
            {
                alongRight = 0f;
            }
            else if (alongRight < 0f && wallCheck != null && wallCheck.IsTouchingLeft)
            {
                alongRight = 0f;
            }

            if (alongRight < 0f && !leftMovementEnabled)
                alongRight = 0f;

            if (alongRight > 0f && !rightMovementEnabled)
                alongRight = 0f;

            _velocity = gravityDirection * alongGravity + rightAxis * alongRight;

            HandleMovementLoopSFX(isCrouching);
        }
    }
}