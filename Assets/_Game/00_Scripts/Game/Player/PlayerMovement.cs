using UnityEngine;
using Slafurry.System.InputHub;

namespace Slafurry.Player
{
    /// <summary>
    /// Core movement controller. Kinematic (manual) physics — Rigidbody2D is
    /// only used in Kinematic mode so trigger/collision events keep working
    /// (enemy detection, pickups, etc.), but all motion is computed here and
    /// applied via Rigidbody2D.MovePosition, with the player snapped exactly
    /// onto ground/wall surfaces each frame instead of overshooting into them.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GroundCheck groundCheck;
        [SerializeField] private HeadCheck headCheck;
        [SerializeField] private WallCheck wallCheck;

        [Header("Move Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float groundAcceleration = 60f;
        [SerializeField] private float groundDeceleration = 70f;
        [SerializeField] private float airAcceleration = 40f;
        [SerializeField] private float airDeceleration = 30f;

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float gravity = 35f;
        [SerializeField] private float maxFallSpeed = 25f;
        [SerializeField] private float fallGravityMultiplier = 1.4f; // snappier descent

        [Header("Snap Settings")]
        [SerializeField] private float groundSnapMargin = 0.05f; // extra cast distance beyond intended vertical movement
        [SerializeField] private float wallSnapMargin = 0.02f;   // extra cast distance beyond intended horizontal movement

        private Rigidbody2D _rb;
        private Vector2 _velocity;
        private float _moveInput;
        private bool _jumpQueued;

        public bool IsGrounded { get; private set; }
        public bool IsHeadBlocked { get; private set; }
        public Vector2 Velocity => _velocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Start()
        {
            Controls.OnMoveChanged += HandleMoveChanged;
            Controls.OnJumpPressed += HandleJumpPressed;
        }

        private void OnDisable()
        {
            Controls.OnMoveChanged -= HandleMoveChanged;
            Controls.OnJumpPressed -= HandleJumpPressed;
        }

        private void HandleMoveChanged(Vector2 input) => _moveInput = input.x;
        private void HandleJumpPressed() => _jumpQueued = true;

        private void FixedUpdate()
        {
            // Use last frame's settled ground state for movement/jump decisions.
            IsGrounded = groundCheck.IsGrounded;
            IsHeadBlocked = headCheck.IsBlocked;

            ApplyHorizontalMovement();
            ApplyGravity();
            HandleJump();

            MoveAndSnap();
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = _moveInput * moveSpeed;
            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;

            float rate;
            if (IsGrounded)
                rate = accelerating ? groundAcceleration : groundDeceleration;
            else
                rate = accelerating ? airAcceleration : airDeceleration;

            _velocity.x = Mathf.MoveTowards(_velocity.x, targetSpeed, rate * Time.fixedDeltaTime);

            // Stop pushing into a wall we're already resting against —
            // prevents wasted acceleration buildup that would otherwise
            // "let go" instantly once the wall is no longer there.
            if (_velocity.x > 0f && wallCheck.IsTouchingRight)
                _velocity.x = 0f;
            else if (_velocity.x < 0f && wallCheck.IsTouchingLeft)
                _velocity.x = 0f;
        }

        private void ApplyGravity()
        {
            if (IsGrounded && _velocity.y <= 0f)
            {
                _velocity.y = 0f;
            }
            else
            {
                float g = _velocity.y < 0f ? gravity * fallGravityMultiplier : gravity;
                _velocity.y -= g * Time.fixedDeltaTime;
                _velocity.y = Mathf.Max(_velocity.y, -maxFallSpeed);
            }

            // Ceiling hit while rising: stop upward motion instead of clipping through
            if (IsHeadBlocked && _velocity.y > 0f)
                _velocity.y = 0f;
        }

        private void HandleJump()
        {
            if (!_jumpQueued) return;
            _jumpQueued = false;

            if (!IsGrounded) return; // no double jump for now

            _velocity.y = jumpForce;
        }

        /// <summary>
        /// Moves the player and, if approaching a wall or the ground, snaps
        /// the position exactly onto the surface instead of moving the full
        /// velocity-based distance and potentially clipping through it.
        /// </summary>
        private void MoveAndSnap()
        {
            Vector2 move = _velocity * Time.fixedDeltaTime;
            float newX = ResolveHorizontal(move.x);
            float newY = ResolveVertical(move.y);

            _rb.MovePosition(new Vector2(newX, newY));
        }

        private float ResolveHorizontal(float moveX)
        {
            if (Mathf.Approximately(moveX, 0f))
                return _rb.position.x;

            int direction = moveX > 0f ? 1 : -1;
            float checkDistance = Mathf.Abs(moveX) + wallSnapMargin;

            if (wallCheck.CastWall(direction, checkDistance, out RaycastHit2D hit))
            {
                _velocity.x = 0f;
                return _rb.position.x + direction * hit.distance;
            }

            return _rb.position.x + moveX;
        }

        private float ResolveVertical(float moveY)
        {
            if (moveY > 0f)
                return _rb.position.y + moveY; // rising, no ground snap needed (head check handles ceiling)

            float checkDistance = Mathf.Abs(moveY) + groundSnapMargin;

            if (groundCheck.CastGround(checkDistance, out RaycastHit2D hit))
            {
                _velocity.y = 0f;
                return _rb.position.y - hit.distance;
            }

            return _rb.position.y + moveY;
        }
    }
}