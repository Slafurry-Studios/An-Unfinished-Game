using UnityEngine;

namespace Slafurry.Player
{
    public partial class PlayerMovement
    {
        // =========================================================
        // GRAVITY
        // =========================================================

        private void ApplyGravity()
        {
            // Gravity disabled -> do nothing. Velocity is zeroed once by
            // SetGravityEnabled(false), not every frame here, so horizontal
            // input still works while gravity is off.
            if (!gravityEnabled)
                return;

            Vector2 direction = gravityDirection.normalized;

            float velocityAlongGravity = Vector2.Dot(_velocity, direction);

            bool movingWithGravity = velocityAlongGravity > 0f;

            float currentGravity =
                movingWithGravity
                    ? gravity * fallGravityMultiplier
                    : gravity;

            _velocity += direction * currentGravity * Time.fixedDeltaTime;

            velocityAlongGravity = Vector2.Dot(_velocity, direction);

            if (velocityAlongGravity > maxFallSpeed)
            {
                Vector2 perpendicularVelocity =
                    _velocity - direction * velocityAlongGravity;

                _velocity = perpendicularVelocity + direction * maxFallSpeed;
            }
        }

        // =========================================================
        // GRAVITY DIRECTION
        // =========================================================

        private Vector2 NormalizeGravityDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return Vector2.down;

            return direction.normalized;
        }

        private void ApplyGravityOrientation()
        {
            Vector2 direction = gravityDirection.normalized;

            /*
             * Gravity:
             *
             * Down  ->   0°
             * Right -> -90°
             * Up    -> 180°
             * Left  ->  90°
             */

            float angle =
                Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg +
                90f;

            transform.localRotation =
                _originalRotation * Quaternion.Euler(0f, 0f, angle);
        }

        public void SetGravityDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            gravityDirection = NormalizeGravityDirection(direction);

            _velocity = Vector2.zero;

            ApplyGravityOrientation();
        }

        // =========================================================
        // GRAVITY PRESETS
        // =========================================================

        public void SetGravityDown() => SetGravityDirection(Vector2.down);
        public void SetGravityUp() => SetGravityDirection(Vector2.up);
        public void SetGravityLeft() => SetGravityDirection(Vector2.left);
        public void SetGravityRight() => SetGravityDirection(Vector2.right);

        public void RotateGravityClockwise()
        {
            Vector2 direction = new Vector2(gravityDirection.y, -gravityDirection.x);
            SetGravityDirection(direction);
        }

        public void RotateGravityCounterClockwise()
        {
            Vector2 direction = new Vector2(-gravityDirection.y, gravityDirection.x);
            SetGravityDirection(direction);
        }

        // =========================================================
        // GRAVITY ENABLE
        // =========================================================

        public void SetGravityEnabled(bool enabled)
        {
            gravityEnabled = enabled;

            if (!enabled)
                _velocity = Vector2.zero;
        }

        public void EnableGravity() => SetGravityEnabled(true);
        public void DisableGravity() => SetGravityEnabled(false);
    }
}