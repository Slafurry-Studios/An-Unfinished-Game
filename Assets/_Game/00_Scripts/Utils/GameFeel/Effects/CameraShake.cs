using UnityEngine;
using Cinemachine;

namespace Slafurry.Utils.GameFeel
{
    public class CameraShake : MonoBehaviour, IGameFeelEffect
    {
        [Header("Impulse Source")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Default Shake Settings")]
        [SerializeField] private float defaultAmplitude = 1f;
        [SerializeField] private float defaultFrequency = 1f;
        [SerializeField] private float defaultDuration = 0.2f;

        private void Awake()
        {
            CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        }

        private void Start()
        {
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
                if (impulseSource == null)
                {
                    Debug.LogError("[CameraShake] No CinemachineImpulseSource found on this GameObject!");
                }
            }
        }

        public void Shake(float amplitude, float frequency, float duration)
        {
            if (impulseSource == null)
            {
                Debug.LogWarning("[CameraShake] Impulse Source not assigned!");
                return;
            }

            impulseSource.m_ImpulseDefinition.m_AmplitudeGain = amplitude;
            impulseSource.m_ImpulseDefinition.m_FrequencyGain = frequency;
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;

            impulseSource.GenerateImpulse();
        }

        public void PlayEffect()
        {
            Shake(defaultAmplitude, defaultFrequency, defaultDuration);
        }

        public void StopEffect()
        {
            if (impulseSource == null) return;

            impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0f;
        }
    }
}