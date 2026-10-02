using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class TurnWithWindup : MonoBehaviour
{
    [SerializeField] Transform visual;            // child sprite
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] bool artFacesRight = false;
    [SerializeField] float windupTime = 0.4f;
    [SerializeField] float squash = 0.85f;

    public UnityEvent onWindupStart, onTurned;
    bool facingRight = true;
    Coroutine routine;

    public void FaceRight() => TurnTo(true);
    public void FaceLeft()  => TurnTo(false);

    void TurnTo(bool right)
    {
        if (right == facingRight) return;       // sudah menghadap ke situ
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Turn(right));
    }

    IEnumerator Turn(bool right)
    {
        onWindupStart.Invoke();
        Vector3 baseScale = visual.localScale;
        float t = 0;
        while (t < windupTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t / windupTime * Mathf.PI);   // turun lalu balik
            visual.localScale = new Vector3(baseScale.x * Mathf.Lerp(1f, squash, k), baseScale.y, baseScale.z);
            yield return null;
        }
        visual.localScale = baseScale;
        facingRight = right;
        sprite.flipX = (right != artFacesRight);
        onTurned.Invoke();
        routine = null;
    }
}