using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Slafurry.System.InputHub;

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

        OnAllLevelsComplete?.Invoke();
        OnGameWin?.Invoke();
    }

    void LoseGame()
    {
        if (gameEnded) return;
        gameEnded = true;

        Controls.EnableInput();

        OnGameLose?.Invoke();
    }

    // Jaga-jaga: kalau objek ini di-disable/destroy saat game masih berjalan
    // (belum win/lose), pastikan control player nggak nyangkut kekunci.
    void OnDisable()
    {
        if (gameStarted && !gameEnded)
            Controls.EnableInput();
    }
}