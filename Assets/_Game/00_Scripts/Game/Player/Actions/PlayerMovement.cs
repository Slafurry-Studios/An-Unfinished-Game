using System.Collections.Generic;
using UnityEngine;
using Slafurry.System.InputHub;

namespace Slafurry.Player
{
    /// <summary>
    /// Core movement controller.
    ///
    /// Uses Kinematic Rigidbody2D with custom movement/collision resolution.
    /// External movement from other Rigidbody2D objects is also supported,
    /// allowing the player to ride moving platforms and be pushed by moving
    /// physics objects without switching the player to Dynamic physics.
    ///
    /// External motion is detected via the same raycasts used for ground /
    /// wall snapping (not via collision callbacks). Kinematic-vs-kinematic
    /// collision events are unreliable when the player is resting exactly
    /// against a surface (no real overlap), which caused external motion
    /// (moving platforms) to be missed or flicker on/off.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GroundCheck groundCheck;
        [SerializeField] private HeadCheck headCheck;
        [SerializeField] private WallCheck wallCheck;
        [SerializeField] private PlayerCrouch crouch;

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
        [SerializeField] private float fallGravityMultiplier = 1.4f;

        [Header("Snap Settings")]
        [SerializeField] private float groundSnapMargin = 0.05f;
        [SerializeField] private float wallSnapMargin = 0.02f;

        [Header("External Motion")]
        [Tooltip("How much external Rigidbody2D movement is transferred to the player.")]
        [SerializeField] private float externalMotionMultiplier = 1f;

        [Tooltip("Ignore extremely small Rigidbody movement.")]
        [SerializeField] private float minimumExternalMotion = 0.0001f;

        [Tooltip("Layers containing moving platforms / physics objects.")]
        [SerializeField] private LayerMask externalMotionMask = ~0;

        [Tooltip("How far beyond the resting margin to probe for a touching ground/wall body.")]
        [SerializeField] private float externalDetectionMargin = 0.02f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer playerSprite;

        private Rigidbody2D _rb;
        private Collider2D _collider;

        private Vector2 _velocity;
        private float _moveInput;
        private bool _jumpQueued;

        // Last known position of each external body we're currently in
        // contact with, used to compute per-frame delta movement.
        private readonly Dictionary<Rigidbody2D, Vector2> _previousBodyPositions = new();

        // Scratch set rebuilt every FixedUpdate: bodies detected as touching
        // this frame. Used to prune _previousBodyPositions of stale entries.
        private readonly HashSet<Rigidbody2D> _activeBodiesThisFrame = new();

        private Vector2 _externalMotion;

        public bool IsGrounded { get; private set; }
        public bool IsHeadBlocked { get; private set; }
        public Vector2 Velocity => _velocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;

            // Important for Kinematic <-> Kinematic contacts.
            _rb.useFullKinematicContacts = true;
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

