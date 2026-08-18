using Slafurry.System.InputHub;
using UnityEngine;

namespace Slafurry.Player
{
    /// <summary>
    /// Core movement controller. Split across partial-class files:
    /// see PlayerMovement.Horizontal.cs, .Gravity.cs, .Jump.cs,
    /// .Collision.cs, .SFX.cs, .Control.cs for the rest.
    ///
    /// Gravity directions:
    /// Down  = (0, -1)
    /// Up    = (0,  1)
    /// Left  = (-1, 0)
    /// Right = (1,  0)
    ///
    /// Player rotates to match gravity direction. All movement, wall checks
    /// and ground checks are resolved relative to that rotation (transform.right
    /// = "sideways" axis, gravityDirection = "downward" axis) instead of raw
    /// world X/Y, so left/right input and snapping stay correct no matter
    /// which way gravity points.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public partial class PlayerMovement : MonoBehaviour
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

        private MovingPlatform _currentPlatform;

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
            _rb.bodyType = RigidbodyType2D.Kinematic;

            _originalRotation = transform.localRotation;

            gravityDirection = NormalizeGravityDirection(gravityDirection);
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

            StopMovementLoopSFX();
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

            HandleLandingSFX();

            HandlePlatformMovement();

            ApplyHorizontalMovement();
            ApplyGravity();
            HandleJump();
            UpdateFacing();
            MoveAndSnap();
        }

        private void HandlePlatformMovement()
        {
            MovingPlatform platform = null;

            if (IsGrounded && groundCheck.GroundCollider != null)
            {
                platform = groundCheck.GroundCollider
                    .GetComponentInParent<MovingPlatform>();
            }

            _currentPlatform = platform;

            if (_currentPlatform != null)
            {
                _rb.position += _currentPlatform.DeltaMovement;
            }
        }

        private void UpdateFacing()
        {
            if (playerSprite == null)
                return;

            if (_moveInput > 0.01f && rightMovementEnabled)
            {
                playerSprite.flipX = false;
            }
            else if (_moveInput < -0.01f && leftMovementEnabled)
            {
                playerSprite.flipX = true;
            }
        }

        private void ZeroVelocityAlong(Vector2 axis)
        {
            axis = axis.normalized;
            float component = Vector2.Dot(_velocity, axis);
            _velocity -= axis * component;
        }
    }
}