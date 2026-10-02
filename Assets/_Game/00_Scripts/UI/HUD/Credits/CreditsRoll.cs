using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// CreditsRoll: scrolls a TextMeshPro text (like movie credits).
/// Works with TextMeshProUGUI (Canvas) and TextMeshPro (3D / world space).
///
/// Setup: place the text so it STARTS where you want it to appear (e.g. just below
/// the visible area). On play it moves in 'direction' until it has travelled
/// (text height + extraDistance), then fires onFinished.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class CreditsRoll : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("Optional. If filled, this replaces the text component's content on start.")]
    [TextArea(5, 25)]
    public string credits = "";
    public bool playOnStart = true;

    [Header("Scroll")]
    [Tooltip("Local units per second")]
    public float scrollSpeed = 50f;
    [Tooltip("Local-space direction. Up = (0,1,0)")]
    public Vector3 direction = Vector3.up;
    public float startDelay = 1f;

    [Header("Distance")]
    [Tooltip("Travel distance = text height + Extra Distance. Turn off to use Manual Distance.")]
    public bool autoDistance = true;
    [Tooltip("Add the height of your visible area so the last line fully leaves the screen")]
    public float extraDistance = 500f;
    public float manualDistance = 1000f;

    [Header("Fast Forward")]
    public float fastForwardMultiplier = 4f;
    [Tooltip("Hold this key to scroll faster (legacy Input Manager only). None = disabled.")]
    public KeyCode fastForwardKey = KeyCode.Space;

    [Header("End Behaviour")]
    public bool loop = false;
    public float loopDelay = 1f;
    public bool deactivateWhenDone = false;

    [Header("Events")]
    public UnityEvent onFinished;

    private TMP_Text _text;
    private Vector3 _startLocalPos;
    private Coroutine _routine;
    private bool _paused;
    private bool _fastForward;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _startLocalPos = transform.localPosition;
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    /// <summary>Start (or restart) the credits from the beginning.</summary>
    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Roll());
    }

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;

    /// <summary>Call from a UI button / your own input code to speed up.</summary>
    public void SetFastForward(bool value) => _fastForward = value;

    /// <summary>Stop and put the text back at its start position.</summary>
    public void ResetRoll()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        transform.localPosition = _startLocalPos;
    }

    private IEnumerator Roll()
    {
        if (!string.IsNullOrEmpty(credits)) _text.text = credits;
        Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;

        do
        {
            transform.localPosition = _startLocalPos;
            _text.ForceMeshUpdate();

            float distance = autoDistance
                ? _text.preferredHeight + extraDistance
                : manualDistance;

            yield return new WaitForSeconds(startDelay);

            float moved = 0f;
            while (moved < distance)
            {
                if (!_paused)
                {
                    float mult = (_fastForward || IsFastForwardKeyHeld()) ? fastForwardMultiplier : 1f;
                    float step = scrollSpeed * mult * Time.deltaTime;
                    transform.localPosition += dir * step;
                    moved += step;
                }
                yield return null;
            }

            onFinished?.Invoke();

            if (loop) yield return new WaitForSeconds(loopDelay);
        }
        while (loop);

        _routine = null;
        if (deactivateWhenDone) gameObject.SetActive(false);
    }

    private bool IsFastForwardKeyHeld()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return fastForwardKey != KeyCode.None && Input.GetKey(fastForwardKey);
#else
        return false; // New Input System: use SetFastForward(true/false) instead.
#endif
    }
}