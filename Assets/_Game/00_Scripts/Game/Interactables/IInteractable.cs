namespace Slafurry.Player
{
    /// <summary>
    /// Implement this on any object the player should be able to interact
    /// with via the Interact input (E). Attach the implementing component
    /// to a GameObject with a Collider2D on the interactable layer so
    /// PlayerInteract can detect it.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }
}