using UnityEngine;
using Slafurry.System.Audio;

namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        // =========================================================
        // JUMP
        // =========================================================

        private void HandleJump()
        {
            if (!_jumpQueued)
                return;

            _jumpQueued = false;

            if (!jumpEnabled)
                return;

            if (!IsGrounded)
                return;

            if (crouch != null && crouchEnabled)
            {
                if (!crouch.TryStandUp())
                    return;
            }

            float velocityAlongGravity = Vector2.Dot(_velocity, gravityDirection);

            Vector2 gravityVelocity = gravityDirection * velocityAlongGravity;

            Vector2 sidewaysVelocity = _velocity - gravityVelocity;

            Vector2 jumpVelocity = -gravityDirection * jumpForce;

            _velocity = sidewaysVelocity + jumpVelocity;

            StopMovementLoopSFX();
            Audio.PlaySFX2D(PlayerSFX.Category, PlayerSFX.Jump);
        }
    }
}