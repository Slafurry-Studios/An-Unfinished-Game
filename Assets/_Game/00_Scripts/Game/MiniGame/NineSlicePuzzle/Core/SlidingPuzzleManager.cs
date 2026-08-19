using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlidingPuzzleManager : MonoBehaviour
{
    [Header("Setup")]
    public Sprite[] pieceSprites; // 8 sprite (piece id 1-8), urut kiri-atas ke kanan-bawah
    public Image[] slotImages;    // 9 Image sesuai urutan grid (row-major, kiri-atas = index 0)

    [Header("Shuffle")]
    public int shuffleMoves = 100;

    int[] board = new int[9];     // 0 = kosong, 1-8 = piece id
    int emptyIndex;

    void Start()
    {
        SetupSolvedBoard();
        Shuffle(shuffleMoves);
        Redraw();
    }

    void Update()
    {
        // Keyboard dpad
        if (Input.GetKeyDown(KeyCode.UpArrow))    TryMove(1, 0);   // ambil tile dari bawah
        if (Input.GetKeyDown(KeyCode.DownArrow))  TryMove(-1, 0);  // ambil tile dari atas
        if (Input.GetKeyDown(KeyCode.LeftArrow))  TryMove(0, 1);   // ambil tile dari kanan
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(0, -1);  // ambil tile dari kiri
    }

    // ---- Board setup ----

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

    // ---- Input: dpad (keyboard atau UI button, panggil function ini) ----

    public void OnDpadUp()    => TryMove(1, 0);
    public void OnDpadDown()  => TryMove(-1, 0);
    public void OnDpadLeft()  => TryMove(0, 1);
    public void OnDpadRight() => TryMove(0, -1);

    void TryMove(int rowOffset, int colOffset)
    {
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
            if (!isEmpty) slotImages[i].sprite = pieceSprites[board[i] - 1];
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
        Debug.Log("Puzzle selesai!");
        // trigger event lain di sini, misal buka pintu / kasih reward
    }
}