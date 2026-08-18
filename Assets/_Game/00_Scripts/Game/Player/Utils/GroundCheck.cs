using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects whether the player is currently touching the ground, and
    /// provides a distance-based cast used by PlayerMovement to snap the
    /// player exactly onto the ground surface (prevents sinking/clipping).
    /// Attach this to a child GameObject positioned at the player's feet.
    ///
    /// Cast direction follows the player's local "down" (-transform.up),
    /// so it stays correct even when the player rotates to match a
    /// non-default gravity direction (left/right/up).
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

        /// <summary>
        /// The collider currently detected as ground (null if not grounded).
        /// Used by PlayerMovement to detect moving platforms, one-way
        /// platforms, surface material, etc.
        /// </summary>
        public Collider2D GroundCollider { get; private set; }

        /// <summary>
        /// Local "down" direction, following the transform's rotation.
        /// Equal to Vector2.down when gravity is Down and the player
        /// hasn't been rotated.
        /// </summary>
        private Vector2 CheckDirection => -transform.up;

        private void FixedUpdate()
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position,
                checkRadius,
                CheckDirection,
                skinWidth,
                groundLayer
            );

            IsGrounded = hit.collider != null;
            GroundCollider = hit.collider;
        }

        /// <summary>
        /// Casts along the player's local-down direction by an arbitrary
        /// distance (typically this frame's intended movement + a small
        /// margin). Used by PlayerMovement to find exactly how far the
        /// player can fall before touching ground, so position can be
        /// snapped instead of overshooting into it.
        /// </summary>
        public bool CastGround(float distance, out RaycastHit2D hit)
        {
            hit = Physics2D.CircleCast(
                transform.position,
                checkRadius,
                CheckDirection,
                distance,
                groundLayer
            );

            return hit.collider != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, checkRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                transform.position,
                transform.position + (Vector3)(CheckDirection * skinWidth)
            );
        }
    }
}