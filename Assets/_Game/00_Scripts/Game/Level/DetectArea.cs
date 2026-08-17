using UnityEngine;
using UnityEngine.Events;

public class DetectArea : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private string targetTag = "Player";

    [Header("Trigger Invoker")]
    [SerializeField] private UnityEvent onTriggerEnter;
    [SerializeField] private UnityEvent onTriggerStay;
    [SerializeField] private UnityEvent onTriggerExit;

    public GameObject CurrentTarget { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        CurrentTarget = other.gameObject;
        onTriggerEnter?.Invoke();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        CurrentTarget = other.gameObject;
        onTriggerStay?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        if (CurrentTarget == other.gameObject)
            CurrentTarget = null;

        onTriggerExit?.Invoke();
    }
}