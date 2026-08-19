[System.Serializable]
public class GridCell
{
    public GridObjectType type = GridObjectType.Empty;
    public int x;
    public int y;

    // Dipakai kalau type == Input (nilai tetap dari level, gak bisa diubah player)
    public bool fixedInputValue;

    // Dipakai kalau type == Output (index ke array target output di level)
    public int outputTargetIndex = -1;

    public bool IsFilled => type != GridObjectType.Empty;

    // Berapa tetangga "resolved" (sudah punya nilai) yang dibutuhkan
    // sebelum cell ini bisa menghitung nilainya sendiri.
    // Input gak butuh apa-apa (nilainya udah fixed dari awal).
    public int RequiredInputs()
    {
        switch (type)
        {
            case GridObjectType.Cable:
            case GridObjectType.Not:
            case GridObjectType.Output:
                return 1;
            case GridObjectType.And:
            case GridObjectType.Or:
                return 2;
            default:
                return 0;
        }
    }
}