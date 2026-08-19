using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
[RequireComponent(typeof(RectTransform))]
public class GridLayoutAutoFill : MonoBehaviour
{
    public int columns = 3;
    public int rows = 3;

    GridLayoutGroup grid;
    RectTransform rectTransform;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        Resize();
    }

    void OnRectTransformDimensionsChange()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        Resize();
    }

    void Resize()
    {
        if (grid == null || rectTransform == null) return;

        float totalWidth = rectTransform.rect.width
            - grid.padding.left - grid.padding.right
            - grid.spacing.x * (columns - 1);

        float totalHeight = rectTransform.rect.height
            - grid.padding.top - grid.padding.bottom
            - grid.spacing.y * (rows - 1);

        float cellWidth = totalWidth / columns;
        float cellHeight = totalHeight / rows;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}