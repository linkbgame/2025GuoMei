using Inventory;
using Inventory.Model;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChuPuMananger : MonoBehaviour
{
    public GameObject winUI;
    public GameObject loseUI;
    public GameObject NoOther;
    public GameObject AllofChess;
    public GameObject AllofFight;
    public BagData data;
    [LabelText("花费的筹码")]
    public int GuDIngChouMa;

    private int ChouMaChance;
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
    /// <summary>
    /// 玩家最后胜利事件
    /// </summary>
    public void Win()
    {
        winUI.SetActive(true);
    }
    /// <summary>
    /// 玩家最后失败事件
    /// </summary>
    public void Lose()
    {
        loseUI.SetActive(true);
    }
    /// <summary>
    /// 最后平局事件
    /// </summary>
    public void NoWinLose()
    {
        NoOther.SetActive(true);
    }
    public void OverGame()
    {
        SaveLoadSystem.Instance.SaveGame();
        SceneManager.LoadScene("PersistentScenes");
    }

    public void ShowAllChess()
    {
        AllofChess.SetActive(true);
        AllofFight.SetActive(false);
    }
    public void ButtonWin()
    {
        ChouMaChance = GuDIngChouMa;
        OverGame();
    }
    public void ButtonLose()
    {
        ChouMaChance = -GuDIngChouMa;
        OverGame();
    }
    public void ButtonNo()
    {
        ChouMaChance = 0;
        OverGame();
    }
    private void Save()
    {
        // 尝试查找现有数据
        data = SaveLoadSystem.Instance.gameData.bagData;
        if (data == null)
        {
            data = new BagData { };
            //data.ChouMa = 100;
            SaveLoadSystem.Instance.gameData.bagData = data;
        }
        // 更新筹码数量信息
        InventoryController.Instance.AddItemToInventory(FindItemByName("筹码"), ChouMaChance);
        //data.ChouMa += ChouMaChance;
        //SaveLoadSystem.Instance.gameData.bagData.ChouMa += ChouMaChance;
    }


    private ItemSO FindItemByName(string name)
    {
        for (int i = 0; i < allofBagSth.Count; i++)
        {
            if (allofBagSth[i].Name == name)
            {
                return allofBagSth[i];
            }
        }
        return null;
    }
    public List<ItemSO> allofBagSth;

    public void Load(GameData gameData)
    {
        this.data = gameData.bagData;
        if (data == null) return;
    }
}
