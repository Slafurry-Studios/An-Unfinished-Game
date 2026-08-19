using TMPro;
using UnityEngine;

namespace RhythmGame
{
    public enum Judgement { Perfect, Good, Ok, Miss }

    /// <summary>
    /// Taruh di GameObject "GameManager". Hubungkan ke 3 TextMeshProUGUI:
    /// scoreText, comboText, judgementText.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("UI (TextMeshPro)")]
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text judgementText;

        [Header("Poin per Judgement")]
        public int perfectScore = 100;
        public int goodScore = 70;
        public int okScore = 30;

        private int _score;
        private int _combo;
        private int _maxCombo;

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
        }

        public void RegisterMiss()
        {
            _combo = 0;
            ShowJudgement(Judgement.Miss);
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
    }
}