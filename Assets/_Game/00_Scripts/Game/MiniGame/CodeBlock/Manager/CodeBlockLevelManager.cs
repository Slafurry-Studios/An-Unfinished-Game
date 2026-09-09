using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Slafurry.System.InputHub;
using Slafurry.System.Audio;
using Slafurry.Utils.GameFeel;
using Slafurry.Interaction;

public class CodeBlockGameManager : MonoBehaviour
{
    [Header("References")]
    public CodeBlockLevelLoader loader;
    public GridBoardView board;
    public InventoryTrayView trayView;

    [Header("UI Root (di-hide di awal, muncul pas StartGame() dipanggil)")]
    public GameObject gameUIRoot;

    [Header("Level yang dimainkan berurutan")]
    public List<CodeBlockLevelData> levels;

    [Header("Jeda sebelum pindah ke level berikutnya (detik)")]
    public float delayBeforeNextLevel = 1f;

    [Header("Events")]
    public UnityEvent OnGameWin;   
    public UnityEvent OnGameLose; 

    [Header("GameFeel")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeOnSolve = 0.4f;
    [SerializeField] private float shakeOnWin = 0.7f;
    [SerializeField] private float shakeOnLose = 0.9f;
    [SerializeField] private Color flashOnSolve = new Color(0.3f, 0.8f, 0.3f, 0.4f);
    [SerializeField] private Color flashOnWin = new Color(1f, 0.85f, 0.2f, 0.5f);
    [SerializeField] private Color flashOnLose = new Color(0.8f, 0.2f, 0.2f, 0.5f);

    [Header("Trigger (opsional)")]
    [Tooltip("Reference ke InteractionTrigger yang meluncurkan minigame ini. " +
             "Kalau diisi, counter akan dikurangi -1 saat kalah atau quit.")]
    [SerializeField] private InteractionTrigger interactionTrigger;

    public System.Action OnAllLevelsComplete;

    private int currentIndex = 0;
    private bool isTransitioning = false;
    private bool gameStarted = false;
    private bool gameEnded = false;

    void Awake()
    {
        if (gameUIRoot != null)
            gameUIRoot.SetActive(false);
    }

    void Start()
    {
        board.OnLevelSolved += HandleLevelSolved;
        board.OnPlacementChanged += HandlePlacementChanged;
    }

    void OnDestroy()
    {
        if (board != null)
        {
            board.OnLevelSolved -= HandleLevelSolved;
            board.OnPlacementChanged -= HandlePlacementChanged;
        }
    }

    void Update()
    {
        if (!gameStarted || gameEnded || isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.R))
            LoadLevelAt(currentIndex);
    }

    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        gameEnded = false;

        if (gameUIRoot != null)
            gameUIRoot.SetActive(true);

        // Kunci control player selama minigame code-block berlangsung
        Controls.DisableInput();

        if (levels != null && levels.Count > 0)
            LoadLevelAt(0);
    }

    void LoadLevelAt(int index)
    {
        currentIndex = index;
        isTransitioning = false;

        CodeBlockLevelData level = levels[index];
        loader.LoadLevel(level);
        board.BuildBoard();
        trayView.BuildTray(level.availableGates);
    }

    void HandleLevelSolved()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayFeel(cameraShake, shakeOnSolve);
        FlashScreen(flashOnSolve, 0.3f);
        Audio.PlaySFX2D("Minigame", "Correct");

        Invoke(nameof(GoToNextLevel), delayBeforeNextLevel);
    }

    void HandlePlacementChanged()
    {
        if (isTransitioning) return;
        if (loader.IsLevelComplete()) return;

        if (IsGridFull())
            LoseGame();
    }

    bool IsGridFull()
    {
        CodeBlockLevelData level = levels[currentIndex];
        for (int x = 0; x < level.width; x++)
        {
            for (int y = 0; y < level.height; y++)
            {
                GridCell cell = board.Circuit.GetCell(x, y);
                if (cell != null && cell.type == GridObjectType.Empty)
                    return false; // masih ada ruang kosong, belum buntu
            }
        }
        return true;
    }

    void GoToNextLevel()
    {
        int next = currentIndex + 1;
        if (next < levels.Count)
        {
            // Masih lanjut ke level berikutnya, game belum berakhir,
            // jadi control TETAP terkunci di sini.
            LoadLevelAt(next);
        }
        else
        {
            WinGame();
        }
    }

    void WinGame()
    {
        if (gameEnded) return;
        gameEnded = true;

        Controls.EnableInput();

        PlayFeel(cameraShake, shakeOnWin);
        FlashScreen(flashOnWin, 0.4f);
        Audio.PlaySFX2D("Minigame", "Win");

        OnAllLevelsComplete?.Invoke();
        OnGameWin?.Invoke();
    }

    void LoseGame()
    {
        if (gameEnded) return;
        gameEnded = true;

        Controls.EnableInput();
        interactionTrigger?.DecrementCount();

        PlayFeel(cameraShake, shakeOnLose);
        FlashScreen(flashOnLose, 0.4f);
        Audio.PlaySFX2D("Minigame", "Lose");

        OnGameLose?.Invoke();
    }

    /// <summary>
    /// Panggil dari tombol Quit buat keluar dari minigame.
    /// Counter interaksi dikurangi -1 supaya bisa dicoba lagi.
    /// </summary>
    public void QuitGame()
    {
        if (!gameStarted || gameEnded) return;
        gameEnded = true;

        Controls.EnableInput();
        interactionTrigger?.DecrementCount();

        OnGameLose?.Invoke();
    }

    // Jaga-jaga: kalau objek ini di-disable/destroy saat game masih berjalan
    // (belum win/lose), pastikan control player nggak nyangkut kekunci.
    void OnDisable()
    {
        if (gameStarted && !gameEnded)
            Controls.EnableInput();
    }

    // ======================== GAMEFEEL HELPERS ========================

    private static void PlayFeel(CameraShake shake, float amplitude)
    {
        if (shake != null) shake.Shake(amplitude, 1f, 0.15f);
    }

    private static void FlashScreen(Color color, float duration)
    {
        if (ScreenFlash.Instance == null) return;
        ScreenFlash.Instance.SetColor(color);
        ScreenFlash.Instance.SetDuration(duration);
        ScreenFlash.Instance.PlayEffect();
    }
}