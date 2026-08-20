using UnityEngine;
using UnityEngine.Events;

public class DebugButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;
    [SerializeField] private UnityEvent triggerEvent;

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            triggerEvent.Invoke();
        }
    }
}