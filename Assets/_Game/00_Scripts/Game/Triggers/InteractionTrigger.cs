using Slafurry.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Interaction
{
    public class InteractionTrigger : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string prompt = "Interact";
        [SerializeField] private UnityEvent onInteract;

        public string Prompt => prompt;

        public void Interact()
        {
            onInteract?.Invoke();
        }
    }
}