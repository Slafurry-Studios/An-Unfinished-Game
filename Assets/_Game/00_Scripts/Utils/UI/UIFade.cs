using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Utils.UI
{
    /// <summary>
    /// Simple one-shot fade in/out for a CanvasGroup. Use for screen
    /// transitions, tooltips appearing/disappearing, or any UI element
    /// that needs to fade rather than blink (see UIBlink for repeating loop).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFade : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool disableInteractionWhileHidden = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onFadeInComplete;
        [SerializeField] private UnityEvent onFadeOutComplete;

        private Coroutine _routine;
        private bool _isFadingIn;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        // ---- Dipanggil dari UnityEvent (tanpa parameter) ----
        public void FadeIn() => FadeIn(duration);

        public void FadeOut() => FadeOut(duration);

        // ---- Dipanggil dari kode (dengan override duration) ----
        public void FadeIn(float overrideDuration)
        {
            _isFadingIn = true;
            StartFade(canvasGroup.alpha, 1f, overrideDuration);
        }

        public void FadeOut(float overrideDuration)
        {
            _isFadingIn = false;
            StartFade(canvasGroup.alpha, 0f, overrideDuration);
        }

        public void SetImmediate(float alpha)
        {
            if (_routine != null) StopCoroutine(_routine);
            ApplyAlpha(alpha);
        }

        private void StartFade(float from, float to, float dur)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine(from, to, dur));
        }

        private IEnumerator FadeRoutine(float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                ApplyAlpha(Mathf.Lerp(from, to, dur > 0 ? t / dur : 1f));
                yield return null;
            }
            ApplyAlpha(to);
            _routine = null;

            if (_isFadingIn)
                onFadeInComplete?.Invoke();
            else
                onFadeOutComplete?.Invoke();
        }

        private void ApplyAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
            if (disableInteractionWhileHidden)
            {
                bool visible = alpha > 0.01f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }
    }
}