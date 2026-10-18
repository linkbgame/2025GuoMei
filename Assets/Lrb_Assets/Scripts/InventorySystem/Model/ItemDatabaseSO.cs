using System.Collections.Generic;
using UnityEngine;

//要提前创建所有道具的List，外部才能够创建
//创建友好得有一个enum，罗列所有写进allItems里面的string
namespace Inventory.Model
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "InventorySystem/ItemDatabase")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ItemSO> allItems;
        public List<ItemSO> GetAllItems() => allItems;

        // 运行时使用的字典
        [SerializeField] private Dictionary<string, ItemSO> itemSearchByID;
        [SerializeField] private Dictionary<string, ItemSO> itemSearchByName;

        // 在游戏启动或首次访问时初始化字典
        public void Init()
        {
            itemSearchByID = new Dictionary<string, ItemSO>();
            itemSearchByName = new Dictionary<string, ItemSO>();

            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemID))
                {
                    if (!itemSearchByID.ContainsKey(item.ItemID))//键为空则添加键值对
                    {
                        itemSearchByID.Add(item.ItemID, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemDatabase] Duplicate ID found: {item.ItemID}");
                    }
                }
            }

            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.Name))
                {
                    if (!itemSearchByName.ContainsKey(item.Name))//键为空则添加键值对
                    {
                        itemSearchByName.Add(item.Name, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[ItemDatabase] Duplicate Name found: {item.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// 通过 ItemSO.ItemID 获取 ItemSO 对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ItemSO GetItemByID(string id)
        {
            if (itemSearchByID == null) Init();

            if (itemSearchByID.TryGetValue(id, out ItemSO item))
            {
                return item;
            }

            Debug.LogError($"[物品数据库] ID为: {id} 的对象没有找到！！！");
            return null;
        }

        /// <summary>
        /// 通过 ItemSO.Name 获取 ItemSO 对象
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        public ItemSO GetItemByName(string itemName)
        {
            if (itemSearchByName == null) Init();

            if (itemSearchByName.TryGetValue(itemName, out ItemSO item))
            {
                return item;
            }

            Debug.LogError($"[物品数据库] Name为: {itemName} 的对象没有找到！！！");
            return null;
        }
    }
}