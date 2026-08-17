using System.Collections.Generic;
using UnityEngine;
using Slafurry.System.InputHub;

namespace Slafurry.Player
{
    /// <summary>
    /// Core movement controller.
    ///
    /// Supports:
    /// - Independent left/right movement control
    /// - Movement speed multiplier
    /// - Gravity enable/disable
    /// - Four-direction gravity
    /// - Jump opposite to gravity direction
    /// - Jump enable/disable
    /// - Crouch enable/disable
    /// - Full control enable/disable
    /// - Selective collider enable/disable
    ///
    /// Gravity directions:
    /// Down  = (0, -1)
    /// Up    = (0,  1)
    /// Left  = (-1, 0)
    /// Right = (1,  0)
    ///
    /// Player rotates to match gravity direction.
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

        [Header("Visual")]
        [SerializeField] private SpriteRenderer playerSprite;

        [Header("Gravity")]
        [SerializeField] private Vector2 gravityDirection = Vector2.down;
        [SerializeField] private bool gravityEnabled = true;

        [Header("Control")]
        [SerializeField] private bool leftMovementEnabled = true;
        [SerializeField] private bool rightMovementEnabled = true;
        [SerializeField] private bool jumpEnabled = true;
        [SerializeField] private bool crouchEnabled = true;
        [SerializeField] private float moveSpeedMultiplier = 1f;

        [Header("Collider Control")]
        [Tooltip("Only these colliders will be affected by EnableCollider/DisableCollider.")]
        [SerializeField] private Collider2D[] controlledColliders;

        private Rigidbody2D _rb;

        private Vector2 _velocity;
        private float _moveInput;
        private bool _jumpQueued;

        private Quaternion _originalRotation;

        public bool IsGrounded { get; private set; }
        public bool IsHeadBlocked { get; private set; }

        public Vector2 Velocity => _velocity;

        public bool GravityEnabled => gravityEnabled;
        public Vector2 GravityDirection => gravityDirection;

        public bool LeftMovementEnabled => leftMovementEnabled;
        public bool RightMovementEnabled => rightMovementEnabled;

        public bool JumpEnabled => jumpEnabled;
        public bool CrouchEnabled => crouchEnabled;

        public float MoveSpeedMultiplier => moveSpeedMultiplier;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;

            _originalRotation = transform.localRotation;

