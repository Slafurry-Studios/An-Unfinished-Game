using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Utils.UI
{
    /// <summary>
    /// Shakes RectTransform(s) with intensity that ramps up over time.
    /// </summary>
    public class UIShake : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private RectTransform[] targets;

        [Header("Shake")]
        [SerializeField] private float duration = 1f;
        [SerializeField] private float maxIntensity = 10f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private bool dampening = true;

        [Header("Axis")]
        [SerializeField] private bool shakeX = true;
        [SerializeField] private bool shakeY = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onFinished;

        private Vector2[] _startPositions;
        private Vector2[] _offsets;
        private float _elapsed;
        private bool _isShaking;

        private void LateUpdate()
        {
            if (!_isShaking)
                return;

            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed >= duration)
            {
                Stop();
                onFinished?.Invoke();
                return;
            }

            float t = Mathf.Clamp01(_elapsed / duration);
            float curveValue = intensityCurve.Evaluate(t);
            float currentIntensity = maxIntensity * curveValue;

            if (dampening)
            {
                float remaining = 1f - t;
                currentIntensity *= remaining;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                Vector2 rand = Random.insideUnitCircle * currentIntensity;
                Vector2 newOffset = new Vector2(
                    shakeX ? rand.x : 0f,
                    shakeY ? rand.y : 0f
                );

                targets[i].anchoredPosition = _startPositions[i] + newOffset;
                _offsets[i] = newOffset;
            }
        }

        public void Play()
        {
            Stop();
            CachePositions();
            _elapsed = 0f;
            _isShaking = true;
        }

        public void Stop()
        {
            _isShaking = false;

            if (_startPositions == null || targets == null) return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].anchoredPosition = _startPositions[i];
            }
        }

        private void CachePositions()
        {
            if (targets == null || targets.Length == 0) return;

            _startPositions = new Vector2[targets.Length];
            _offsets = new Vector2[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                _startPositions[i] = targets[i].anchoredPosition;
            }
        }
    }
}
