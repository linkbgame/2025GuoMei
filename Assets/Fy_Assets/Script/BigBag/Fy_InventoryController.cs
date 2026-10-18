using System.Collections.Generic;
using UnityEngine;

public class Fy_InventoryController : MonoBehaviour
{
    public ItemGrid selectedItemGrid;//操作的背包
    Item selectedItem;//选中物品

    [SerializeField] List<ItemData> items;
    [SerializeField] GameObject itemPrefab;
    Canvas canvas;
    InventoryHighlight inventoryHighlight;
    Item itemToHighlight;//高亮显示物品

    //鼠标坐标转化为格子坐标
    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidth / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }
        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(position);
        return tileGridPosition;
    }
    Vector2Int oldPosition;

    //随机入库物品
    private void InsertRandomItem()
    {
        if (selectedItemGrid == null) return;

        int index = UnityEngine.Random.Range(0, items.Count);
        // 在网格中找到适合放置物品的位置
        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(items[index]);
        if (posOnGrid == null) return;

        Item item = Instantiate(itemPrefab).GetComponent<Item>();
        item.transform.SetParent(canvas.transform, false);
        item.Set(items[index]);

        // 将物品放置到网格中的指定位置
        selectedItemGrid.PlaceItem(item, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    //高亮显示
    private void HandleHighlight()
    {
        Vector2Int positionOnGrid = GetTileGridPosition();

        //节约没必要的计算
        if (oldPosition == positionOnGrid) return;
        oldPosition = positionOnGrid;
        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetParent(selectedItemGrid);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            inventoryHighlight.Show(selectedItemGrid.BoundryCheck(
                    positionOnGrid.x,
                    positionOnGrid.y,
                    selectedItem.WIDTH,
                    selectedItem.HEIGHT)
            );//防止显示跨界
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetParent(selectedItemGrid);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
        }
    }
    //点击操作，选中物品
    private void LeftMouseButtonPress()
    {
        Vector2Int tileGridPosition = GetTileGridPosition();
        if (selectedItem == null)
        {
            //选中物品
            selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
            selectedItem.transform.SetAsLastSibling();
        }
        else
        {
            // 移动物品
            PlaceItem(tileGridPosition);
        }
    }

    //移动物品
    void PlaceItem(Vector2Int tileGridPosition)
    {
        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if (complete)
        {
            selectedItem = null;

            //如果存在重叠物品
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
                selectedItem.transform.SetAsLastSibling();
            }
        }
    }

    //随机添加物品
    private void CreateRandomItem()
    {
        if (selectedItem) return;
        Item item = Instantiate(itemPrefab).GetComponent<Item>();
        selectedItem = item;
        selectedItem.transform.SetParent(canvas.transform, false);
        selectedItem.transform.SetAsLastSibling();
        int index = UnityEngine.Random.Range(0, items.Count);
        item.Set(items[index]);
    }

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        inventoryHighlight = GetComponent<InventoryHighlight>();
    }



    Item overlapItem;//重叠物品

    private void Update()
    {
        //TODO: 方便测试，动态随机添加物品
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CreateRandomItem();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            InsertRandomItem();
        }
        //旋转物品
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }

        //物品跟随鼠标
        if (selectedItem) selectedItem.transform.position = Input.mousePosition;

        if (selectedItemGrid == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButtonPress();
        }

        //高亮显示
        HandleHighlight();
    }

    //旋转物品
    void RotateItem()
    {
        if (selectedItem == null) return;
        selectedItem.Rotate();
    }

}

