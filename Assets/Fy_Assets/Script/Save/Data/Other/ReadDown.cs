using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadDown : SingletonMono<ReadDown>
{
    public static ReadDown instance;
    public Button button;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public float playTime;//当前的存档的游戏时长
    public string ThisTime;//当前存档的时间
    public void Update()
    {
        playTime += Time.deltaTime;
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            //button.onClick.Invoke();
        }
    }
    /// <summary>其他存档信息数据</summary>
    [SerializeField]
    private OtherData data;

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
        data = SaveLoadSystem.Instance.gameData.otherData;
        //Debug.Log(2);
        if (data.playTime == 0)
        {
            //Debug.Log(1);
            data = new OtherData { };
            playTime = 1;
            ThisTime = DateTime.Now.ToString("yyyy/M/d/H：mm");
            SaveLoadSystem.Instance.gameData.otherData = data;
        }
        // 更新角色,,到数据
        data.playTime = playTime;
        data.ThisTime = ThisTime;
    }

    public void Load(GameData gameData)
    {
        this.data = gameData.otherData;
        if (data == null) return;
        playTime = gameData.otherData.playTime;
        ThisTime = gameData.otherData.ThisTime;
    }
}
