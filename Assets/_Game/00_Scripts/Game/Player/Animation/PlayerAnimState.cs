using UnityEngine;

namespace Slafurry.Player.Animation
{
    /// <summary>
    /// Shared references every animation state needs to decide transitions
    /// and push playback. Built once by PlayerAnimationStateMachine and
    /// handed to every state instance.
    /// </summary>
    public class PlayerAnimContext
    {
        public PlayerMovement Movement;
        public PlayerCrouch Crouch;
        public PlayerInteract Interact;
        public PlayerHealth Health;
        public ClipPlayer Player;
        public PlayerAnimationStateMachine Machine;

        public const float MoveInputThreshold = 0.05f;

        // IMPORTANT: Movement.Velocity is a WORLD-SPACE vector
        // (gravityDirection * alongGravity + rightAxis * alongRight).
        // rightAxis/gravityDirection rotate with the player whenever
        // gravity changes, so raw Velocity.x/.y only means "sideways" /
        // "vertical" while gravity happens to be Down. Use the
        // gravity-relative projections exposed by PlayerMovement instead
        // (SpeedAlongRight / SpeedAlongGravity), which stay correct no
        // matter which way gravity is currently pointing.
        public bool IsMoving => Mathf.Abs(Movement.SpeedAlongRight) > MoveInputThreshold;

        /// <summary>
        /// Picks the correct "steady state" (Idle/Run/CrouchIdle/CrouchMove)
        /// based on current crouch + movement — used by every one-shot state
        /// (Landing, Interact, CrouchTransition, ...) to decide where to land
        /// once its clip finishes.
        /// </summary>
        public PlayerAnimState ResolveGroundedState()
        {
            if (Crouch.IsCrouching)
                return IsMoving ? Machine.CrouchMove : Machine.CrouchIdle;

            return IsMoving ? Machine.Run : Machine.Idle;
        }

        /// <summary>
        /// Picks Jump vs FallTransition when the player leaves the ground —
        /// Jump if it happened because of an upward jump impulse, otherwise
        /// FallTransition (walked off a ledge, no jump).
        ///
        /// SpeedAlongGravity is the velocity component along gravityDirection:
        /// negative means moving AGAINST gravity (jumping "up" relative to
        /// the player, regardless of which world direction that currently is),
        /// positive means moving WITH gravity (falling).
        /// </summary>
        public PlayerAnimState ResolveAirborneEntryState()
        {
            return Movement.SpeedAlongGravity < 0f ? Machine.Jump : Machine.FallTransition;
        }
    }

    /// <summary>
    /// A single node in the code-driven animation state machine. There is no
    /// Animator Controller / Mecanim graph involved anywhere — every
    /// transition rule lives here in code, and playback goes straight to the
    /// Playables-based ClipPlayer.
    /// </summary>
    public abstract class PlayerAnimState
    {
        protected readonly PlayerAnimContext Ctx;
        protected PlayerAnimState(PlayerAnimContext ctx) => Ctx = ctx;

        /// <summary>Called once when the state machine switches into this state.</summary>
        public abstract void Enter();

        /// <summary>
        /// Called every frame while this state is active. Return the state to
        /// switch to next, or null to keep ticking this one.
        /// </summary>
        public abstract PlayerAnimState Tick(float deltaTime);

        /// <summary>Called once when leaving this state, before the next state's Enter().</summary>
        public virtual void Exit() { }
    }
}