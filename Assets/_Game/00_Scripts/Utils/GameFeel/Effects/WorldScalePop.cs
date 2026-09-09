using System.Collections;
using UnityEngine;

namespace Slafurry.Utils.GameFeel
{
    /// <summary>
    /// One-shot "pop" scale effect for world-space Transforms.
    /// Follows Time.timeScale — freezes when game is paused.
    /// For UI RectTransform, use UIScalePunch instead.
    /// </summary>
    public class WorldScalePop : MonoBehaviour, IGameFeelEffect
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Pop")]
        [SerializeField] private float popScale = 1.3f;
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Trigger")]
        [SerializeField] private bool playOnEnable = false;

        private Vector3 _originalScale;
        private Coroutine _routine;

        private void Awake()
        {
            if (target == null) target = transform;
            _originalScale = target.localScale;
        }

        private void OnEnable()
        {
            _routine = null;
            target.localScale = _originalScale;

            if (playOnEnable)
                PlayEffect();
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        public void PlayEffect()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PopRoutine());
        }

        public void StopEffect()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (target != null)
                target.localScale = _originalScale;
        }

        private IEnumerator PopRoutine()
        {
            Vector3 peak = _originalScale * popScale;
            float half = duration * 0.5f;

            yield return Scale(_originalScale, peak, half);
            yield return Scale(peak, _originalScale, half);

            target.localScale = _originalScale;
            _routine = null;
        }

        private IEnumerator Scale(Vector3 from, Vector3 to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float eased = curve.Evaluate(Mathf.Clamp01(t / dur));
                target.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }
        }
    }
}