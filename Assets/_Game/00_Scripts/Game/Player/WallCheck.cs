using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects walls to the player's left and right, and provides a
    /// distance-based cast used by PlayerMovement to snap horizontal
    /// movement exactly onto the wall surface (prevents clipping through).
    /// Attach this to the player root (or a dedicated child), and assign
    /// two child Transforms positioned at the left and right edges of the
    /// standing collider, roughly at center height.
    /// </summary>
    public class WallCheck : MonoBehaviour
    {
        [Header("Check Points")]
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;

        [Header("Check Settings")]
        [SerializeField] private float checkRadius = 0.1f;
        [SerializeField] private float skinWidth = 0.05f; // short-range "am I touching a wall right now" check
        [SerializeField] private LayerMask wallLayer;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        public bool IsTouchingLeft { get; private set; }
        public bool IsTouchingRight { get; private set; }

        private void FixedUpdate()
        {
            IsTouchingLeft = Physics2D.CircleCast(leftPoint.position, checkRadius, Vector2.left, skinWidth, wallLayer);
            IsTouchingRight = Physics2D.CircleCast(rightPoint.position, checkRadius, Vector2.right, skinWidth, wallLayer);
        }

        /// <summary>
        /// Casts sideways by an arbitrary distance (typically this frame's
        /// intended horizontal movement + a small margin). Used by
        /// PlayerMovement to find exactly how far the player can move
        /// before touching a wall, so position can be snapped instead of
        /// overshooting into it.
        /// </summary>
        /// <param name="direction">-1 for left, +1 for right.</param>
        public bool CastWall(int direction, float distance, out RaycastHit2D hit)
        {
            if (direction < 0)
            {
                hit = Physics2D.CircleCast(leftPoint.position, checkRadius, Vector2.left, distance, wallLayer);
            }
            else if (direction > 0)
            {
                hit = Physics2D.CircleCast(rightPoint.position, checkRadius, Vector2.right, distance, wallLayer);
            }
            else
            {
                hit = default;
            }

            return hit.collider != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            if (leftPoint != null)
            {
                Gizmos.color = IsTouchingLeft ? Color.red : Color.green;
                Gizmos.DrawWireSphere(leftPoint.position, checkRadius);
            }
            if (rightPoint != null)
            {
                Gizmos.color = IsTouchingRight ? Color.red : Color.green;
                Gizmos.DrawWireSphere(rightPoint.position, checkRadius);
            }
        }
    }
}