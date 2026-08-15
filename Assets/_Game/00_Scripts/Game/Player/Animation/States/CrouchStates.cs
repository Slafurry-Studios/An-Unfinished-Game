namespace Slafurry.Player.Animation
{
    public class CrouchIdleState : PlayerAnimState
    {
        public CrouchIdleState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.CrouchIdle_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            // No dedicated crouch-air clips exist in the asset set, so a
            // player who walks off a ledge while crouched just falls using
            // the normal Fall_Transition/Fall/Landing chain.
            if (!Ctx.Movement.IsGrounded)
                return Ctx.Machine.FallTransition;

            if (Ctx.IsMoving)
                return Ctx.Machine.CrouchMove;

            return null;
        }
    }

    public class CrouchMoveState : PlayerAnimState
    {
        public CrouchMoveState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.CrouchMove_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.Machine.FallTransition;

            if (!Ctx.IsMoving)
                return Ctx.Machine.CrouchIdle;

            return null;
        }
    }

    /// <summary>
    /// Played once whenever PlayerCrouch.IsCrouching flips while grounded
    /// (either direction — stand-to-crouch or crouch-to-stand). The state
    /// machine triggers this as a global interrupt, not from another state's
    /// Tick(); see PlayerAnimationStateMachine.TryHandleCrouchToggle.
    /// </summary>
    public class CrouchTransitionState : PlayerAnimState
    {
        public CrouchTransitionState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.CrouchTransition_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.ResolveGroundedState();

            return null;
        }
    }
}
