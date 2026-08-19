using System.Collections.Generic;
using UnityEngine;

public class CodeBlockCircuit : MonoBehaviour
{
    [Header("Grid Size (default 4x4, bisa diperbesar per level)")]
    public int width = 4;
    public int height = 4;

    private GridCell[,] grid;

    // Hasil simulasi terakhir: posisi -> nilai boolean
    private Dictionary<Vector2Int, bool> resolvedValues = new Dictionary<Vector2Int, bool>();

    void Awake()
    {
        InitGrid(width, height);
    }

    // ---------------- Setup Grid ----------------

    public void InitGrid(int w, int h)
    {
        width = w;
        height = h;
        grid = new GridCell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new GridCell { x = x, y = y };
    }

    bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    public GridCell GetCell(int x, int y) => InBounds(x, y) ? grid[x, y] : null;

    // ---------------- Placement API (dipanggil dari drag-drop UI) ----------------

    public bool PlaceObject(int x, int y, GridObjectType type)
    {
        if (!InBounds(x, y)) return false;

        GridCell cell = grid[x, y];

        // Input/Output posisinya fixed dari level, gak boleh ditimpa player
        if (cell.type == GridObjectType.Input || cell.type == GridObjectType.Output)
            return false;

        cell.type = type;
        Simulate();
        return true;
    }

    public void RemoveObject(int x, int y)
    {
        if (!InBounds(x, y)) return;
        GridCell cell = grid[x, y];
        if (cell.type == GridObjectType.Input || cell.type == GridObjectType.Output)
            return;

        cell.type = GridObjectType.Empty;
        Simulate();
    }

    // Dipanggil sekali saat load level, buat naruh Input/Output fixed
    public void SetFixedInput(int x, int y, bool value)
    {
        if (!InBounds(x, y)) return;
        grid[x, y].type = GridObjectType.Input;
        grid[x, y].fixedInputValue = value;
    }

    public void SetOutput(int x, int y, int targetIndex)
    {
        if (!InBounds(x, y)) return;
        grid[x, y].type = GridObjectType.Output;
        grid[x, y].outputTargetIndex = targetIndex;
    }

    // ---------------- Simulasi (flood-fill bertahap, auto-connect) ----------------

    public void Simulate()
    {
        resolvedValues.Clear();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Step 1: semua Input langsung resolved (nilainya udah fixed)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = grid[x, y];
                if (cell.type == GridObjectType.Input)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    resolvedValues[pos] = cell.fixedInputValue;
                    queue.Enqueue(pos);
                }
            }
        }

        // Step 2: propagasi. Tiap kali ada cell baru resolved,
        // cek tetangganya: kalau tetangga itu udah cukup "input" (sesuai RequiredInputs),
        // hitung nilainya dan lanjutkan penyebarannya.
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int neighborPos in GetNeighbors(current))
            {
                if (resolvedValues.ContainsKey(neighborPos)) continue;

                GridCell neighbor = grid[neighborPos.x, neighborPos.y];
                if (!neighbor.IsFilled) continue;

                int required = neighbor.RequiredInputs();
                if (required <= 0) continue; // Empty atau Input (Input udah di-handle di Step 1)

                List<bool> knownInputs = GetResolvedNeighborValues(neighborPos);
                if (knownInputs.Count >= required)
                {
                    bool value = ComputeValue(neighbor.type, knownInputs);
                    resolvedValues[neighborPos] = value;
                    queue.Enqueue(neighborPos);
                }
            }
        }
    }

    List<bool> GetResolvedNeighborValues(Vector2Int pos)
    {
        List<bool> result = new List<bool>();
        foreach (Vector2Int n in GetNeighbors(pos))
        {
            if (resolvedValues.TryGetValue(n, out bool val))
                result.Add(val);
        }
        return result;
    }

    IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        Vector2Int[] dirs =
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 0), new Vector2Int(-1, 0)
        };
        foreach (var d in dirs)
        {
            Vector2Int n = pos + d;
            if (InBounds(n.x, n.y)) yield return n;
        }
    }

    bool ComputeValue(GridObjectType type, List<bool> inputs)
    {
        switch (type)
        {
            case GridObjectType.Cable:
                return inputs[0];
            case GridObjectType.Not:
                return !inputs[0];
            case GridObjectType.And:
                return inputs[0] && inputs[1];
            case GridObjectType.Or:
                return inputs[0] || inputs[1];
            case GridObjectType.Output:
                return inputs[0];
            default:
                return false;
        }
    }

    // ---------------- Query hasil (buat visual & win-check) ----------------

    // true kalau cell itu sudah kebagian nilai dari simulasi terakhir
    public bool TryGetValue(int x, int y, out bool value)
    {
        return resolvedValues.TryGetValue(new Vector2Int(x, y), out value);
    }

    public bool CheckWin(bool[] targetOutputs)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = grid[x, y];
                if (cell.type != GridObjectType.Output) continue;

                Vector2Int pos = new Vector2Int(x, y);
                if (!resolvedValues.TryGetValue(pos, out bool value))
                    return false; // ada output yang belum kebagian nilai (rangkaian belum lengkap)

                int idx = cell.outputTargetIndex;
                if (idx < 0 || idx >= targetOutputs.Length) return false;
                if (value != targetOutputs[idx]) return false;
            }
        }
        return true;
    }
}