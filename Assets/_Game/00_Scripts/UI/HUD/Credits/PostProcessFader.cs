using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering; // Volume (URP / HDRP)

/// <summary>
/// PostProcessFader: smoothly fades a post-processing Volume's weight
/// (e.g. 1 -> 0 to slowly remove all its effects, or back again).
/// Works with URP and HDRP Volumes. Put it on the Volume's GameObject
/// or drag the Volume into the field.
/// </summary>
public class PostProcessFader : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to use the Volume on this GameObject")]
    public Volume volume;

    [Header("Fade Out")]
    public bool fadeOutOnStart = false;
    public float startDelay = 0f;
    public float duration = 3f;
    [Tooltip("X = time (0-1), Y = weight progress (0-1). Default is a smooth ease.")]
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool useUnscaledTime = false;
    [Tooltip("Disable the Volume once its weight reaches 0")]
    public bool disableVolumeWhenDone = false;

    [Header("Events")]
    public UnityEvent onFinished;

    private Coroutine _routine;

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();
    }

    private void Start()
    {
        if (fadeOutOnStart) FadeOut();
    }

    /// <summary>Fade weight from its current value down to 0.</summary>
    public void FadeOut() => FadeTo(0f, duration);

    /// <summary>Fade weight from its current value up to 1.</summary>
    public void FadeIn() => FadeTo(1f, duration);

    /// <summary>Fade weight from its current value to any target (0-1) over 'time' seconds.</summary>
    public void FadeTo(float targetWeight, float time)
    {
        if (volume == null)
        {
            Debug.LogWarning("PostProcessFader: no Volume assigned.", this);
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Fade(Mathf.Clamp01(targetWeight), Mathf.Max(0.0001f, time)));
    }

    /// <summary>Instantly set the weight (cancels any running fade).</summary>
    public void SetWeight(float weight)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        volume.enabled = true;
        volume.weight = Mathf.Clamp01(weight);
    }

    private IEnumerator Fade(float target, float time)
    {
        if (startDelay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(startDelay);
            else yield return new WaitForSeconds(startDelay);
        }

        volume.enabled = true;
        float from = volume.weight;
        float t = 0f;

        while (t < time)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = curve.Evaluate(Mathf.Clamp01(t / time));
            volume.weight = Mathf.Lerp(from, target, k);
            yield return null;
        }

        volume.weight = target;

        if (disableVolumeWhenDone && Mathf.Approximately(target, 0f))
            volume.enabled = false;

        _routine = null;
        onFinished?.Invoke();
    }
}