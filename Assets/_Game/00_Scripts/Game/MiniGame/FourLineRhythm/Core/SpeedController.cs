using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Hubungkan ke UI Slider untuk mengatur scrollSpeed di Conductor secara live,
    /// persis seperti pengaturan "Speed" (misal 5.0, 10.0, dst) di rhythm game
    /// pada umumnya. Karena Note.cs menghitung posisi dari selisih waktu,
    /// perubahan speed langsung terasa tanpa mengganggu timing/akurasi hit.
    /// </summary>
    public class SpeedController : MonoBehaviour
    {
        public Conductor conductor;
        public Slider speedSlider;       // Slider tetap dari UnityEngine.UI (bukan TMP)
        public TMP_Text speedLabel;      // Label pakai TextMeshPro

        private void Start()
        {
            if (speedSlider != null)
            {
                speedSlider.minValue = 100f;
                speedSlider.maxValue = 1200f;
                speedSlider.value = conductor.scrollSpeed;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
                OnSpeedChanged(speedSlider.value);
            }
        }

        private void OnSpeedChanged(float value)
        {
            conductor.SetScrollSpeed(value);
            if (speedLabel != null) speedLabel.text = $"Speed: {value:0.0}";
        }
    }
}