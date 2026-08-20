using System;
using UnityEngine;

namespace Slafurry.Player.Animation
{
    /// <summary>
    /// Code-driven replacement for a Mecanim Animator Controller. Attach this
    /// alongside PlayerMovement / PlayerCrouch / PlayerInteract / PlayerHealth.
    /// The Animator component is still required (Playables needs it as an
    /// output target) but it should have NO Controller asset assigned —
    /// every state and every transition rule lives in this file and the
    /// state classes under States/.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationStateMachine : MonoBehaviour
    {
        [Serializable]
        public struct ClipConfig
        {
            public AnimationClip clip;
            [Tooltip("0.08–0.15s is usually a good default for a crossfade.")]
            public float crossFadeDuration;
        }

        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCrouch crouch;
        [SerializeField] private PlayerInteract interact;
        [SerializeField] private PlayerHealth health;

        [Header("Locomotion")]
        [SerializeField] private ClipConfig idleClip;
        [SerializeField] private ClipConfig runClip;

        [Header("Airborne")]
        [SerializeField] private ClipConfig jumpClip;
        [SerializeField] private ClipConfig fallTransitionClip;
        [SerializeField] private ClipConfig fallClip;
        [SerializeField] private ClipConfig landingClip;

        [Header("Interact")]
        [SerializeField] private ClipConfig interactClip;

        [Header("Crouch")]
        [SerializeField] private ClipConfig crouchIdleClip;
        [SerializeField] private ClipConfig crouchMoveClip;
        [SerializeField] private ClipConfig crouchInteractClip;
        [SerializeField] private ClipConfig crouchTransitionClip;

        [Header("Death")]
        [SerializeField] private ClipConfig deathTransitionClip;
        [SerializeField] private ClipConfig deathLoopClip;
        [SerializeField] private ClipConfig deathBanishClip;

        // Exposed read-only to the states — keeps the state classes out of
        // the serialization/inspector concerns.
        public ClipConfig Idle_Clip => idleClip;
        public ClipConfig Run_Clip => runClip;
        public ClipConfig Jump_Clip => jumpClip;
        public ClipConfig FallTransition_Clip => fallTransitionClip;
        public ClipConfig Fall_Clip => fallClip;
        public ClipConfig Landing_Clip => landingClip;
        public ClipConfig Interact_Clip => interactClip;
        public ClipConfig CrouchIdle_Clip => crouchIdleClip;
        public ClipConfig CrouchMove_Clip => crouchMoveClip;
        public ClipConfig CrouchInteract_Clip => crouchInteractClip;
        public ClipConfig CrouchTransition_Clip => crouchTransitionClip;
        public ClipConfig DeathTransition_Clip => deathTransitionClip;
        public ClipConfig DeathLoop_Clip => deathLoopClip;
        public ClipConfig DeathBanish_Clip => deathBanishClip;

        // State instances — created once, reused for the object's lifetime.
        public PlayerAnimState Idle { get; private set; }
        public PlayerAnimState Run { get; private set; }
        public PlayerAnimState Jump { get; private set; }
        public PlayerAnimState FallTransition { get; private set; }
        public PlayerAnimState Fall { get; private set; }
        public PlayerAnimState Landing { get; private set; }
        public PlayerAnimState Interact { get; private set; }
        public PlayerAnimState CrouchIdle { get; private set; }
        public PlayerAnimState CrouchMove { get; private set; }
        public PlayerAnimState CrouchInteract { get; private set; }
        public PlayerAnimState CrouchTransition { get; private set; }
        public PlayerAnimState DeathTransition { get; private set; }
        public PlayerAnimState DeathLoop { get; private set; }
        public PlayerAnimState DeathBanish { get; private set; }

        public event Action OnDeathBanishComplete;

        private ClipPlayer _clipPlayer;
        private PlayerAnimContext _ctx;
        private PlayerAnimState _current;

        private bool _isDead;
        private bool _wasCrouching;
        private bool _interactRequested;
        private bool _banishRequested;

        private void Awake()
        {
            _clipPlayer = new ClipPlayer();
            _clipPlayer.Initialize(GetComponent<Animator>());

            _ctx = new PlayerAnimContext
            {
                Movement = movement,
                Crouch = crouch,
                Interact = interact,
                Health = health,
                Player = _clipPlayer,
                Machine = this,
            };

            Idle = new IdleState(_ctx);
            Run = new RunState(_ctx);
            Jump = new JumpState(_ctx);
            FallTransition = new FallTransitionState(_ctx);
            Fall = new FallState(_ctx);
            Landing = new LandingState(_ctx);
            Interact = new InteractState(_ctx);
            CrouchIdle = new CrouchIdleState(_ctx);
            CrouchMove = new CrouchMoveState(_ctx);
            CrouchInteract = new CrouchInteractState(_ctx);
            CrouchTransition = new CrouchTransitionState(_ctx);
            DeathTransition = new DeathTransitionState(_ctx);
            DeathLoop = new DeathLoopState(_ctx);
            DeathBanish = new DeathBanishState(_ctx);

            GetComponent<Animator>().updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        private void OnEnable()
        {
            interact.OnInteracted += HandleInteracted;
        }

        private void OnDisable()
        {
            interact.OnInteracted -= HandleInteracted;
        }

        private void Start()
        {
            _wasCrouching = crouch.IsCrouching;
            ChangeState(movement.IsGrounded ? Idle : Fall);
        }

        private void Update()
        {
            if (_current == null) return;

            float dt = Time.unscaledDeltaTime;
            _clipPlayer.Tick(dt);

            if (TryHandleCrouchToggle()) return;
            if (TryHandleInteractRequest()) return;

            var next = _current.Tick(dt);
            if (next != null && next != _current)
                ChangeState(next);
        }

        private void ChangeState(PlayerAnimState next)
        {
            _current?.Exit();
            _current = next;
            _current.Enter();
        }

        private bool TryHandleCrouchToggle()
        {
            bool crouchingNow = crouch.IsCrouching;
            bool changed = crouchingNow != _wasCrouching;
            _wasCrouching = crouchingNow;

            if (!changed || _isDead || !movement.IsGrounded)
                return false;

            // Don't interrupt an in-progress interact/landing with a crouch
            // toggle — let it resolve into the right steady state naturally.
            if (_current == Interact || _current == CrouchInteract || _current == Landing)
                return false;

            ChangeState(CrouchTransition);
            return true;
        }

        private bool TryHandleInteractRequest()
        {
            if (!_interactRequested || _isDead)
                return false;

            _interactRequested = false;

            bool canInteractFromHere =
                _current == Idle || _current == Run || _current == CrouchIdle || _current == CrouchMove;

            if (!canInteractFromHere)
                return false;

            ChangeState(crouch.IsCrouching ? CrouchInteract : Interact);
            return true;
        }

        private void HandleInteracted() => _interactRequested = true;

        public void HandleDied()
        {
            if (_isDead) return;
            _isDead = true;
            ChangeState(DeathTransition);
        }

        /// <summary>Call from your respawn/death-UI flow when it's time to play the final banish clip.</summary>
        public void RequestBanish() => _banishRequested = true;

        public bool ConsumeBanishRequest()
        {
            if (!_banishRequested) return false;
            _banishRequested = false;
            return true;
        }

        public void RaiseDeathBanishComplete() => OnDeathBanishComplete?.Invoke();

        private void OnDestroy() => _clipPlayer?.Destroy();
    }
}
