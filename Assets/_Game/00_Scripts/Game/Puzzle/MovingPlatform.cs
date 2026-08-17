using UnityEngine;
using System.Collections;

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

    private bool movingToEnd = true;
    private bool isWaiting = false;

    private void Start()
    {
        if (startPos != null)
        {
            transform.position = startPos.position;
        }
    }

    private void Update()
    {
        if (!isActive || isWaiting)
            return;

        if (startPos == null || endPos == null)
            return;

        Transform target = movingToEnd ? endPos : startPos;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) <= 0.01f)
        {
            transform.position = target.position;

            if (movingToEnd)
            {
                // Sampai di end
                if (loop)
                {
                    StartCoroutine(WaitAndReverse());
                }
                else
                {
                    isActive = false;
                }
            }
            else
            {
                // Sampai di start
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
    }

    private IEnumerator WaitAndReverse()
    {
        isWaiting = true;

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