            _activeBodiesThisFrame.Clear();
            _previousBodyPositions.Clear();
        }

        private void HandleMoveChanged(Vector2 input)
        {
            _moveInput = input.x;
        }

        private void HandleJumpPressed()
        {
            _jumpQueued = true;
        }

        private void FixedUpdate()
        {
            // Use last frame's settled state for movement/jump decisions.
            IsGrounded = groundCheck.IsGrounded;
            IsHeadBlocked = headCheck.IsBlocked;

            // Detect movement produced by other Rigidbody2D objects we are
            // currently resting against (ground and/or wall).
            UpdateExternalMotion();

            ApplyHorizontalMovement();
            ApplyGravity();
            HandleJump();
            UpdateFacing();

            MoveAndSnap();

            PruneStaleBodyPositions();
        }

        private void UpdateFacing()
        {
            if (_moveInput > 0.01f)
                playerSprite.flipX = false;
            else if (_moveInput < -0.01f)
                playerSprite.flipX = true;
        }

        private void ApplyHorizontalMovement()
        {
            float speedMultiplier = crouch != null ? crouch.SpeedMultiplier : 1f;
            float targetSpeed = _moveInput * moveSpeed * speedMultiplier;

            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f;

            float rate;

            if (IsGrounded)
                rate = accelerating ? groundAcceleration : groundDeceleration;
            else
                rate = accelerating ? airAcceleration : airDeceleration;

            _velocity.x = Mathf.MoveTowards(
                _velocity.x,
                targetSpeed,
                rate * Time.fixedDeltaTime
            );

            // Do not fight a wall we're already resting against.
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
                float g = _velocity.y < 0f
                    ? gravity * fallGravityMultiplier
                    : gravity;

                _velocity.y -= g * Time.fixedDeltaTime;
                _velocity.y = Mathf.Max(_velocity.y, -maxFallSpeed);
            }

            // Ceiling hit while rising.
            if (IsHeadBlocked && _velocity.y > 0f)
                _velocity.y = 0f;
        }

        private void HandleJump()
        {
            if (!_jumpQueued)
                return;

            _jumpQueued = false;

            if (!IsGrounded)
                return;

            // Stand before jumping.
            if (crouch != null && !crouch.TryStandUp())
                return;

            _velocity.y = jumpForce;
        }

        /// <summary>
        /// Calculates movement coming from other Rigidbody2D objects the
        /// player is currently resting against (ground and/or wall),
        /// detected via the same raycasts used for snapping - not via
        /// collision callbacks, which are unreliable for a kinematic body
        /// resting exactly against a surface with no real overlap.
        ///
        /// Example:
        /// Platform moves +1 X between FixedUpdates.
        /// Player receives +1 X as external movement.
        /// </summary>
        private void UpdateExternalMotion()
        {
            _externalMotion = Vector2.zero;
            _activeBodiesThisFrame.Clear();

            if (IsGrounded)
                AccumulateExternalMotion(GetGroundBody());

            if (wallCheck.IsTouchingRight)
                AccumulateExternalMotion(GetWallBody(1));

            if (wallCheck.IsTouchingLeft)
                AccumulateExternalMotion(GetWallBody(-1));
        }

        private Rigidbody2D GetGroundBody()
        {
            float checkDistance = groundSnapMargin + externalDetectionMargin;

            if (groundCheck.CastGround(checkDistance, out RaycastHit2D hit))
                return hit.rigidbody;

            return null;
        }

        private Rigidbody2D GetWallBody(int direction)
        {
            float checkDistance = wallSnapMargin + externalDetectionMargin;

            if (wallCheck.CastWall(direction, checkDistance, out RaycastHit2D hit))
                return hit.rigidbody;

            return null;
        }

        private void AccumulateExternalMotion(Rigidbody2D body)
        {
            if (body == null)
                return;

            if (body == _rb)
                return;

            if (!IsLayerIncluded(body.gameObject.layer))
                return;

            // Same body detected via both ground and wall checks (e.g. a
            // corner) - don't double count its delta.
            if (!_activeBodiesThisFrame.Add(body))
                return;

            Vector2 currentPosition = body.position;

            if (!_previousBodyPositions.TryGetValue(body, out Vector2 previousPosition))
            {
                // First frame touching this body - no delta yet, just seed it.
                _previousBodyPositions[body] = currentPosition;
                return;
            }

            Vector2 delta = currentPosition - previousPosition;

            if (delta.sqrMagnitude >= minimumExternalMotion * minimumExternalMotion)
            {
                _externalMotion += delta * externalMotionMultiplier;
            }

            _previousBodyPositions[body] = currentPosition;
        }

        private bool IsLayerIncluded(int layer)
        {
            return (externalMotionMask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// Combines movement generated by the player and movement generated
        /// by external Rigidbody2D objects.
        /// </summary>
        private void MoveAndSnap()
        {
            Vector2 playerMove = _velocity * Time.fixedDeltaTime;

            Vector2 totalMove = playerMove + _externalMotion;

            float newX = ResolveHorizontal(totalMove.x);
            float newY = ResolveVertical(totalMove.y);

            _rb.MovePosition(new Vector2(newX, newY));
        }

        private float ResolveHorizontal(float moveX)
        {
            if (Mathf.Approximately(moveX, 0f))
                return _rb.position.x;

            int direction = moveX > 0f ? 1 : -1;

            float checkDistance =
                Mathf.Abs(moveX) + wallSnapMargin;

            if (wallCheck.CastWall(
                    direction,
                    checkDistance,
                    out RaycastHit2D hit))
            {
                // External object is allowed to push us until we hit
                // another solid surface.
                _velocity.x = 0f;

                return _rb.position.x + direction * hit.distance;
            }

            return _rb.position.x + moveX;
        }

        private float ResolveVertical(float moveY)
        {
            if (Mathf.Approximately(moveY, 0f))
                return _rb.position.y;

            // Moving upward.
            if (moveY > 0f)
            {
                // HeadCheck handles ceiling collision.
                return _rb.position.y + moveY;
            }

            float checkDistance =
                Mathf.Abs(moveY) + groundSnapMargin;

            if (groundCheck.CastGround(
                    checkDistance,
                    out RaycastHit2D hit))
            {
                _velocity.y = 0f;

                return _rb.position.y - hit.distance;
            }

            return _rb.position.y + moveY;
        }

        private void PruneStaleBodyPositions()
        {
            List<Rigidbody2D> bodiesToRemove = new();

            foreach (Rigidbody2D body in _previousBodyPositions.Keys)
            {
                if (body == null || !_activeBodiesThisFrame.Contains(body))
                    bodiesToRemove.Add(body);
            }

            foreach (Rigidbody2D body in bodiesToRemove)
            {
                _previousBodyPositions.Remove(body);
            }
        }
    }
}