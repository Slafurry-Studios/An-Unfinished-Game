namespace Slafurry.Player.Animation
{
    public class IdleState : PlayerAnimState
    {
        public IdleState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Idle_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.ResolveAirborneEntryState();

            if (Ctx.IsMoving)
                return Ctx.Machine.Run;

            return null;
        }
    }

    public class RunState : PlayerAnimState
    {
        public RunState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Run_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.ResolveAirborneEntryState();

            if (!Ctx.IsMoving)
                return Ctx.Machine.Idle;

            return null;
        }
    }
}
