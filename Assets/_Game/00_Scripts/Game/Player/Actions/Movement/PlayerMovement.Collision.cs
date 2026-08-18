using UnityEngine;

namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        // =========================================================
        // MOVEMENT (resolved along gravity-relative axes)
        // =========================================================

        private void MoveAndSnap()
        {
            Vector2 move = _velocity * Time.fixedDeltaTime;

            Vector2 rightAxis = transform.right;
            Vector2 groundAxis = gravityDirection; // "downward" direction

            float alongRight = Vector2.Dot(move, rightAxis);
            float alongGround = Vector2.Dot(move, groundAxis);

            float resolvedRight = ResolveHorizontal(alongRight, rightAxis);
            float resolvedGround = ResolveVertical(alongGround, groundAxis);

            Vector2 worldDelta =
                rightAxis * resolvedRight +
                groundAxis * resolvedGround;

            _rb.MovePosition(_rb.position + worldDelta);
        }

        private float ResolveHorizontal(float alongRight, Vector2 rightAxis)
        {
            if (Mathf.Approximately(alongRight, 0f))
                return 0f;

            int direction = alongRight > 0f ? 1 : -1;

            float checkDistance = Mathf.Abs(alongRight) + wallSnapMargin;

            if (wallCheck != null &&
                wallCheck.CastWall(direction, checkDistance, out RaycastHit2D hit))
            {
                ZeroVelocityAlong(rightAxis);
                return direction * hit.distance;
            }

            return alongRight;
        }

        private float ResolveVertical(float alongGround, Vector2 groundAxis)
        {
            if (Mathf.Approximately(alongGround, 0f))
                return 0f;

            // alongGround > 0 means moving in the direction of gravity (falling)
            if (alongGround > 0f)
            {
                float checkDistance = Mathf.Abs(alongGround) + groundSnapMargin;

                if (groundCheck != null &&
                    groundCheck.CastGround(checkDistance, out RaycastHit2D hit))
                {
                    ZeroVelocityAlong(groundAxis);
                    return hit.distance;
                }
            }

            return alongGround;
        }
    }
}