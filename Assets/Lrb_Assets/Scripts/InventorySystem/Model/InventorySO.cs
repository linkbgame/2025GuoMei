using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;// 1. 务必引用 Newtonsoft.Json

namespace Inventory.Model
{
    [CreateAssetMenu(fileName = "InventorySO", menuName = "InventorySystem/Inventory/InventorySO")]
    public class InventorySO : ScriptableObject
    {       
        public List<InventoryItem> inventoryItems;

        [field: SerializeField]
        public int Size { get; private set; } = 10;

        public event Action<Dictionary<int, InventoryItem>> OnInventoryUpdated;

        #region public methods

        // 1. 改造 Initialize：把它变成“安全检查”
        // 作用：只在 List 为 null（从未被使用）时才初始化，如果有数据则不动。
        public void Initialize()
        {
            // 如果列表还没创建，创建一个空的
            if (inventoryItems == null)
            {
                //Debug.Log("列表还没创建，创建一个空的列表");
                inventoryItems = new List<InventoryItem>();
            }

            // 如果列表是空的（比如刚 new 出来，或者某种异常情况），填充空槽位
            // 这样保证了如果有存档数据（inventoryItems.Count > 0），这里不会覆盖它
            if (inventoryItems.Count == 0)
            {
                //Debug.Log("空列表将进行填充空槽位");
                for (int i = 0; i < Size; i++)
                {
                    inventoryItems.Add(InventoryItem.GetEmptyItem());
                }
            }
        }

        // 2. 新增：专门给“新游戏”按钮调用的方法
        // 作用：强制清空所有数据（在主菜单点击 New Game 时调用）
        public void ResetForNewGame()
        {
            inventoryItems = new List<InventoryItem>();
            for (int i = 0; i < Size; i++)
            {
                inventoryItems.Add(InventoryItem.GetEmptyItem());
            }
        }

        /// <summary>
        /// 数据重连，拿 itemID 去 ItemDatabaseSO 查背包 Item SO数据再填回去
        /// </summary>
        /// <param name="database"></param>
        public void LoadDataFromSave(ItemDatabaseSO database)
        {
            //Debug.Log($"[InventorySO.LoadDataFromSave] 开始执行数据恢复... 背包物品的类型数量有: {inventoryItems.Count}");
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                InventoryItem entry = inventoryItems[i];

                //Debug.Log("开始查库");

                //Debug.Log($"{entry.itemID} {entry.item}");

                // 调试日志：如果代码没执行，请查看这里的输出
                // 如果 itemID 为空，说明是旧存档，需要删除存档文件重新开始
                if (string.IsNullOrEmpty(entry.itemID) && entry.item == null)
                {
                    // 仅在非空槽位时打印警告，避免刷屏
                    if (entry.quantity > 0)
                        Debug.LogWarning($"[LoadData] 第 {i} 格物品数据异常：Quantity={entry.quantity} 但 ItemID 为空。请删除旧存档！");
                }

                // 如果有 ID 但是 item 是空的，说明是刚读档读进来的
                if (!string.IsNullOrEmpty(entry.itemID) && entry.item == null)
                {
                    // 查库
                    ItemSO realItem = database.GetItemByID(entry.itemID);
                    if (realItem != null)
                    {
                        Debug.Log($"[LoadData] 成功恢复物品引用: {realItem.Name} (ID: {entry.itemID})");
                        // 结构体赋值
                        entry.item = realItem;
                        inventoryItems[i] = entry;
                    }
                    else
                    {
                        Debug.LogError($"[LoadData] 数据库中找不到 ID 为 '{entry.itemID}' 的物品！");
                    }
                }
            }

            // 数据修好了，通知 UI 刷新
            InformAboutChange();
        }

        //背包控制器里初始化时调用
        public void AddItem(InventoryItem item)
        {
            AddItem(item.item, item.quantity);
        }

        //重要的添加物品方法，“增”
        public int AddItem(ItemSO item, int quantity, List<ItemParameter> itemState = null)
        {
            //不可堆叠
            if (item.IsStackable == false)
            {
                // 核心逻辑：只要还有剩余数量，且背包没满，就一个一个往里塞
                while (quantity > 0 && IsInventoryFull() == false)
                {
                    // 尝试添加 1 个物品到第一个空位
                    // 这里假设 AddItemToFirstFreeSlot 返回实际添加的数量（成功为1，没有空位置的话失败为0）
                    quantity -= AddItemToFirstFreeSlot(item, 1, itemState);
                }

                // 添加循环结束后，统一更新一次 UI
                InformAboutChange();

                // 返回剩余未添加的数量
                return quantity;
            }

            //可堆叠
            quantity = AddStackableItem(item, quantity);
            InformAboutChange();//更新UI
            return quantity;
        }

