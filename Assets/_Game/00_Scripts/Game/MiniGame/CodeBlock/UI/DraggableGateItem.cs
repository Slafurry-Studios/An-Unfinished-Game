using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableGateItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Jenis gate yang direpresentasikan item ini")]
    public GridObjectType gateType;

    [Header("Kalau true, item ini hilang dari tray setelah berhasil ditaruh (limited-use)")]
    public bool consumeOnPlace = false;

    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Canvas rootCanvas;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalAnchoredPos = rect.anchoredPosition;

        // Pindah jadi child langsung dari root canvas biar render di atas semua UI lain
        transform.SetParent(rootCanvas.transform, true);

        // Matikan raycast blocking selama drag, biar GridCellView di bawahnya
        // tetap bisa kedeteksi sebagai target OnDrop
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Kalau OnDrop di cell gak memindahkan objek ini (drop gagal/di luar grid),
        // baliki ke posisi asal di tray
        if (transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent);
            rect.anchoredPosition = originalAnchoredPos;
        }
    }
}