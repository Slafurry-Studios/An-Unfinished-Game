using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCellView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [HideInInspector] public int x;
    [HideInInspector] public int y;
    [HideInInspector] public GridBoardView board;

    [Header("Visual")]
    public Image background; // warna sinyal (kosong/off/on/benar/salah)
    public Image icon;       // sprite gate yang lagi nempatin cell ini

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DraggableGateItem draggedItem = eventData.pointerDrag.GetComponent<DraggableGateItem>();
        if (draggedItem == null) return;

        bool placed = board.Circuit.PlaceObject(x, y, draggedItem.gateType);
        if (!placed) return; // gagal (misal cell ini Input/Output yang fixed)

        if (draggedItem.consumeOnPlace)
            draggedItem.gameObject.SetActive(false);

        board.RefreshVisuals();
        board.OnPlacementChanged?.Invoke();
    }

    // Klik cell yang udah keisi gate (bukan Input/Output) buat hapus lagi
    public void OnPointerClick(PointerEventData eventData)
    {
        GridCell cell = board.Circuit.GetCell(x, y);
        if (cell == null) return;
        if (cell.type == GridObjectType.Input || cell.type == GridObjectType.Output) return;
        if (cell.type == GridObjectType.Empty) return;

        board.Circuit.RemoveObject(x, y);
        board.RefreshVisuals();
        board.OnPlacementChanged?.Invoke();
    }
}