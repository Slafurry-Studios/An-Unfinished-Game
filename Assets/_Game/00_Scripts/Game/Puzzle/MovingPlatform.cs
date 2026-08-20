using UnityEngine;
using System.Collections;
using Slafurry.System.Audio;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float speed = 2f;

    [Header("Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool loop = true;
    [SerializeField] private float loopDelay = 1f;

    [Header("Audio")]
    [SerializeField] private string category = "Puzzle";
    [SerializeField] private string moveSound = "Platform";

    private Rigidbody2D _rb;
    private Vector2 _lastPosition;

    private bool movingToEnd = true;
    private bool isWaiting = false;

    public Vector2 DeltaMovement { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        if (startPos != null)
            _rb.position = startPos.position;

        _lastPosition = _rb.position;
    }

    private void FixedUpdate()
    {
        DeltaMovement = Vector2.zero;

        if (!isActive || isWaiting)
            return;

        if (startPos == null || endPos == null)
            return;

        Transform target = movingToEnd ? endPos : startPos;

        Vector2 newPos = Vector2.MoveTowards(
            _rb.position,
            target.position,
            speed * Time.fixedDeltaTime
        );

        _rb.MovePosition(newPos);

        DeltaMovement = newPos - _lastPosition;
        _lastPosition = newPos;

        if (Vector2.Distance(newPos, target.position) <= 0.01f)
        {
            if (loop)
            {
                StartCoroutine(WaitAndReverse());
            }
            else
            {
                isActive = false;
            }
        }
    }

    private IEnumerator WaitAndReverse()
    {
        isWaiting = true;

        Audio.PlaySFX3D(category, moveSound, transform.position);

        yield return new WaitForSeconds(loopDelay);

        movingToEnd = !movingToEnd;
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