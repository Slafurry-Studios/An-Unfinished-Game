using UnityEngine;

[CreateAssetMenu(fileName = "CodeBlockLevel", menuName = "CodeBlock/Level Data")]
public class CodeBlockLevelData : ScriptableObject
{
    [Header("Ukuran grid (default 4x4)")]
    public int width = 4;
    public int height = 4;

    [System.Serializable]
    public struct FixedInput
    {
        public int x;
        public int y;
        public bool value;
    }

    [System.Serializable]
    public struct FixedOutput
    {
        public int x;
        public int y;
        public bool targetValue;
    }

    [Header("Posisi Input (fixed, gak bisa diubah player)")]
    public FixedInput[] inputs;

    [Header("Posisi Output + target yang harus dicapai")]
    public FixedOutput[] outputs;

    [Header("Gate yang tersedia di tray buat level ini")]
    public GridObjectType[] availableGates =
    {
        GridObjectType.Cable, GridObjectType.Not, GridObjectType.And, GridObjectType.Or
    };
}