            gravityDirection =
                NormalizeGravityDirection(gravityDirection);
        }

        private void Start()
        {
            Controls.OnMoveChanged += HandleMoveChanged;
            Controls.OnJumpPressed += HandleJumpPressed;

            ApplyGravityOrientation();
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

        // =========================================================
        // HORIZONTAL MOVEMENT
        // =========================================================

        private void ApplyHorizontalMovement()
        {
            float input = _moveInput;

            if (input < 0f && !leftMovementEnabled)
                input = 0f;

            if (input > 0f && !rightMovementEnabled)
                input = 0f;

            float crouchMultiplier =
                crouch != null
                    ? crouch.SpeedMultiplier
                    : 1f;

            float targetSpeed =
                input *
                moveSpeed *
                moveSpeedMultiplier *
                crouchMultiplier;

            bool accelerating =
                Mathf.Abs(targetSpeed) > 0.01f;

            float rate;

            if (IsGrounded)
            {
                rate = accelerating
                    ? groundAcceleration
                    : groundDeceleration;
            }
            else
            {
                rate = accelerating
                    ? airAcceleration
                    : airDeceleration;
            }

            _velocity.x = Mathf.MoveTowards(
                _velocity.x,
                targetSpeed,
                rate * Time.fixedDeltaTime
            );

            if (_velocity.x > 0f &&
                wallCheck != null &&
                wallCheck.IsTouchingRight)
            {
                _velocity.x = 0f;
            }
            else if (_velocity.x < 0f &&
                     wallCheck != null &&
                     wallCheck.IsTouchingLeft)
            {
                _velocity.x = 0f;
            }

            if (_velocity.x < 0f &&
                !leftMovementEnabled)
            {
                _velocity.x = 0f;
            }

            if (_velocity.x > 0f &&
                !rightMovementEnabled)
            {
                _velocity.x = 0f;
            }
        }

        // =========================================================
        // GRAVITY
        // =========================================================

        private void ApplyGravity()
        {
            if (!gravityEnabled)
            {
                _velocity = Vector2.zero;
                return;
            }

            Vector2 direction =
                gravityDirection.normalized;

            float velocityAlongGravity =
                Vector2.Dot(
                    _velocity,
                    direction
                );

            bool movingWithGravity =
                velocityAlongGravity > 0f;

            float currentGravity =
                movingWithGravity
                    ? gravity * fallGravityMultiplier
                    : gravity;

            _velocity +=
                direction *
                currentGravity *
                Time.fixedDeltaTime;

            velocityAlongGravity =
                Vector2.Dot(
                    _velocity,
                    direction
                );

            if (velocityAlongGravity > maxFallSpeed)
            {
                Vector2 perpendicularVelocity =
                    _velocity -
                    direction * velocityAlongGravity;

                _velocity =
                    perpendicularVelocity +
                    direction * maxFallSpeed;
            }
        }

        // =========================================================
        // JUMP
        // =========================================================

        private void HandleJump()
        {
            if (!_jumpQueued)
                return;

            _jumpQueued = false;

            if (!jumpEnabled)
                return;

            if (!IsGrounded)
                return;

            if (crouch != null && crouchEnabled)
            {
                if (!crouch.TryStandUp())
                    return;
            }

            float velocityAlongGravity =
                Vector2.Dot(
                    _velocity,
                    gravityDirection
                );

            Vector2 gravityVelocity =
                gravityDirection *
                velocityAlongGravity;

            Vector2 sidewaysVelocity =
                _velocity -
                gravityVelocity;

            Vector2 jumpVelocity =
                -gravityDirection *
                jumpForce;

            _velocity =
                sidewaysVelocity +
                jumpVelocity;
        }

        // =========================================================
        // FACING
        // =========================================================

        private void UpdateFacing()
        {
            if (playerSprite == null)
                return;

            if (_moveInput > 0.01f &&
                rightMovementEnabled)
            {
                playerSprite.flipX = false;
            }
            else if (_moveInput < -0.01f &&
                     leftMovementEnabled)
            {
                playerSprite.flipX = true;
            }
        }

        // =========================================================
        // MOVEMENT
        // =========================================================

        private void MoveAndSnap()
        {
            Vector2 move =
                _velocity *
                Time.fixedDeltaTime;

            float newX =
                ResolveHorizontal(move.x);

            float newY =
                ResolveVertical(move.y);

            _rb.MovePosition(
                new Vector2(
                    newX,
                    newY
                )
            );
        }

        private float ResolveHorizontal(float moveX)
        {
            if (Mathf.Approximately(moveX, 0f))
                return _rb.position.x;

            int direction =
                moveX > 0f ? 1 : -1;

            float checkDistance =
                Mathf.Abs(moveX) +
                wallSnapMargin;

            if (wallCheck != null &&
                wallCheck.CastWall(
                    direction,
                    checkDistance,
                    out RaycastHit2D hit))
            {
                // External object is allowed to push us until we hit
                // another solid surface.
                _velocity.x = 0f;

                return _rb.position.x +
                       direction *
                       hit.distance;
            }

            return _rb.position.x + moveX;
        }

        private float ResolveVertical(float moveY)
        {
            if (Mathf.Approximately(moveY, 0f))
                return _rb.position.y;

            /*
             * GroundCheck tetap sederhana dan melakukan
             * pengecekan ke bawah.
             *
             * Jadi snap hanya berlaku untuk gravity ke bawah.
             */

            if (gravityDirection == Vector2.down &&
                moveY < 0f)
            {
                float checkDistance =
                    Mathf.Abs(moveY) +
                    groundSnapMargin;

                if (groundCheck != null &&
                    groundCheck.CastGround(
                        checkDistance,
                        out RaycastHit2D hit))
                {
                    _velocity.y = 0f;

                    return _rb.position.y -
                           hit.distance;
                }
            }

            return _rb.position.y + moveY;
        }

        // =========================================================
        // GRAVITY DIRECTION
        // =========================================================

        private Vector2 NormalizeGravityDirection(
            Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return Vector2.down;

            return direction.normalized;
        }

        private void ApplyGravityOrientation()
        {
            Vector2 direction =
                gravityDirection.normalized;

            /*
             * Gravity:
             *
             * Down  ->   0°
             * Right -> -90°
             * Up    -> 180°
             * Left  ->  90°
             */

            float angle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) *
                Mathf.Rad2Deg +
                90f;

            transform.localRotation =
                _originalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }

        public void SetGravityDirection(
            Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            gravityDirection =
                NormalizeGravityDirection(direction);

            _velocity = Vector2.zero;

            ApplyGravityOrientation();
        }

        // =========================================================
        // GRAVITY PRESETS
        // =========================================================

        public void SetGravityDown()
        {
            SetGravityDirection(Vector2.down);
        }

        public void SetGravityUp()
        {
            SetGravityDirection(Vector2.up);
        }

        public void SetGravityLeft()
        {
            SetGravityDirection(Vector2.left);
        }

        public void SetGravityRight()
        {
            SetGravityDirection(Vector2.right);
        }

        public void RotateGravityClockwise()
        {
            Vector2 direction =
                new Vector2(
                    gravityDirection.y,
                    -gravityDirection.x
                );

            SetGravityDirection(direction);
        }

        public void RotateGravityCounterClockwise()
        {
            Vector2 direction =
                new Vector2(
                    -gravityDirection.y,
                    gravityDirection.x
                );

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

        public void EnableGravity()
        {
            SetGravityEnabled(true);
        }

        public void DisableGravity()
        {
            SetGravityEnabled(false);
        }

        // =========================================================
        // LEFT MOVEMENT
        // =========================================================

        public void SetLeftMovementEnabled(bool enabled)
        {
            leftMovementEnabled = enabled;

            if (!enabled &&
                _velocity.x < 0f)
            {
                _velocity.x = 0f;
            }
        }

        public void EnableLeftMovement()
        {
            SetLeftMovementEnabled(true);
        }

        public void DisableLeftMovement()
        {
            SetLeftMovementEnabled(false);
        }

        // =========================================================
        // RIGHT MOVEMENT
        // =========================================================

        public void SetRightMovementEnabled(bool enabled)
        {
            rightMovementEnabled = enabled;

            if (!enabled &&
                _velocity.x > 0f)
            {
                _velocity.x = 0f;
            }
        }

        public void EnableRightMovement()
        {
            SetRightMovementEnabled(true);
        }

        public void DisableRightMovement()
        {
            SetRightMovementEnabled(false);
        }

        // =========================================================
        // SPEED
        // =========================================================

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            moveSpeedMultiplier =
                Mathf.Max(0f, multiplier);
        }

        public void ResetMoveSpeedMultiplier()
        {
            moveSpeedMultiplier = 1f;
        }

        // =========================================================
        // JUMP
        // =========================================================

        public void SetJumpEnabled(bool enabled)
        {
            jumpEnabled = enabled;

            if (!enabled)
                _jumpQueued = false;
        }

        public void EnableJump()
        {
            SetJumpEnabled(true);
        }

        public void DisableJump()
        {
            SetJumpEnabled(false);
        }

        // =========================================================
        // CROUCH
        // =========================================================

        public void SetCrouchEnabled(bool enabled)
        {
            crouchEnabled = enabled;
        }

        public void EnableCrouch()
        {
            SetCrouchEnabled(true);
        }

        public void DisableCrouch()
        {
            SetCrouchEnabled(false);
        }

        // =========================================================
        // COLLIDER CONTROL
        // =========================================================

        /// <summary>
        /// Enables or disables only the colliders assigned
        /// to Controlled Colliders.
        /// </summary>
        public void SetCollidersEnabled(bool enabled)
        {
            if (controlledColliders == null)
                return;

            foreach (Collider2D collider in controlledColliders)
            {
                if (collider != null)
                    collider.enabled = enabled;
            }
        }

        public void EnableColliders()
        {
            SetCollidersEnabled(true);
        }

        public void DisableColliders()
        {
            SetCollidersEnabled(false);
        }

        // =========================================================
        // ALL CONTROL
        // =========================================================

        public void SetControlEnabled(bool enabled)
        {
            leftMovementEnabled = enabled;
            rightMovementEnabled = enabled;
            gravityEnabled = enabled;
            jumpEnabled = enabled;
            crouchEnabled = enabled;

            if (!enabled)
            {
                _velocity = Vector2.zero;
                _jumpQueued = false;
            }
        }

        public void EnableControl()
        {
            SetControlEnabled(true);
        }

        public void DisableControl()
        {
            SetControlEnabled(false);
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