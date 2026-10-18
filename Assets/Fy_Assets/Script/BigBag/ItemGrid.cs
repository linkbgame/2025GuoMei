using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    public GameObject itemPrefab;

    // 定义每个格子的宽度和高度
    public const float tileSizeWidth = 256 / 4;
    public const float tileSizeHeight = 256 / 4;

    // 计算在格子中的位置
    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int tileGridPosition = new Vector2Int();

    RectTransform rectTransform;
    Canvas canvas;
    [SerializeField] int gridSizeWidth = 10;
    [SerializeField] int gridSizeHeight = 10;

    Item[,] itemSlot;//存储物品位置信息

    private void Start()
    {
        itemSlot = new Item[gridSizeWidth, gridSizeHeight];

        rectTransform = GetComponent<RectTransform>();
        canvas = FindObjectOfType<Canvas>();

        Init(gridSizeWidth, gridSizeHeight);

    }

    //判断物品是否超出边界
    public bool BoundryCheck(int posX, int posY, int width, int height)
    {
        if (PositionCheck(posX, posY) == false) return false;

        posX += width - 1;
        posY += height - 1;
        if (PositionCheck(posX, posY) == false) return false;
        return true;
    }

    //判断格子坐标是否超出
    bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0) return false;

        if (posX >= gridSizeWidth || posY >= gridSizeHeight) return false;

        return true;
    }


    //按格子坐标添加物品
    public bool PlaceItem(Item item, int posX, int posY, ref Item overlapItem)
    {
        //判断物品是否超出边界
        if (BoundryCheck(posX, posY, item.WIDTH, item.HEIGHT) == false) return false;

        //检查指定位置和范围内是否存在重叠物品，有多个重叠物品退出
        if (OverlapCheck(posX, posY, item.WIDTH, item.HEIGHT, ref overlapItem) == false) return false;

        if (overlapItem) CleanGridReference(overlapItem);

        PlaceItem(item, posX, posY);

        return true;
    }

    //按格子坐标添加物品
    public void PlaceItem(Item item, int posX, int posY)
    {
        item.transform.SetParent(transform, false);

        // 按物品尺寸占用对应大小的格子
        for (int ix = 0; ix < item.WIDTH; ix++)
        {
            for (int iy = 0; iy < item.HEIGHT; iy++)
            {
                itemSlot[posX + ix, posY + iy] = item;
            }
        }
        item.onGridPositionX = posX;
        item.onGridPositionY = posY;

        Vector2 position = CalculatePositionOnGrid(item, posX, posY);

        item.transform.localPosition = position;
    }

    // 检查指定位置是否有足够的空间来放置物品
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (itemSlot[posX + x, posY + y] != null)
                {
                    return false; // 如果当前位置已经有物品，则返回false
                }
            }
        }
        return true; // 如果所有位置都空闲，则返回true
    }

    // 在网格中找到适合放置物品的位置Data
    public Vector2Int? FindSpaceForObject(ItemData itemData)
    {
        int height = gridSizeHeight - itemData.height + 1;
        int width = gridSizeWidth - itemData.width + 1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CheckAvailableSpace(x, y, itemData.width, itemData.height) == true)
                {
                    return new Vector2Int(x, y); // 返回找到的空闲位置
                }
            }
        }
        return null; // 如果没有找到合适的位置，则返回null
    }

    //按格子坐标添加物品


    //检查指定位置和范围内是否存在重叠物品，并overlapItem返回重叠物品，，有多个重叠物品返回false
    private bool OverlapCheck(int posX, int posY, int width, int height, ref Item overlapItem)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 如果当前位置有物品
                if (itemSlot[posX + x, posY + y] != null)
                {
                    // 如果 overlapItem 还未被赋值（第一次找到重叠物品）
                    if (overlapItem == null)
                    {
                        overlapItem = itemSlot[posX + x, posY + y];
                    }
                    else
                    {
                        // 如果发现范围有多个重叠物品
                        if (overlapItem != itemSlot[posX + x, posY + y])
                        {
                            overlapItem = null;
                            return false;
                        }
                    }
                }
            }
        }
        // 如果所有被检查的位置都有相同的重叠物品，则返回 true
        return true;
    }

    //按格子坐标获取物品
    public Item PickUpItem(int x, int y)
    {
        Item toReturn = itemSlot[x, y];

        if (toReturn == null) return null;

        CleanGridReference(toReturn);

        return toReturn;
    }

    //按物品尺寸取消对应大小的格子的占用
    void CleanGridReference(Item item)
    {
        for (int ix = 0; ix < item.WIDTH; ix++)
        {
            for (int iy = 0; iy < item.HEIGHT; iy++)
            {
                itemSlot[item.onGridPositionX + ix, item.onGridPositionY + iy] = null;
            }
        }
    }


    void Init(int width, int height)
    {
        Vector2 size = new Vector2(width * tileSizeWidth, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }


    private void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            // 获取当前鼠标位置在网格中的格子坐标，并打印到控制台
            Debug.Log(GetTileGridPosition(Input.mousePosition));
        }

    }
    //按格子坐标转化为UI坐标位置
    public Vector2 CalculatePositionOnGrid(Item item, int posX, int posY)
    {
        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidth + tileSizeWidth * item.WIDTH / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * item.HEIGHT / 2);
        return position;
    }

    //按格子坐标获取物品
    internal Item GetItem(int x, int y)
    {
        return itemSlot[x, y];
    }

    // 根据鼠标位置计算在格子中的位置
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        // 计算鼠标位置相对于 RectTransform 的偏移量
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        // 将偏移量转换为网格位置
        // 这里假设 tileSizeWidth 和 tileSizeHeight 是单个瓦片的宽度和高度
        // canvas.scaleFactor 是 Canvas 的缩放因子（通常用于 UI 适配不同分辨率）
        tileGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidth / canvas.scaleFactor);
        tileGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight / canvas.scaleFactor);

        // 返回计算出的网格位置
        return tileGridPosition;
    }
}


