using Inventory;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakeBagSave : MonoBehaviour
{
    /// <summary>角色数据</summary>
    [SerializeField]
    private BagData data;

    void OnEnable()
    {
        EventManager.Instance.AddEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, Load);
    }
    void OnDisable()
    {
        EventManager.Instance.RemoveEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, Load);
    }
    private void Save()
    {
        // 尝试查找现有数据
        data = SaveLoadSystem.Instance.gameData.bagData;
        if (data == null)
        {
            data = new BagData { };
            SaveLoadSystem.Instance.gameData.bagData = data;
        }
        // 更新角色位置和旋转到数据
        data.bagContent = InventoryController.Instance.GetCurrentInventoryList();
    }

    public void Load(GameData gameData)
    {
        this.data = gameData.bagData;
        if (data == null) return;
        for (int i = 0; i < data.bagContent.Count; i++) 
        {
            //InventoryController.Instance.AddItemToInventory(data.bagContent[i].item, data.bagContent[i].quantity);
        }
    }
}
