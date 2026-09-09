using TMPro;
using UnityEngine;
using Slafurry.Utils.GameFeel;
using Slafurry.System.Audio;

namespace RhythmGame
{
    public enum Judgement { Perfect, Good, Ok, Miss }

    /// <summary>
    /// Taruh di GameObject "GameManager". Hubungkan ke 3 TextMeshProUGUI:
    /// scoreText, comboText, judgementText.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; internal set; }

        [Header("UI (TextMeshPro)")]
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text judgementText;

        [Header("Poin per Judgement")]
        public int perfectScore = 100;
        public int goodScore = 70;
        public int okScore = 30;

        [Header("GameFeel")]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private float shakeOnPerfect = 0.3f;
        [SerializeField] private float shakeOnGood = 0.15f;
        [SerializeField] private float shakeOnMiss = 0.5f;
        [SerializeField] private float shakeOnComboMilestone = 0.6f;
        [SerializeField] private Color flashOnPerfect = new Color(1f, 0.85f, 0.2f, 0.4f);
        [SerializeField] private Color flashOnMiss = new Color(0.8f, 0.2f, 0.2f, 0.4f);
        [SerializeField] private Color flashOnCombo = new Color(0.3f, 0.8f, 1f, 0.3f);

        private int _score;
        private int _combo;
        private int _maxCombo;
        private int _missCount;

        private void Awake() => Instance = this;

        public void RegisterHit(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: _score += perfectScore; _combo++; break;
                case Judgement.Good: _score += goodScore; _combo++; break;
                case Judgement.Ok: _score += okScore; _combo++; break;
            }
            _maxCombo = Mathf.Max(_maxCombo, _combo);
            ShowJudgement(judgement);
            UpdateUI();

            PlayHitFeel(judgement);
            CheckComboMilestone();
        }

        public void RegisterMiss()
        {
            _combo = 0;
            _missCount++;
            ShowJudgement(Judgement.Miss);
            UpdateUI();

            PlayMissFeel();
        }

        /// <summary>Reset semua statistik (dipanggil GameManager.PlayGame() supaya bisa main ulang).</summary>
        public void ResetStats()
        {
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _missCount = 0;
            if (judgementText != null) judgementText.text = "";
            UpdateUI();
        }

        private void ShowJudgement(Judgement judgement)
        {
            if (judgementText == null) return;
            judgementText.text = judgement switch
            {
                Judgement.Perfect => "PERFECT!",
                Judgement.Good => "GOOD",
                Judgement.Ok => "OK",
                _ => "MISS"
            };
        }

        private void UpdateUI()
        {
            if (scoreText != null) scoreText.text = $"Score: {_score}";
            if (comboText != null) comboText.text = _combo > 0 ? $"Combo x{_combo}" : "";
        }

        public int Score => _score;
        public int MaxCombo => _maxCombo;
        public int MissCount => _missCount;

        // ======================== GAMEFEEL ========================

        private void PlayHitFeel(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect:
                    if (cameraShake != null) cameraShake.Shake(shakeOnPerfect, 1f, 0.1f);
                    Flash(flashOnPerfect, 0.15f);
                    Audio.PlaySFX2D("Rhythm", "Perfect");
                    break;
                case Judgement.Good:
                    if (cameraShake != null) cameraShake.Shake(shakeOnGood, 1f, 0.08f);
                    Audio.PlaySFX2D("Rhythm", "Good");
                    break;
                case Judgement.Ok:
                    Audio.PlaySFX2D("Rhythm", "Ok");
                    break;
            }
        }

        private void PlayMissFeel()
        {
            if (cameraShake != null) cameraShake.Shake(shakeOnMiss, 1f, 0.15f);
            Flash(flashOnMiss, 0.2f);
            Audio.PlaySFX2D("Rhythm", "Miss");
        }

        private void CheckComboMilestone()
        {
            if (_combo <= 0) return;
            if (_combo % 25 != 0) return;

            if (cameraShake != null) cameraShake.Shake(shakeOnComboMilestone, 1.2f, 0.2f);
            Flash(flashOnCombo, 0.25f);
            Audio.PlaySFX2D("Rhythm", "ComboMilestone");
        }

        private static void Flash(Color color, float duration)
        {
            if (ScreenFlash.Instance == null) return;
            ScreenFlash.Instance.SetColor(color);
            ScreenFlash.Instance.SetDuration(duration);
            ScreenFlash.Instance.PlayEffect();
        }
    }
}