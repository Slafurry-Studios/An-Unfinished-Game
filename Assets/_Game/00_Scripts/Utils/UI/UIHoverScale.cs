using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Slafurry.Utils.UI
{
    /// <summary>
    /// Simple UI hover scale effect.
    /// Scales the UI up when hovered and returns to its original scale
    /// when the pointer exits.
    /// </summary>
    public class UIHoverScale : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Hover")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float speed = 10f;

        [Header("Options")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onHoverEnter;
        [SerializeField] private UnityEvent onHoverExit;

        private Vector3 _originalScale;
        private Vector3 _targetScale;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            _originalScale = target.localScale;
            _targetScale = _originalScale;
        }

        private void Update()
        {
            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            target.localScale = Vector3.Lerp(
                target.localScale,
                _targetScale,
                speed * deltaTime
            );
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = _originalScale * hoverScale;

            onHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = _originalScale;

            onHoverExit?.Invoke();
        }

        public void SetHoverScale(float scale)
        {
            _targetScale = _originalScale * scale;
        }

        public void ResetScale()
        {
            _targetScale = _originalScale;
        }
    }
}