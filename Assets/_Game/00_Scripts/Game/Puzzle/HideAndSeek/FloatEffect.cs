using UnityEngine;

public class FloatEffect : MonoBehaviour
{
    [SerializeField] float amplitude = 0.1f, speed = 2f;
    Vector3 start;
    void Awake() => start = transform.localPosition;
    void Update() =>
        transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
}