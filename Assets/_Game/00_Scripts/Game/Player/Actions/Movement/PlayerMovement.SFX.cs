using UnityEngine;
using Slafurry.System.Audio;

namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        private bool _wasGrounded;
        private bool _wasMovingOnGround;
        private bool _wasCrouchingWhileMoving;

        private void HandleLandingSFX()
        {
            if (IsGrounded && !_wasGrounded)
            {
                Audio.PlaySFX2D(PlayerSFX.Category, PlayerSFX.Land);
            }

            _wasGrounded = IsGrounded;
        }

        private void HandleMovementLoopSFX(bool isCrouching)
        {
            Vector2 rightAxis = transform.right;
            float alongRight = Vector2.Dot(_velocity, rightAxis);

            bool isMovingOnGround = IsGrounded && Mathf.Abs(alongRight) > 0.05f;

            if (isMovingOnGround &&
                (!_wasMovingOnGround || isCrouching != _wasCrouchingWhileMoving))
            {
                StopMovementLoopSFX();

                Audio.PlaySFX2D(
                    PlayerSFX.Category,
                    isCrouching ? PlayerSFX.CrouchWalk : PlayerSFX.Run,
                    true
                );
            }
            else if (!isMovingOnGround && _wasMovingOnGround)
            {
                StopMovementLoopSFX();
            }

            _wasMovingOnGround = isMovingOnGround;
            _wasCrouchingWhileMoving = isCrouching;
        }

        private void StopMovementLoopSFX()
        {
            Audio.StopSFX(PlayerSFX.Category, PlayerSFX.Run);
            Audio.StopSFX(PlayerSFX.Category, PlayerSFX.CrouchWalk);
        }
    }
}