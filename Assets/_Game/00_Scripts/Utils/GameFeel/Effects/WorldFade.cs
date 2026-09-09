using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Utils.GameFeel
{
    /// <summary>
    /// Simple one-shot fade in/out for world-space SpriteRenderer(s). Use for
    /// enemy spawn/despawn, pickup fade-out, or any world object that needs
    /// to fade rather than blink. UI equivalent: see UIFade (CanvasGroup-based).
    /// </summary>
    public class WorldFade : MonoBehaviour, IGameFeelEffect
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer[] renderers;

        [Header("Fade")]
        [SerializeField] private float duration = 0.3f;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Options")]
        [SerializeField] private bool disableCollidersWhileHidden = false;
        [SerializeField] private Collider2D[] colliders;

        [Header("Trigger")]
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool fadeInOnEnable = true; // true = fade in, false = fade out

        [Header("Events")]
        [SerializeField] private UnityEvent onFadeInComplete;
        [SerializeField] private UnityEvent onFadeOutComplete;

        private Coroutine _routine;
        private bool _isFadingIn;
        private float _currentAlpha;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>();

            if (disableCollidersWhileHidden && (colliders == null || colliders.Length == 0))
                colliders = GetComponentsInChildren<Collider2D>();

            _currentAlpha = renderers.Length > 0 ? renderers[0].color.a : 1f;
        }

        private void OnEnable()
        {
            _routine = null;

            if (playOnEnable)
            {
                // Mulai dari titik lawan target, biar transisinya kelihatan (bukan cuma diam di alpha akhir).
                SetImmediate(fadeInOnEnable ? 0f : 1f);

                if (fadeInOnEnable)
                    FadeIn();
                else
                    FadeOut();
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        // ---- Dipanggil dari UnityEvent (tanpa parameter) ----
        public void FadeIn() => FadeIn(duration);

        public void FadeOut() => FadeOut(duration);

        // ---- Dipanggil dari kode (dengan override duration) ----
        public void FadeIn(float overrideDuration)
        {
            _isFadingIn = true;
            StartFade(_currentAlpha, 1f, overrideDuration);
        }

        public void FadeOut(float overrideDuration)
        {
            _isFadingIn = false;
            StartFade(_currentAlpha, 0f, overrideDuration);
        }

        public void SetImmediate(float alpha)
        {
            if (_routine != null) StopCoroutine(_routine);
            ApplyAlpha(alpha);
        }

        // ---- IGameFeelEffect ----
        public void PlayEffect() => FadeIn();

        public void StopEffect()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
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
            _currentAlpha = alpha;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color;
                c.a = alpha;
                renderers[i].color = c;
            }

            if (disableCollidersWhileHidden && colliders != null)
            {
                bool visible = alpha > 0.01f;
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        colliders[i].enabled = visible;
                }
            }
        }
    }
}