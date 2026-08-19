using UnityEngine;
using UnityEngine.UI;

public class InventoryTrayView : MonoBehaviour
{
    public DraggableGateItem itemPrefab;
    public RectTransform trayParent; // pasang GridLayoutGroup / HorizontalLayoutGroup di object ini
    public GateIconSet iconSet;

    public void BuildTray(GridObjectType[] availableGates)
    {
        foreach (Transform child in trayParent) Destroy(child.gameObject);

        foreach (var gateType in availableGates)
        {
            DraggableGateItem item = Instantiate(itemPrefab, trayParent);
            item.gateType = gateType;

            Image image = item.GetComponent<Image>();
            if (image != null && iconSet != null)
                image.sprite = iconSet.GetSprite(gateType);
        }
    }
}