using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Detects whether there's a ceiling above the player that would block
    /// them from standing up out of a crouch.
    /// Attach this to a child GameObject positioned at the player's head
    /// (should be repositioned when the player crouches/stands, since the
    /// collider height changes).
    /// </summary>
    public class HeadCheck : MonoBehaviour
    {
        [Header("Check Settings")]
        [SerializeField] private float checkRadius = 0.1f;
        [SerializeField] private LayerMask ceilingLayer;

        [Header("Debug")]
        [SerializeField] private bool drawGizmo = true;

        public bool IsBlocked { get; private set; }

        private void FixedUpdate()
        {
            IsBlocked = Physics2D.OverlapCircle(transform.position, checkRadius, ceilingLayer);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;

            Gizmos.color = IsBlocked ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, checkRadius);
        }
    }
}