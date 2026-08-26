using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SlidingPuzzleManager : MonoBehaviour
{
    [Header("Setup")]
    public Sprite[] pieceSprites;
    public Image[] slotImages;

    [Header("Warna tiap piece (biar gak numpang default putih dari vertex color)")]
    public Color[] pieceColors = new Color[8]
    {
        Color.white, Color.white, Color.white, Color.white,
        Color.white, Color.white, Color.white, Color.white
    };

    [Header("UI Root (di-hide di awal, muncul pas StartGame() dipanggil)")]
    public GameObject gameUIRoot;

    [Header("Shuffle")]
    public int shuffleMoves = 100;

    [Header("Events")]
    public UnityEvent OnPuzzleWin;
    public UnityEvent OnPuzzleLose;

    int[] board = new int[9];  
    int emptyIndex;
    bool gameStarted = false;
    bool gameEnded = false;

    void Awake()
    {
        if (gameUIRoot != null)
            gameUIRoot.SetActive(false);
    }

    void Update()
    {
        if (!gameStarted || gameEnded) return;

        // Keyboard dpad
        if (Input.GetKeyDown(KeyCode.UpArrow))    TryMove(1, 0);   // ambil tile dari bawah
        if (Input.GetKeyDown(KeyCode.DownArrow))  TryMove(-1, 0);  // ambil tile dari atas
        if (Input.GetKeyDown(KeyCode.LeftArrow))  TryMove(0, 1);   // ambil tile dari kanan
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(0, -1);  // ambil tile dari kiri
    }


    public void StartGame()
    {
        if (gameStarted) return;
        gameStarted = true;
        gameEnded = false;

        if (gameUIRoot != null)
            gameUIRoot.SetActive(true);

        SetupSolvedBoard();
        Shuffle(shuffleMoves);
        Redraw();
    }

    public void LoseGame()
    {
        if (gameEnded) return;
        gameEnded = true;

        OnPuzzleLose?.Invoke();
    }

    void SetupSolvedBoard()
    {
        for (int i = 0; i < 8; i++) board[i] = i + 1;
        board[8] = 0;
        emptyIndex = 8;
    }

    void Shuffle(int moves)
    {
        for (int m = 0; m < moves; m++)
        {
            var neighbors = GetNeighbors(emptyIndex);
            int pick = neighbors[Random.Range(0, neighbors.Count)];
            Swap(pick, emptyIndex);
        }
    }

    List<int> GetNeighbors(int index)
    {
        var result = new List<int>();
        int row = index / 3, col = index % 3;
        if (row > 0) result.Add(index - 3);
        if (row < 2) result.Add(index + 3);
        if (col > 0) result.Add(index - 1);
        if (col < 2) result.Add(index + 1);
        return result;
    }

    public void OnDpadUp()    => TryMove(1, 0);
    public void OnDpadDown()  => TryMove(-1, 0);
    public void OnDpadLeft()  => TryMove(0, 1);
    public void OnDpadRight() => TryMove(0, -1);

    void TryMove(int rowOffset, int colOffset)
    {
        if (!gameStarted || gameEnded) return;

        int emptyRow = emptyIndex / 3;
        int emptyCol = emptyIndex % 3;

        int targetRow = emptyRow + rowOffset;
        int targetCol = emptyCol + colOffset;

        if (targetRow < 0 || targetRow > 2 || targetCol < 0 || targetCol > 2)
            return; // di luar grid, abaikan

        int targetIndex = targetRow * 3 + targetCol;

        Swap(targetIndex, emptyIndex);
        Redraw();
        if (IsSolved()) OnPuzzleSolved();
    }

    // ---- Core ----

    void Swap(int a, int b)
    {
        (board[a], board[b]) = (board[b], board[a]);
        if (a == emptyIndex) emptyIndex = b;
        else if (b == emptyIndex) emptyIndex = a;
    }

    void Redraw()
    {
        for (int i = 0; i < 9; i++)
        {
            bool isEmpty = board[i] == 0;
            slotImages[i].enabled = !isEmpty;
            if (!isEmpty)
            {
                int pieceId = board[i];
                slotImages[i].sprite = pieceSprites[pieceId - 1];

                // Kasih warna sendiri per piece, biar gak numpang default putih dari vertex color
                if (pieceColors != null && pieceId - 1 < pieceColors.Length)
                    slotImages[i].color = pieceColors[pieceId - 1];
            }
        }
    }

    bool IsSolved()
    {
        for (int i = 0; i < 8; i++)
            if (board[i] != i + 1) return false;
        return board[8] == 0;
    }

    void OnPuzzleSolved()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("Puzzle selesai!");
        OnPuzzleWin?.Invoke();
    }
}