using UnityEngine;
using UnityEngine.Events;

public class DetectArea : MonoBehaviour
{
    [Header("Trigger Invoker")]
    [SerializeField] private UnityEvent onTriggerEnter;
    [SerializeField] private UnityEvent onTriggerStay;
    [SerializeField] private UnityEvent onTriggerExit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        onTriggerEnter?.Invoke();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        onTriggerStay?.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        onTriggerExit?.Invoke();
    }
}