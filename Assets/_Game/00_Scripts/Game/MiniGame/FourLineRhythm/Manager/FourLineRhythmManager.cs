using UnityEngine;
using UnityEngine.Events;
using Slafurry.System.InputHub;

namespace RhythmGame
{
    /// <summary>
    /// Mengatur alur permainan: mulai game lewat PlayGame(), lalu terus mengecek
    /// kondisi MENANG / KALAH tiap frame dan memicu UnityEvent OnWin / OnLose.
    /// Karena pakai UnityEvent, kamu bisa hubungkan ke UI (misal munculkan panel
    /// "You Win" / "Game Over"), animasi, SFX, load scene, dll langsung lewat
    /// Inspector tanpa perlu tulis kode tambahan.
    ///
    /// Taruh script ini di GameObject "FourLineRhythmManager" (boleh 1 objek yang sama
    /// dengan ScoreManager), lalu isi field Referensi & hubungkan tombol
    /// "Play" ke fungsi PlayGame() lewat OnClick() di Inspector.
    /// </summary>
    public class FourLineRhythmManager : MonoBehaviour
    {
        public static FourLineRhythmManager Instance { get; private set; }

        public enum GameState { NotStarted, Playing, Won, Lost }

        [Header("Referensi")]
        public Conductor conductor;
        public NoteSpawner noteSpawner;
        public ScoreManager scoreManager;

        [Header("UI Gameplay")]
        [Tooltip("GameObject yang berisi UI gameplay (score, combo, lane, note container, dll). " +
                 "Akan otomatis DI-HIDE selama game belum di-play (NotStarted/Won/Lost), " +
                 "dan DITAMPILKAN saat PlayGame() dipanggil. Boleh 1 parent panel yang membungkus semua UI gameplay.")]
        public GameObject gameplayUI;

        [Header("Kondisi Kalah")]
        [Tooltip("Game akan KALAH kalau jumlah MISS mencapai angka ini. Set 0 kalau tidak mau ada kondisi kalah dari miss (game hanya akan berakhir MENANG saat chart selesai).")]
        public int maxMisses = 10;

        [Header("Event Menang / Kalah")]
        [Tooltip("Dipicu saat semua note di chart selesai dimainkan tanpa kena kondisi kalah.")]
        public UnityEvent OnWin;
        [Tooltip("Dipicu saat jumlah miss mencapai maxMisses.")]
        public UnityEvent OnLose;

        [Header("Event Tambahan (opsional)")]
        [Tooltip("Dipicu tepat saat PlayGame() dipanggil (lagu mulai diputar).")]
        public UnityEvent OnGameStart;

        public GameState State { get; private set; } = GameState.NotStarted;

        private void Awake()
        {
            Instance = this;
            SetGameplayUIVisible(false); // hide dulu, baru muncul saat PlayGame()
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            // Cek KALAH: jumlah miss sudah mencapai batas
            if (maxMisses > 0 && scoreManager.MissCount >= maxMisses)
            {
                Lose();
                return;
            }

            // Cek MENANG: semua note di chart sudah selesai di-spawn & tidak ada
            // note yang masih tersisa di layar (semua sudah di-hit atau di-miss)
            if (noteSpawner.IsChartFinished)
            {
                Win();
            }
        }

        /// <summary>
        /// PANGGIL FUNGSI INI UNTUK MULAI / MAIN GAME.
        /// Bisa dihubungkan langsung ke tombol "Play" (Button > OnClick di Inspector),
        /// atau dipanggil dari script lain: FourLineRhythmManager.Instance.PlayGame();
        /// Fungsi ini mereset skor & note, lalu memulai lagu.
        /// </summary>
        public void PlayGame()
        {
            scoreManager.ResetStats();
            noteSpawner.ResetSpawner();
            State = GameState.Playing;
            SetGameplayUIVisible(true);

            // Kunci control player selama minigame rhythm berlangsung
            Controls.DisableInput();

            conductor.StartSong();
            OnGameStart?.Invoke();
        }

        private void Win()
        {
            State = GameState.Won;
            SetGameplayUIVisible(false);

            Controls.EnableInput();

            OnWin?.Invoke();
        }

        private void Lose()
        {
            State = GameState.Lost;
            SetGameplayUIVisible(false);

            Controls.EnableInput();

            OnLose?.Invoke();
        }

        private void SetGameplayUIVisible(bool visible)
        {
            if (gameplayUI != null) gameplayUI.SetActive(visible);
        }

        // Jaga-jaga: kalau objek ini di-disable/destroy saat masih Playing
        // (misal scene dipindah paksa), pastikan control player nggak nyangkut kekunci.
        private void OnDisable()
        {
            if (State == GameState.Playing)
                Controls.EnableInput();
        }
    }
}