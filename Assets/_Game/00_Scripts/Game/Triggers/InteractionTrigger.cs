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

        [Header("Limit")]
        [Tooltip("Berapa kali interaksi ini boleh dipicu. Set 0 atau negatif = tanpa limit (bisa dipakai terus-menerus).")]
        [SerializeField] private int maxInteractions = 1;

        [Tooltip("Kalau limit sudah tercapai, apakah object ini otomatis di-nonaktifkan (SetActive(false))? Kalau tidak, object tetap ada tapi Interact() diam saja.")]
        [SerializeField] private bool disableWhenLimitReached = false;

        [Header("Events Tambahan (opsional)")]
        [Tooltip("Dipicu sekali saja, tepat saat limit interaksi baru saja tercapai.")]
        [SerializeField] private UnityEvent onLimitReached;

        public string Prompt => prompt;

        private int _interactionCount = 0;

        public bool IsLimitReached => maxInteractions > 0 && _interactionCount >= maxInteractions;

        public void Interact()
        {
            if (IsLimitReached) return;

            _interactionCount++;
            onInteract?.Invoke();

            if (IsLimitReached)
            {
                onLimitReached?.Invoke();

                if (disableWhenLimitReached)
                    gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reset hitungan interaksi, kalau butuh object ini bisa dipakai ulang lagi
        /// (misal setelah reset level / checkpoint).
        /// </summary>
        public void ResetInteractionCount()
        {
            _interactionCount = 0;
        }
    }
}