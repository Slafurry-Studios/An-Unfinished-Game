namespace Slafurry.Player
{
    public interface IInteractable
    {
        string Prompt { get; }

        void Interact();
    }
}