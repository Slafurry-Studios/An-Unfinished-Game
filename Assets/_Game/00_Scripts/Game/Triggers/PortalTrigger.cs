using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private Transform destination;

    [Header("References")]
    [SerializeField] private DetectArea detectArea;

    public void Teleport()
    {
        if (destination == null)
            return;

        if (detectArea == null)
            return;

        if (detectArea.CurrentTarget == null)
            return;

        detectArea.CurrentTarget.transform.position = destination.position;
    }
}