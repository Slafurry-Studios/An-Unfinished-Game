using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Slafurry.Utils.UI
{
    /// <summary>
    /// Simple floating animation for UI elements.
    /// Moves the UI smoothly up and down around its original position.
    /// </summary>
    public class UIFloat : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Float")]
        [SerializeField] private float height = 10f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool loop = true;

        [Header("Options")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onFloatStart;
        [SerializeField] private UnityEvent onFloatStop;

        private Coroutine _routine;
        private Vector2 _originalPosition;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            if (target != null)
                _originalPosition = target.anchoredPosition;
        }

        public void Float()
        {
            if (target == null)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _originalPosition = target.anchoredPosition;

            _routine = StartCoroutine(FloatRoutine());

            onFloatStart?.Invoke();
        }

        public void StopFloat()
        {
            if (_routine == null)
                return;

            StopCoroutine(_routine);
            _routine = null;

            target.anchoredPosition = _originalPosition;

            onFloatStop?.Invoke();
        }

        public void SetPosition(Vector2 position)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _originalPosition = position;
            target.anchoredPosition = position;
        }

        private IEnumerator FloatRoutine()
        {
            float time = 0f;

            do
            {
                time = 0f;

                while (time < Mathf.PI * 2f)
                {
                    float deltaTime = useUnscaledTime
                        ? Time.unscaledDeltaTime
                        : Time.deltaTime;

                    time += deltaTime * speed;

                    float offset = Mathf.Sin(time) * height;

                    target.anchoredPosition =
                        _originalPosition +
                        Vector2.up * offset;

                    yield return null;
                }

            } while (loop);

            target.anchoredPosition = _originalPosition;

            _routine = null;

            onFloatStop?.Invoke();
        }
    }
}