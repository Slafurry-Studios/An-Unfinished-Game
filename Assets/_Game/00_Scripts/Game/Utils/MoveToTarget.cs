using UnityEngine;
using UnityEngine.Events;

public class MoveToTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float threshold = 0.1f;
    [SerializeField] private bool playOnEnable = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onArrived;

    private bool _isMoving;

    private void OnEnable()
    {
        if (playOnEnable)
            StartMove();
    }

    public void StartMove()
    {
        _isMoving = true;
    }

    public void StopMove()
    {
        _isMoving = false;
    }

    private void Update()
    {
        if (!_isMoving || target == null) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) <= threshold)
        {
            _isMoving = false;
            onArrived?.Invoke();
        }
    }
}
