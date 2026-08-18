using Slafurry.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Interaction
{
    public class InteractionTrigger : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private UnityEvent onInteract;

        public void Interact()
        {
            onInteract?.Invoke();
        }
    }
}