using System.Collections.Generic;
using UnityEngine;

public class CodeBlockGameManager : MonoBehaviour
{
    [Header("References")]
    public CodeBlockLevelLoader loader;
    public GridBoardView board;
    public InventoryTrayView trayView;

    [Header("Level yang dimainkan berurutan")]
    public List<CodeBlockLevelData> levels;

    [Header("Jeda sebelum pindah ke level berikutnya (detik)")]
    public float delayBeforeNextLevel = 1f;

    public System.Action OnAllLevelsComplete;

    private int currentIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        board.OnLevelSolved += HandleLevelSolved;

        if (levels != null && levels.Count > 0)
            LoadLevelAt(0);
    }

    void OnDestroy()
    {
        if (board != null)
            board.OnLevelSolved -= HandleLevelSolved;
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
        // RefreshVisuals bisa manggil OnLevelSolved berkali-kali (tiap ada perubahan grid),
        // flag ini biar transisi cuma ke-trigger sekali per level
        if (isTransitioning) return;
        isTransitioning = true;

        Invoke(nameof(GoToNextLevel), delayBeforeNextLevel);
    }

    void GoToNextLevel()
    {
        int next = currentIndex + 1;
        if (next < levels.Count)
            LoadLevelAt(next);
        else
            OnAllLevelsComplete?.Invoke();
    }
}