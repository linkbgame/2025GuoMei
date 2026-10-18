using ClueBoard.Model;
using Inventory.Model;
using Inventory.UI;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        public static InventoryController Instance;

        private PlayerStateManager m_stateManager;//开启背包后禁止移动

        [SerializeField]
        private UIInventoryPage inventoryUI;
        [SerializeField]
        public InventorySO inventoryData;
        [SerializeField]
        private ItemDatabaseSO itemDatabase;

        //初始物品列表
        public List<InventoryItem> initialItems = new List<InventoryItem>();

        //音效
        [SerializeField]
        private AudioClip dropClip;
        [SerializeField]
        private AudioSource audioSource;

        #region 存档系统订阅相关
        private void OnEnable()
        {
            EventManager.Instance.AddEventListener(EventNameEnum.Save, OnSaveGame);
            EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, OnLoadGame);
        }

        private void OnDisable()
        {
            EventManager.Instance.RemoveEventListener(EventNameEnum.Save, OnSaveGame);
            EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, OnLoadGame);
        }

        private void OnSaveGame()
        {
            // 获取当前背包数据
            var currentData = SaveLoadSystem.Instance.gameData;

            // 把 InventorySO 里的数据同步到 GameData 里
            currentData.bagData.bagContent = new List<InventoryItem>(inventoryData.inventoryItems);
        }

        private void OnLoadGame(GameData loadedData)
        {
            // 1. 拿到存档里的背包列表 
            List<InventoryItem> loadedItems = loadedData.bagData.bagContent;

            // 2. 覆盖
            inventoryData.inventoryItems = loadedItems;

            // 3. 配数据
            if (itemDatabase != null)
            {
                inventoryData.LoadDataFromSave(itemDatabase);
            }

            // 4. 刷新 UI
            inventoryData.InformAboutChange();
        }
        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            m_stateManager = GetComponent<PlayerStateManager>();

            // 确保数据库初始化
            if (itemDatabase != null) itemDatabase.Init();
        }

        private void OnDestroy()
        {
            inventoryData.OnInventoryUpdated -= UpdateInventoryUI;
            if (inventoryUI == null)
            {
                return;
            }
            inventoryUI.OnDescriptionRequested -= HandleDescriptionRequest;
            inventoryUI.OnSwapItems -= HandleSwapItems;
            inventoryUI.OnStartDragging -= HandleDragging;
            inventoryUI.OnItemActionRequested -= HandleItemActionRequest;
        }

        private void Start()
        {
            PrepareUI();
            PrepareInventoryData();   
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Toggle();
            }
        }

        /// <summary>
        /// 准备背包数据
        /// </summary>
        private void PrepareInventoryData()
        {
            inventoryData.Initialize();

            if (itemDatabase != null)
            {
                inventoryData.LoadDataFromSave(itemDatabase);
            }

            inventoryData.OnInventoryUpdated += UpdateInventoryUI;

            if (IsInventoryEmpty())
            {
                foreach (InventoryItem item in initialItems)
                {
                    if (item.IsEmpty)
                        continue;
                    inventoryData.AddItem(item);
                }
            }

            inventoryData.InformAboutChange();
        }

        /// <summary>
        /// 准备UI
        /// </summary>
        private void PrepareUI()
        {
            if (inventoryUI == null)
            {
                return;
            }
            inventoryUI.InitializeInventoryUI(inventoryData.Size);
            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;
        }

        /// <summary>
        /// 检查背包是否为空
        /// </summary>
        /// <returns>布尔值</returns>
        private bool IsInventoryEmpty()
        {
            foreach (var item in inventoryData.inventoryItems)
            {
                if (!item.IsEmpty) return false;
            }
            return true;
        }

        /// <summary>
        /// 处理UI描述文本的请求
        /// </summary>
        /// <param name="itemIndex"></param>
        private void HandleDescriptionRequest(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;
            }

            ItemSO item = inventoryItem.item;
            string description = PrepareDescription(inventoryItem);
            inventoryUI.UpdateDescription(itemIndex, item.ItemImage, item.Name, description);

            inventoryUI.SetCurrentDesItemCluePos(item.OriginalWorldPos);
        }

        /// <summary>
        /// 准备好物品数据里的描述文本
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        private string PrepareDescription(InventoryItem inventoryItem)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(inventoryItem.item.Description);
            sb.AppendLine();

            for(int i = 0; i < inventoryItem.itemState.Count; i++)
            {
                sb.Append($"{inventoryItem.itemState[i].itemParameter.ParameterName}" +
                    $": {inventoryItem.itemState[i].value} /" +
                    $"{inventoryItem.item.DefaultParametersList[i].value}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// 处理交换物品的请求
        /// </summary>
        /// <param name="itemIndex_1"></param>
        /// <param name="itemIndex_2"></param>
        private void HandleSwapItems(int itemIndex_1, int itemIndex_2)
        {
            inventoryData.SwapItems(itemIndex_1, itemIndex_2);
        }

        /// <summary>
        /// 处理拖拽物品的请求
        /// </summary>
        /// <param name="itemIndex"></param>
        private void HandleDragging(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;
            inventoryUI.CreateDraggedItem(inventoryItem.item.ItemImage, inventoryItem.quantity);
            inventoryUI.SetDraggedItemName(itemIndex, inventoryItem.item.name);
        }

        /// <summary>
        /// 处理右键UIItem显示行动选项的请求
        /// </summary>
        /// <param name="itemIndex"></param>
        private void HandleItemActionRequest(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;

            IItemAction itemAction = inventoryItem.item as IItemAction;//as的使用
            if (itemAction != null)
            {
                inventoryUI.ShowItemAction(itemIndex);
                inventoryUI.AddAction(itemAction.ActionName, () => PerformAction(itemIndex));  
            }

            //添加丢弃行为 
            IDroppableItem droppableItem = inventoryItem.item as IDroppableItem;
            if (droppableItem != null)
            {
                inventoryUI.AddAction("Drop", () => DropItem(itemIndex, inventoryItem.quantity));
            } 
        }   

        /// <summary>
        /// 执行行动选项
        /// </summary>
        /// <param name="itemIndex"></param>
        private void PerformAction(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;

            //你的理解：三个格子都满了，使用剑先将其销毁，再将一个参数降低的剑放进去；相反则放不进去

            IDestroyableItem destroyableItem = inventoryItem.item as IDestroyableItem;
            if (destroyableItem != null)
            {
                inventoryData.RemoveItem(itemIndex, 1);
            }

            IItemAction itemAction = inventoryItem.item as IItemAction;//as的使用
            if (itemAction != null)
            {
                itemAction.PerformAction(gameObject, inventoryItem.itemState);//传入当前物品状态属性
                audioSource.PlayOneShot(itemAction.ActionSFX);
                if (inventoryData.GetItemAt(itemIndex).IsEmpty)
                {
                    inventoryUI.ResetSelection();
                }
            }
        }

        /// <summary>
        /// 丢弃物品
        /// </summary>
        /// <param name="itemIndex"></param>
        /// <param name="quantity"></param>
        private void DropItem(int itemIndex, int quantity)
        {
            inventoryData.RemoveItem(itemIndex, quantity);
            inventoryUI.ResetSelection();
            audioSource.PlayOneShot(dropClip);
        }

        /// <summary>
        /// 响应SO背包数据变化，更新UI
        /// </summary>
        /// <param name="inventoryState"></param>
        private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
        {
            if (inventoryUI == null)
            {
                return;
            }
            inventoryUI.ResetAllItems();
            //更新整个背包
            foreach (var item in inventoryState)
            {
                inventoryUI.UpdateData(item.Key, item.Value.item.ItemImage,
                    item.Value.quantity);
            }
        }



        #region 公共方法
        /// <summary>
        /// 外部添加道具的调用方法
        /// </summary>
        public void AddItemToInventory(ItemEnum itemEnum, int quantity)
        {
            if (itemEnum == ItemEnum.None) return;

            // 将 Enum 转回字符串 ID
            // 注意：Enum的名字必须和ItemSO里的String ID保持一致（或者你有映射规则）
            string stringName = itemEnum.ToString();

            AddItemToInventory(stringName, quantity);
        }

        /// <summary>
        /// 用 ItemSO.Name 查找物品
        /// </summary>
        public void AddItemToInventory(string itemName, int quantity)
        {
            if (itemDatabase == null)
            {
                Debug.LogError("ItemDatabase not assigned in InventoryController!");
                return;
            }

            // 将该方法传入的参数，以 ItemID 的方式来查找物品
            //ItemSO item = itemDatabase.GetItemByID(itemName);

            // 以 ItemName 的方式来查找物品
            ItemSO item = itemDatabase.GetItemByName(itemName);

            if (item != null)
            {
                AddItemToInventory(item, quantity, null);
            }
        }

        /// <summary>
        /// 于底层传入 ItemSO 来添加物品的方法
        /// </summary>
        public void AddItemToInventory(ItemSO item, int quantity, List<ItemParameter> itemState = null)
        {
            inventoryData.AddItem(item, quantity, itemState);
        }

        /// <summary>
        /// 供按钮监听调用的背包开关
        /// </summary>
        public void Toggle()
        {
            bool isOpening = !inventoryUI.gameObject.activeSelf;

            if (isOpening)
            {
                if (m_stateManager != null) m_stateManager.SetMovementLock(true, this);

                inventoryUI.Show();//显示UI
                foreach (var item in inventoryData.GetCurrentInventoryState())//遍历背包数据字典然后更新UI
                {
                    inventoryUI.UpdateData(item.Key,
                        item.Value.item.ItemImage,
                        item.Value.quantity);
                }

                GetCurrentInventoryList();
            }
            else
            {
                if (m_stateManager != null) m_stateManager.SetMovementLock(false, this);
                inventoryUI.Hide();
            }
        }


        public void RemoveItemFromInventory(int itemIndex, int quantity)
        {
            inventoryData.RemoveItem(itemIndex, quantity);
            audioSource.PlayOneShot(dropClip);
        }

        public List<InventoryItem> GetCurrentInventoryList()
        {
            List<InventoryItem> returnValue = new List<InventoryItem>(new InventoryItem[inventoryData.inventoryItems.Count]);
            for (int i = 0; i < inventoryData.inventoryItems.Count; i++)
            {
                if (inventoryData.inventoryItems[i].IsEmpty)
                {
                    continue;
                }
                returnValue[i] = inventoryData.inventoryItems[i];
            }
            return returnValue;
        }

        /// <summary>
        /// 获取指定 ItemID 在当前背包中的总数量（会统计所有格子）。
        /// </summary>
        public int GetItemQuantityByID(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID) || inventoryData == null || inventoryData.inventoryItems == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < inventoryData.inventoryItems.Count; i++)
            {
                InventoryItem entry = inventoryData.inventoryItems[i];
                if (entry.IsEmpty) continue;
                if (entry.itemID == itemID)
                {
                    total += entry.quantity;
                }
            }

            return total;
        }
        #endregion
    }
}
