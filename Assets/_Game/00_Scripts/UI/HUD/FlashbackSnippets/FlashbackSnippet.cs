using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// FlashbackSnippet: world-space TextMeshPro text that types out, shakes, then fades out.
/// Works with TextMeshPro (3D) or TextMeshProUGUI on a World Space canvas.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class FlashbackSnippet : MonoBehaviour
{
    [Header("Text")]
    [TextArea(2, 5)]
    public string message = "I remember that night...";
    public bool playOnStart = true;

    [Header("Typing")]
    [Tooltip("Characters typed per second")]
    public float charsPerSecond = 20f;
    public float startDelay = 0.2f;

    [Header("Shake")]
    [Tooltip("How far each character is displaced (local text units, not world units)")]
    public float shakeAmount = 3f;
    [Tooltip("How fast the shake moves")]
    public float shakeSpeed = 30f;
    [Tooltip("Keep shaking while the text fades out")]
    public bool shakeDuringFade = true;

    [Header("Fade Out")]
    [Tooltip("If off, the text stays visible after typing and never fades")]
    public bool fadeOut = true;
    [Tooltip("Only used when Fade Out is off: keep shaking after typing finishes")]
    public bool shakeWhenStaying = true;
    [Tooltip("Pause after typing finishes, before the fade starts")]
    public float holdDuration = 1.5f;
    public float fadeDuration = 1f;

    [Header("World Space")]
    [Tooltip("Rotate the text to always face the camera")]
    public bool faceCamera = false;
    [Tooltip("Leave empty to use Camera.main")]
    public Camera targetCamera;
    [Tooltip("Disable the GameObject once the fade is done")]
    public bool deactivateWhenDone = false;

    [Header("Events")]
    public UnityEvent onTypingFinished;
    public UnityEvent onFadeFinished;

    private TMP_Text _text;
    private float _alpha = 1f;
    private bool _shaking;
    private Coroutine _routine;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (playOnStart) Play(message);
        else _text.maxVisibleCharacters = 0;
    }

    /// <summary>Start the snippet with a new message (can be called from other scripts).</summary>
    private void Play(string newMessage)
    {
        if (_routine != null) StopCoroutine(_routine);
        message = newMessage;
        _routine = StartCoroutine(Sequence());
    }

    public void Play() => Play(message);

    private IEnumerator Sequence()
    {
        _alpha = 1f;
        _shaking = true;
        _text.text = message;
        _text.maxVisibleCharacters = 0;
        _text.ForceMeshUpdate();

        yield return new WaitForSeconds(startDelay);

        // --- Typing ---
        int total = _text.textInfo.characterCount;
        float timer = 0f;
        int visible = 0;

        while (visible < total)
        {
            timer += Time.deltaTime * charsPerSecond;
            visible = Mathf.Min(total, Mathf.FloorToInt(timer));
            _text.maxVisibleCharacters = visible;
            yield return null;
        }

        onTypingFinished?.Invoke();

        // --- Stay visible (no fade) ---
        if (!fadeOut)
        {
            _shaking = shakeWhenStaying;
            _routine = null;
            yield break;
        }

        // --- Hold ---
        yield return new WaitForSeconds(holdDuration);

        // --- Fade out ---
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        _alpha = 0f;
        if (!shakeDuringFade) _shaking = false;
        onFadeFinished?.Invoke();
        _routine = null;

        if (deactivateWhenDone) gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Billboard: face the camera in world space
        if (faceCamera)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }

        // Runs after the TMP mesh is regenerated, so it's safe to modify vertices here.
        _text.ForceMeshUpdate();
        TMP_TextInfo info = _text.textInfo;
        int visibleCount = Mathf.Min(_text.maxVisibleCharacters, info.characterCount);
        float time = Time.unscaledTime * shakeSpeed;
        byte alphaByte = (byte)Mathf.RoundToInt(_alpha * 255f);

        for (int i = 0; i < visibleCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            int matIndex = c.materialReferenceIndex;
            int vIndex = c.vertexIndex;
            Vector3[] verts = info.meshInfo[matIndex].vertices;
            Color32[] colors = info.meshInfo[matIndex].colors32;

            // Smooth random offset per character (Perlin noise)
            Vector3 offset = Vector3.zero;
            if (_shaking)
            {
                float x = (Mathf.PerlinNoise(i * 12.9898f, time) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(i * 78.233f, time + 100f) - 0.5f) * 2f;
                offset = new Vector3(x, y, 0f) * shakeAmount;
            }

            for (int j = 0; j < 4; j++)
            {
                verts[vIndex + j] += offset;
                Color32 col = colors[vIndex + j];
                col.a = (byte)(col.a * alphaByte / 255);
                colors[vIndex + j] = col;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}