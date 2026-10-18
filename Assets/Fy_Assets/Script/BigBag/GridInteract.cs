using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Fy_InventoryController inventoryController;
    private ItemGrid itemGrid;

    private void Awake()
    {
        inventoryController = FindObjectOfType<Fy_InventoryController>();
        itemGrid = GetComponent<ItemGrid>();
    }

    // 鼠标进入触发
    public void OnPointerEnter(PointerEventData eventData)
    {
        inventoryController.selectedItemGrid = itemGrid;
    }

    // 鼠标退出触发
    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryController.selectedItemGrid = null;
    }
}

