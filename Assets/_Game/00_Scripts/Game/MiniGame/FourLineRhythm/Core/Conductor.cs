using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Conductor = "jantung" ritme. Mengatur waktu lagu (song time) yang jadi
    /// acuan utama untuk spawn note, gerakan note, dan penilaian hit.
    /// Taruh script ini di satu GameObject kosong bernama "Conductor".
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance { get; private set; }

        [Header("Audio")]
        public AudioSource musicSource;
        [Tooltip("Delay sebelum lagu mulai diputar (detik), memberi waktu countdown")]
        public float startDelay = 2f;

        [Header("Speed / Scroll")]
        [Tooltip("Kecepatan scroll note dalam PIXEL per detik (satuan Canvas UI). Diubah lewat SpeedController / slider UI.")]
        [Range(100f, 1200f)]
        public float scrollSpeed = 400f;

        private double _dspSongStartTime;
        private bool _hasStarted;

        public bool HasStarted => _hasStarted;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _dspSongStartTime = AudioSettings.dspTime + startDelay;
            musicSource.PlayScheduled(_dspSongStartTime);
            _hasStarted = true;
        }

        /// <summary>
        /// Waktu lagu saat ini dalam detik. Nilai negatif = masih countdown/delay.
        /// </summary>
        public float GetSongTime()
        {
            return (float)(AudioSettings.dspTime - _dspSongStartTime);
        }

        public void SetScrollSpeed(float newSpeed)
        {
            scrollSpeed = Mathf.Clamp(newSpeed, 100f, 1200f);
        }
    }
}