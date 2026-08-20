using UnityEngine;
using System.Collections;
using Slafurry.System.Audio;

public class RotatingJoint : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private Transform pivot;
    [SerializeField] private float rotation = 90f;
    [SerializeField] private float speed = 90f;

    [Header("Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private float loopDelay = 1f;
    [SerializeField] private bool reverse = false;
    [SerializeField] private bool reverseOnLoop = false;

    [Header("Audio")]
    [SerializeField] private string category = "Puzzle";
    [SerializeField] private string moveSound = "Joint";

    private float currentRotation;
    private bool isWaiting;
    private float direction;

    private void Start()
    {
        direction = reverse ? -1f : 1f;
    }

    private void Update()
    {
        if (!isActive || isWaiting)
            return;

        if (pivot == null || rotation <= 0f || speed <= 0f)
            return;

        float rotationStep = speed * Time.deltaTime;
        currentRotation += rotationStep;

        if (currentRotation >= rotation)
        {
            rotationStep -= currentRotation - rotation;
            currentRotation = 0f;

            RotateAroundPivot(rotationStep * direction);

            if (loop)
            {
                if (reverseOnLoop)
                    direction *= -1f;

                StartCoroutine(LoopDelay());
            }
            else
            {
                isActive = false;
            }

            return;
        }

        RotateAroundPivot(rotationStep * direction);
    }

    private void RotateAroundPivot(float angle)
    {
        transform.RotateAround(
            pivot.position,
            Vector3.forward,
            angle
        );
    }

    private IEnumerator LoopDelay()
    {
        isWaiting = true;
        yield return new WaitForSeconds(loopDelay);

        Audio.PlaySFX3D(category, moveSound, transform.position);
        isWaiting = false;
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }

    public void ToggleActive()
    {
        isActive = !isActive;
    }
}