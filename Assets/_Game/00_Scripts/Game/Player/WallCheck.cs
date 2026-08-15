using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects walls to the player's left and right, and provides a
    /// distance-based cast used by PlayerMovement to snap horizontal
    /// movement exactly onto the wall surface (prevents clipping through).
    ///
    /// Uses BoxCast (not CircleCast) so the check spans the player's full
    /// height in one sweep — a single-point circle check can miss thin
    /// platforms/walls that sit above or below that one sampled height.
    ///
    /// leftPoint/rightPoint should be positioned at FEET height (same as
    /// GroundCheck), not center — the check box is anchored at the feet and
    /// grows upward, so shrinking for a crouch only lowers the top of the
    /// box instead of shrinking symmetrically toward the center (which
    /// would incorrectly lift the bottom of the check off the ground).
    /// </summary>
    public class WallCheck : MonoBehaviour
    {
        [Header("Check Points (positioned at feet height)")]
        [SerializeField] private Transform leftPoint;
        [SerializeField] private Transform rightPoint;

        [Header("Check Settings")]
        [SerializeField] private float standingCheckHeight = 1.8f; // should roughly match standing collider height (with a small inset)
        [SerializeField] private float crouchCheckHeight = 0.9f;   // should roughly match crouch collider height (with a small inset)
        [SerializeField] private float checkThickness = 0.1f;      // depth of the box along the cast direction
        [SerializeField] private float skinWidth = 0.05f;          // short-range "am I touching a wall right now" check
        [SerializeField] private LayerMask wallLayer;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        public bool IsTouchingLeft { get; private set; }
        public bool IsTouchingRight { get; private set; }

        private float _currentCheckHeight;
        private Vector2 BoxSize => new Vector2(checkThickness, _currentCheckHeight);

        private void Awake()
        {
            _currentCheckHeight = standingCheckHeight;
        }

        /// <summary>
        /// Call this when the player toggles between standing and crouch
        /// colliders, so the wall check band shrinks/grows to match — a
        /// standing-height check while crouched would false-positive on
        /// ceilings/overhangs that are only in the way while standing.
        /// </summary>
        public void SetCrouching(bool isCrouching)
        {
            _currentCheckHeight = isCrouching ? crouchCheckHeight : standingCheckHeight;
        }

        /// <summary>
        /// Box center anchored so the BOTTOM edge sits at the point's feet
        /// position, growing upward by _currentCheckHeight.
        /// </summary>
        private Vector2 GetOrigin(Transform point)
        {
            return (Vector2)point.position + Vector2.up * (_currentCheckHeight * 0.5f);
        }

        private void FixedUpdate()
        {
            IsTouchingLeft = Physics2D.BoxCast(GetOrigin(leftPoint), BoxSize, 0f, Vector2.left, skinWidth, wallLayer);
            IsTouchingRight = Physics2D.BoxCast(GetOrigin(rightPoint), BoxSize, 0f, Vector2.right, skinWidth, wallLayer);
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
                hit = Physics2D.BoxCast(GetOrigin(leftPoint), BoxSize, 0f, Vector2.left, distance, wallLayer);
            }
            else if (direction > 0)
            {
                hit = Physics2D.BoxCast(GetOrigin(rightPoint), BoxSize, 0f, Vector2.right, distance, wallLayer);
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
                Gizmos.DrawWireCube(GetOrigin(leftPoint), BoxSize);
            }
            if (rightPoint != null)
            {
                Gizmos.color = IsTouchingRight ? Color.red : Color.green;
                Gizmos.DrawWireCube(GetOrigin(rightPoint), BoxSize);
            }
        }
    }
}