        //返回背包字典
        public Dictionary<int, InventoryItem> GetCurrentInventoryState()
        {
            Dictionary<int, InventoryItem> returnValue =
                new Dictionary<int, InventoryItem>();

            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEmpty)
                {
                    continue;
                }
                returnValue[i] = inventoryItems[i];
            }
            return returnValue;
        }

        //获取指定索引的物品数据，“查”
        public InventoryItem GetItemAt(int itemIndex)
        {
            return inventoryItems[itemIndex];
        }

        //交换物品数据
        public void SwapItems(int itemIndex_1, int itemIndex_2)
        {
            InventoryItem item1 = inventoryItems[itemIndex_1];
            inventoryItems[itemIndex_1] = inventoryItems[itemIndex_2];
            inventoryItems[itemIndex_2] = item1;
            InformAboutChange();//更新UI
        }

        //重要的移除物品方法，“删”
        public void RemoveItem(int itemIndex, int amount)
        {
            if (inventoryItems.Count > itemIndex)
            {
                if (inventoryItems[itemIndex].IsEmpty)
                    return;
                int reminder = inventoryItems[itemIndex].quantity - amount;
                if (reminder <= 0)
                {
                    inventoryItems[itemIndex] = InventoryItem.GetEmptyItem();
                }
                else
                {
                    inventoryItems[itemIndex] = inventoryItems[itemIndex].ChangeQuantity(reminder);
                }

                InformAboutChange();//更新UI
            }
        }

        //重要的回调方法，通知Controller来更新UI
        public void InformAboutChange()
        {
            //将更新后的背包字典发送出去
            OnInventoryUpdated?.Invoke(GetCurrentInventoryState());
        }

        #endregion

        #region private methods

        private bool IsInventoryFull()
            => inventoryItems.Where(item => item.IsEmpty).Any() == false;


        private int AddStackableItem(ItemSO item, int quantity)
        {
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                //空slot跳过
                if (inventoryItems[i].IsEmpty)
                    continue;
                //如果拾取到已有的物品，进行对应的逻辑；否则这个物品是新物品，循环整个列表后，进入下面的while进行对应的逻辑
                if (inventoryItems[i].item.ID == item.ID)
                {
                    //当前格子的最大容量减去当前数量，得到最大可添加数量
                    int amountPossibleToTake =
                        inventoryItems[i].item.MaxStackSize - inventoryItems[i].quantity;

                    if (quantity > amountPossibleToTake)
                    {
                        inventoryItems[i] = inventoryItems[i].ChangeQuantity(inventoryItems[i].item.MaxStackSize);
                        quantity -= amountPossibleToTake;
                    }
                    else
                    {
                        inventoryItems[i] = inventoryItems[i].ChangeQuantity(inventoryItems[i].quantity + quantity);
                        InformAboutChange();
                        return 0;
                    }
                }
            }

            //新物品进行的添加逻辑
            while(quantity > 0 && IsInventoryFull() == false)
            {
                int newQuantity = Mathf.Clamp(quantity, 0, item.MaxStackSize);
                quantity -= newQuantity;
                AddItemToFirstFreeSlot(item, newQuantity);
            }
            return quantity;
        }

        /// <summary>
        /// 最底层添加物品
        /// </summary>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        /// <param name="itemState"></param>
        /// <returns></returns>
        private int AddItemToFirstFreeSlot(ItemSO item, int quantity, List<ItemParameter> itemState = null)
        {
            InventoryItem newItem = new InventoryItem
            {
                item = item,
                quantity = quantity,
                itemID = item.ItemID,//3.新加：确保同步

                //若物品参数状态不为空，则使用传入的参数状态，否则使用物品默认参数状态
                itemState =
                new List<ItemParameter>(itemState == null ? item.DefaultParametersList : itemState)
            };


            for (int i = 0; i < inventoryItems.Count; i++)
            {
                //找到空的slot，然后把结构体对象塞进去
                if (inventoryItems[i].IsEmpty)
                {
                    inventoryItems[i] = newItem;
                    return quantity;
                }
            }
            return 0;
        }

        #endregion
    }

    //p13详细解释堆与栈，使用结构体和类
    //背包中的基本单位
    [Serializable]
    public struct InventoryItem
    {
        public int quantity;


        // 2. 核心修改：禁止 Json 触碰这个 SO 引用
        // 这样就不会报 "must be instantiated using CreateInstance" 的错了
        [JsonIgnore]
        public ItemSO item;

        // 3. 新增：专门用来存盘的 ID
        /// <summary>
        /// 专门用来存档存盘的 ID
        /// </summary>
        public string itemID;


        public List<ItemParameter> itemState;
        public bool IsEmpty => item == null;

        public InventoryItem ChangeQuantity(int newQuantity)
        {
            return new InventoryItem
            {
                item = this.item,
                itemID = this.itemID,
                quantity = newQuantity,
                itemState = new List<ItemParameter>(this.itemState)
            };
        }

        /*public InventoryItem ChangeQuantity(int newQuantity)
            => new InventoryItem
            {
                item = this.item,
                quantity = newQuantity
            }*/

        public static InventoryItem GetEmptyItem()
            => new InventoryItem
            {
                item = null,
                itemID = string.Empty,
                quantity = 0,
                itemState = new List<ItemParameter>()
            };
    }
}
