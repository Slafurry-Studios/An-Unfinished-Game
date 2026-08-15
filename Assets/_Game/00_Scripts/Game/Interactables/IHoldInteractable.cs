namespace Slafurry.Player
{
    /// <summary>
    /// Implement this instead of the plain IInteractable when the object
    /// requires the player to hold Interact for a duration rather than tap
    /// it (e.g. a heavy door, a charge-up mechanism). Interact() is called
    /// automatically once the hold completes — you don't need to call it
    /// yourself.
    /// </summary>
    public interface IHoldInteractable : IInteractable
    {
        /// <summary>Seconds the input must be held before Interact() fires.</summary>
        float HoldDuration { get; }

        /// <summary>Called every frame while holding. 0-1, for progress UI (e.g. a radial fill).</summary>
        void OnHoldProgress(float normalizedProgress);

        /// <summary>Called if the hold is released early or interrupted (e.g. player walks out of range) before completing.</summary>
        void OnHoldCanceled();
    }
}