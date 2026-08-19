using UnityEngine;

public class GridBoardView : MonoBehaviour
{
    [Header("References")]
    public CodeBlockCircuit Circuit;
    public CodeBlockLevelLoader Loader;
    public GridCellView cellPrefab;
    public RectTransform gridParent; // pasang GridLayoutGroup di object ini

    [Header("Visual: sprite per gate type")]
    public GateIconSet iconSet;

    [Header("Visual: warna sinyal")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.1f);
    public Color unresolvedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color signalOnColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color signalOffColor = new Color(0.2f, 0.3f, 0.5f, 1f);
    public Color outputCorrectColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    public Color outputWrongColor = new Color(0.8f, 0.3f, 0.3f, 1f);

    public System.Action OnPlacementChanged;
    public System.Action OnLevelSolved;

    private GridCellView[,] cellViews;
    private bool alreadySolved; // guard biar OnLevelSolved cuma fire sekali

    // Panggil ini setelah Loader.LoadLevel(...)
    public void BuildBoard()
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        ApplyGridSizing();

        int w = Circuit.width;
        int h = Circuit.height;
        cellViews = new GridCellView[w, h];

        // y tinggi digambar di atas, biar orientasinya natural di layar
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                GridCellView cellView = Instantiate(cellPrefab, gridParent);
                cellView.x = x;
                cellView.y = y;
                cellView.board = this;
                cellViews[x, y] = cellView;
            }
        }

        alreadySolved = false;
        RefreshVisuals();
    }

    // Hitung ulang Cell Size di GridLayoutGroup supaya grid selalu pas ngisi
    // seluruh area gridParent, berapapun width/height level-nya
    void ApplyGridSizing()
    {
        UnityEngine.UI.GridLayoutGroup layout = gridParent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (layout == null) return;

        int w = Circuit.width;
        int h = Circuit.height;

        float availableWidth = gridParent.rect.width
            - layout.padding.left - layout.padding.right
            - layout.spacing.x * (w - 1);

        float availableHeight = gridParent.rect.height
            - layout.padding.top - layout.padding.bottom
            - layout.spacing.y * (h - 1);

        float cellWidth = availableWidth / w;
        float cellHeight = availableHeight / h;

        // Pakai yang lebih kecil biar cell tetap persegi (gak gepeng), sisanya
        // otomatis jadi margin simetris kiri-kanan atau atas-bawah
        float cellSize = Mathf.Min(cellWidth, cellHeight);
        layout.cellSize = new Vector2(cellSize, cellSize);

        layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = w;
    }

    public void RefreshVisuals()
    {
        bool solved = Loader != null && Loader.IsLevelComplete();

        for (int x = 0; x < Circuit.width; x++)
        {
            for (int y = 0; y < Circuit.height; y++)
            {
                GridCell cell = Circuit.GetCell(x, y);
                GridCellView view = cellViews[x, y];
                if (cell == null || view == null) continue;

                if (view.icon != null)
                {
                    Sprite sprite = iconSet != null ? iconSet.GetSprite(cell.type) : null;
                    view.icon.sprite = sprite;
                    // Image tanpa sprite tetap kegambar kotak putih solid kalau enabled,
                    // jadi cuma nyalain icon kalau memang ada sprite-nya (misal gate type belum
                    // punya sprite di GateIconSet, biarin transparan daripada nutupin cell)
                    view.icon.enabled = sprite != null;
                }

                if (cell.type == GridObjectType.Empty)
                {
                    view.background.color = emptyColor;
                    continue;
                }

                bool hasValue = Circuit.TryGetValue(x, y, out bool value);

                if (cell.type == GridObjectType.Output)
                {
                    view.background.color = !hasValue ? unresolvedColor
                        : (solved ? outputCorrectColor : outputWrongColor);
                }
                else
                {
                    view.background.color = !hasValue ? unresolvedColor
                        : (value ? signalOnColor : signalOffColor);
                }
            }
        }

        if (solved && !alreadySolved)
        {
            alreadySolved = true;
            OnLevelSolved?.Invoke();
        }
    }
}