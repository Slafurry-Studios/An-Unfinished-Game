namespace Slafurry.Player.Animation
{
    /// <summary>Played on the way up, right after the jump impulse is applied.</summary>
    public class JumpState : PlayerAnimState
    {
        public JumpState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Jump_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (Ctx.Movement.IsGrounded)
                return Ctx.Machine.Landing; // very short hop, landed before the clip mattered

            if (Ctx.Movement.Velocity.y <= 0f)
                return Ctx.Machine.FallTransition; // apex reached, start descending

            return null;
        }
    }

    /// <summary>Bridges Jump -> Fall (or ledge-walk-off -> Fall) — one shot, then loops into Fall.</summary>
    public class FallTransitionState : PlayerAnimState
    {
        public FallTransitionState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.FallTransition_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (Ctx.Movement.IsGrounded)
                return Ctx.Machine.Landing;

            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.Machine.Fall;

            return null;
        }
    }

    /// <summary>Looping descent.</summary>
    public class FallState : PlayerAnimState
    {
        public FallState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Fall_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (Ctx.Movement.IsGrounded)
                return Ctx.Machine.Landing;

            return null;
        }
    }

    /// <summary>One shot on touching ground, then resolves into Idle/Run/CrouchIdle/CrouchMove.</summary>
    public class LandingState : PlayerAnimState
    {
        public LandingState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Landing_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
            Ctx.Movement.DisableLeftMovement();
            Ctx.Movement.DisableRightMovement();
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.Machine.Jump; // jumped again immediately out of the landing pose

            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.ResolveGroundedState();

            return null;
        }

        public override void Exit()
        {
            Ctx.Movement.EnableLeftMovement();
            Ctx.Movement.EnableRightMovement();
        }
    }
}
