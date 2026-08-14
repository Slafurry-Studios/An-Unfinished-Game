using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects whether the player is currently touching the ground, and
    /// provides a distance-based cast used by PlayerMovement to snap the
    /// player exactly onto the ground surface (prevents sinking/clipping).
    /// Attach this to a child GameObject positioned at the player's feet.
    /// </summary>
    public class GroundCheck : MonoBehaviour
    {
        [Header("Check Settings")]
        [SerializeField] private float checkRadius = 0.1f;
        [SerializeField] private float skinWidth = 0.05f; // short-range "am I grounded right now" check
        [SerializeField] private LayerMask groundLayer;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        public bool IsGrounded { get; private set; }

        private void FixedUpdate()
        {
            IsGrounded = Physics2D.CircleCast(transform.position, checkRadius, Vector2.down, skinWidth, groundLayer);
        }

        /// <summary>
        /// Casts downward by an arbitrary distance (typically this frame's
        /// intended movement + a small margin). Used by PlayerMovement to
        /// find exactly how far the player can fall before touching ground,
        /// so position can be snapped instead of overshooting into it.
        /// </summary>
        public bool CastGround(float distance, out RaycastHit2D hit)
        {
            hit = Physics2D.CircleCast(transform.position, checkRadius, Vector2.down, distance, groundLayer);
            return hit.collider != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, checkRadius);
        }
    }
}