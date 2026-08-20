using Slafurry.System.Audio;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private Transform destination;

    [Header("References")]
    [SerializeField] private DetectArea detectArea;

    [Header("Audio")]
    [SerializeField] private string category = "Puzzle";
    [SerializeField] private string useSound = "Glitch_Portal";

    public void Teleport()
    {
        if (destination == null)
            return;

        if (detectArea == null)
            return;

        if (detectArea.CurrentTarget == null)
            return;

        detectArea.CurrentTarget.transform.position = destination.position;
        Audio.PlaySFX2D(category, useSound);
    }
}