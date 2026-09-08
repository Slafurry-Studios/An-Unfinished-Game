using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Slafurry.System.Camera
{
    /// <summary>
    /// Blends a URP Volume's weight over time. Wire via DetectArea UnityEvents.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class PostProcessBlender : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Volume volume;

        [Header("Blend Settings")]
        [SerializeField] private float blendDuration = 0.5f;
        [SerializeField] private bool useUnscaledTime = false;

        private float startWeight;
        private float targetWeight;
        private float currentWeight;
        private bool isBlending;

        private void Awake()
        {
            if (volume == null)
                volume = GetComponent<Volume>();

            currentWeight = volume.weight;
        }

        private void Update()
        {
            if (!isBlending) return;

            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, (1f / blendDuration) * delta);
            volume.weight = currentWeight;

            if (Mathf.Approximately(currentWeight, targetWeight))
                isBlending = false;
        }

        /// <summary>
        /// Blend from current weight to target (0-1).
        /// </summary>
        public void BlendTo(float target)
        {
            targetWeight = Mathf.Clamp01(target);
            startWeight = currentWeight;
            isBlending = true;
        }

        /// <summary>
        /// Blend to full effect (1).
        /// </summary>
        public void BlendIn()
        {
            BlendTo(1f);
        }

        /// <summary>
        /// Blend to off (0).
        /// </summary>
        public void BlendOut()
        {
            BlendTo(0f);
        }

        public void SetBlendDuration(float duration)
        {
            blendDuration = Mathf.Max(0.01f, duration);
        }

        public void InstantApply()
        {
            currentWeight = 1f;
            volume.weight = 1f;
            isBlending = false;
        }

        public void InstantReset()
        {
            currentWeight = 0f;
            volume.weight = 0f;
            isBlending = false;
        }
    }
}
