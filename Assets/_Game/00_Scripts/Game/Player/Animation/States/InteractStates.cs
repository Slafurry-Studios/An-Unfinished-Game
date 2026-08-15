namespace Slafurry.Player.Animation
{
    /// <summary>
    /// Triggered as a global interrupt when PlayerInteract fires its
    /// OnInteracted event while standing still on the ground; see
    /// PlayerAnimationStateMachine.TryHandleInteractRequest.
    /// </summary>
    public class InteractState : PlayerAnimState
    {
        public InteractState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.Interact_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.ResolveAirborneEntryState();

            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.ResolveGroundedState();

            return null;
        }
    }

    public class CrouchInteractState : PlayerAnimState
    {
        public CrouchInteractState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.CrouchInteract_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!Ctx.Movement.IsGrounded)
                return Ctx.Machine.FallTransition;

            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.ResolveGroundedState();

            return null;
        }
    }
}
