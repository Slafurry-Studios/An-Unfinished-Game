using UnityEngine;
using Cinemachine;
using Slafurry.System.Audio;

namespace Slafurry.System.Camera
{
    /// <summary>
    /// Manages camera state transitions. Use for cutscenes, boss fights,
    /// or any programmatic camera switching beyond zone triggers.
    /// </summary>
    public class CameraChanger : MonoBehaviour
    {
        [Header("Default Camera")]
        [SerializeField] private CinemachineVirtualCamera defaultCamera;

        [Header("Cutscene Cameras")]
        [SerializeField] private CinemachineVirtualCamera[] cutsceneCameras;

        [Header("Transition")]
        [SerializeField] private float transitionTime = 0.5f;

        private CinemachineVirtualCamera currentCamera;

        private void Start()
        {
            currentCamera = defaultCamera;
        }

        /// <summary>
        /// Switch to a specific cutscene camera by index.
        /// </summary>
        public void SwitchToCutscene(int index)
        {
            if (index < 0 || index >= cutsceneCameras.Length)
            {
                Debug.LogWarning($"[CameraChanger] Invalid cutscene camera index: {index}");
                return;
            }

            SwitchCamera(cutsceneCameras[index]);
        }

        /// <summary>
        /// Switch to a specific VirtualCamera.
        /// </summary>
        public void SwitchCamera(CinemachineVirtualCamera target)
        {
            if (target == null)
            {
                Debug.LogWarning("[CameraChanger] Target camera is null");
                return;
            }

            if (currentCamera != null)
                currentCamera.Priority = 0;

            target.Priority = 10;
            currentCamera = target;
        }

        /// <summary>
        /// Return to the default camera.
        /// </summary>
        public void ReturnToDefault()
        {
            if (currentCamera != null)
                currentCamera.Priority = 0;

            if (defaultCamera != null)
                defaultCamera.Priority = 10;

            currentCamera = defaultCamera;
        }

        /// <summary>
        /// Temporarily boost a camera's priority (useful for one-shot events).
        /// </summary>
        public void BoostPriority(CinemachineVirtualCamera cam, int boost = 5)
        {
            if (cam == null) return;
            cam.Priority += boost;
        }

        /// <summary>
        /// Reset a camera's priority back to default.
        /// </summary>
        public void ResetPriority(CinemachineVirtualCamera cam)
        {
            if (cam == null) return;
            cam.Priority = 0;
        }
    }
}
