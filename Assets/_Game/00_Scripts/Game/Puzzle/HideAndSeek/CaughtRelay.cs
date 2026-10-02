using UnityEngine;

// Relay: otomatis dipasang ke tiap collider di list
public class CaughtRelay : MonoBehaviour
{
    AutoTurnLoop owner;
    public void Init(AutoTurnLoop o) => owner = o;

    // Stay: pemain yang sudah di dalam saat collider menyala tetap terdeteksi
    void OnTriggerStay2D(Collider2D other)
    {
        if (owner != null && other.CompareTag("Player")) owner.OnPlayerInside();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (owner != null && other.CompareTag("Player")) owner.OnPlayerLeft();
    }
}