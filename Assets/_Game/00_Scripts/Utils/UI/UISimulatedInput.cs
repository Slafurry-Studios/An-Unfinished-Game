using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Slafurry.System.Audio;

namespace Slafurry.Utils.UI
{
    /// <summary>
    /// Cutscene: moves two hand images randomly while intensity ramps up.
    /// After duration, stops and fires OnFinished. All positions reset.
    /// </summary>
    public class UISimulatedInput : MonoBehaviour
    {
        [Header("Keyboard Hand")]
        [SerializeField] private RectTransform keyboardHand;
        [SerializeField] private RectTransform keyboardBounds;
        [SerializeField] private float keyboardIntensityMult = 1.3f;

        [Header("Mouse Hand")]
        [SerializeField] private RectTransform mouseHand;
        [SerializeField] private RectTransform mouseBounds;
        [SerializeField] private float mouseIntensityMult = 0.7f;

        [Header("Screen")]
        [SerializeField] private Image screenImage;
        [SerializeField] private Sprite[] screenSprites;

        [Header("Audio")]
        [SerializeField] private string sfxCategory = "UI";
        [SerializeField] private string keyboardSFX = "keyboard";
        [SerializeField] private string mouseSFX = "mouse";

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 300f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Timing")]
        [SerializeField] private float minPause = 0.3f;
        [SerializeField] private float maxPause = 2f;

        [Header("Cutscene")]
        [SerializeField] private float duration = 5f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);
        [SerializeField] private float resetDuration = 0.5f;

        [Header("Events")]
        [SerializeField] private UnityEvent onFinished;

        private Coroutine _cutsceneRoutine;
        private Vector2 _keyboardStart;
        private Vector2 _mouseStart;

        private void OnEnable()
        {
            if (keyboardHand != null) _keyboardStart = keyboardHand.anchoredPosition;
            if (mouseHand != null) _mouseStart = mouseHand.anchoredPosition;

            Play();
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play()
        {
            Stop();
            _cutsceneRoutine = StartCoroutine(CutsceneRoutine());
        }

        public void Stop()
        {
            if (_cutsceneRoutine != null) { StopCoroutine(_cutsceneRoutine); _cutsceneRoutine = null; }
            ResetPositions();
        }

        private IEnumerator CutsceneRoutine()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float intensity = intensityCurve.Evaluate(t);

                Coroutine kbRoutine = null;
                Coroutine msRoutine = null;

                if (keyboardHand != null && keyboardBounds != null)
                    kbRoutine = StartCoroutine(HandRoutine(keyboardHand, keyboardBounds, Mathf.Clamp01(intensity * keyboardIntensityMult)));
                if (mouseHand != null && mouseBounds != null)
                    msRoutine = StartCoroutine(MouseRoutine(Mathf.Clamp01(intensity * mouseIntensityMult)));

                float pause = Mathf.Lerp(maxPause, minPause, intensity);
                float waitEnd = Time.unscaledTime + pause + Random.Range(0f, 0.3f);

                while (Time.unscaledTime < waitEnd)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (kbRoutine != null) StopCoroutine(kbRoutine);
                if (msRoutine != null) StopCoroutine(msRoutine);
            }

            _cutsceneRoutine = null;
            yield return ResetPositionsSmooth();
            onFinished?.Invoke();
        }

        private IEnumerator HandRoutine(RectTransform hand, RectTransform bounds, float intensity)
        {
            if (AudioSystem.Instance != null) Audio.PlaySFX2D(sfxCategory, keyboardSFX);
            Vector2 target = RandomPointInBounds(bounds);
            yield return MoveTo(hand, target, intensity);
        }

        private IEnumerator MouseRoutine(float intensity)
        {
            if (AudioSystem.Instance != null) Audio.PlaySFX2D(sfxCategory, mouseSFX);
            Vector2 target = RandomPointInBounds(mouseBounds);
            yield return MoveTo(mouseHand, target, intensity);
            ChangeScreen();
        }

        private void ChangeScreen()
        {
            if (screenImage == null || screenSprites == null || screenSprites.Length == 0)
                return;

            screenImage.sprite = screenSprites[Random.Range(0, screenSprites.Length)];
        }

        private void ResetPositions()
        {
            if (keyboardHand != null) keyboardHand.anchoredPosition = _keyboardStart;
            if (mouseHand != null) mouseHand.anchoredPosition = _mouseStart;
        }

        private IEnumerator ResetPositionsSmooth()
        {
            Vector2 kbFrom = keyboardHand != null ? keyboardHand.anchoredPosition : Vector2.zero;
            Vector2 msFrom = mouseHand != null ? mouseHand.anchoredPosition : Vector2.zero;
            float t = 0f;

            while (t < resetDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / resetDuration);
                float eased = moveCurve.Evaluate(n);

                if (keyboardHand != null)
                    keyboardHand.anchoredPosition = Vector2.LerpUnclamped(kbFrom, _keyboardStart, eased);
                if (mouseHand != null)
                    mouseHand.anchoredPosition = Vector2.LerpUnclamped(msFrom, _mouseStart, eased);

                yield return null;
            }

            ResetPositions();
        }

        private IEnumerator MoveTo(RectTransform hand, Vector2 target, float intensity)
        {
            Vector2 start = hand.anchoredPosition;
            float distance = Vector2.Distance(start, target);
            float moveDur = distance / (moveSpeed * Mathf.Max(intensity, 0.1f));
            float t = 0f;

            while (t < moveDur)
            {
                t += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(t / moveDur);
                hand.anchoredPosition = Vector2.LerpUnclamped(start, target, moveCurve.Evaluate(normalized));
                yield return null;
            }

            hand.anchoredPosition = target;
        }

        private Vector2 RandomPointInBounds(RectTransform bounds)
        {
            Rect rect = bounds.rect;
            return new Vector2(
                Random.Range(rect.xMin, rect.xMax),
                Random.Range(rect.yMin, rect.yMax)
            );
        }
    }
}
