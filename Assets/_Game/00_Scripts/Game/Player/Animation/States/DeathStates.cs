namespace Slafurry.Player.Animation
{
    /// <summary>
    /// Entered once, as a global interrupt, when PlayerHealth.OnDied fires —
    /// see PlayerAnimationStateMachine.HandleDied. Overrides whatever state
    /// was active, and nothing else can interrupt it afterwards.
    /// </summary>
    public class DeathTransitionState : PlayerAnimState
    {
        public DeathTransitionState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.DeathTransition_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (Ctx.Player.IsCurrentClipFinished)
                return Ctx.Machine.DeathLoop;

            return null;
        }
    }

    public class DeathLoopState : PlayerAnimState
    {
        public DeathLoopState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            var c = Ctx.Machine.DeathLoop_Clip;
            Ctx.Player.Play(c.clip, true, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            // Call PlayerAnimationStateMachine.RequestBanish() from wherever
            // your respawn/death-screen flow decides it's time (e.g. after
            // a delay, or once the death UI has faded in).
            if (Ctx.Machine.ConsumeBanishRequest())
                return Ctx.Machine.DeathBanish;

            return null;
        }
    }

    /// <summary>Final one shot, then holds on its last frame indefinitely.</summary>
    public class DeathBanishState : PlayerAnimState
    {
        private bool _completedFired;

        public DeathBanishState(PlayerAnimContext ctx) : base(ctx) { }

        public override void Enter()
        {
            _completedFired = false;
            var c = Ctx.Machine.DeathBanish_Clip;
            Ctx.Player.Play(c.clip, false, c.crossFadeDuration);
        }

        public override PlayerAnimState Tick(float deltaTime)
        {
            if (!_completedFired && Ctx.Player.IsCurrentClipFinished)
            {
                _completedFired = true;
                Ctx.Machine.RaiseDeathBanishComplete();
            }

            return null;
        }
    }
}
