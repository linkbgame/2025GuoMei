using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SaveOne : MonoBehaviour
{
    [SerializeField] protected OtherData data;
    public Image image;//截图
    public int num;//对应的格子位置
    public Text playTime;
    public GameData nowFile;
    public AboutUI father;
    public bool canRead;
    public bool canWrite;
    public Button Get, Down, Des;
    public SaveOne(GameData now)
    {
        //Debug.Log(1);
        nowFile = now;
        int times= (int)now.otherData.playTime;
        times = times / 60;
        playTime.text = times.ToString();
    }
    void Start()
    {

        //EventManager.Instance.AddEventListener(EventNameEnum.Save, Save);
        //EventManager.Instance.AddEventListener<GameData>(EventNameEnum.Load, Load);
    }
    private void Update()
    {
        if (nowFile.otherData.playTime == -100)  
        {
            canRead = false;
        }
        else
        {
            canRead = true;
        }
    }
    public void StartSaveOne()
    {
        Get.onClick.AddListener(ButtonGet);
        Down.onClick.AddListener(ButtonDown);
        Des.onClick.AddListener(ButtonDes);
    }
    public void ButtonDown()
    {
        //father.ChoiceSaveOne(num);
        father.SetButtonDown(num);
    }
    public void ButtonGet()
    {
        father.SetButtonGet(num);
    }
    public void ButtonDes()
    {
        father.DeleteOneGame(num);
    }

    /*
    void OnDisable()
    {
        EventManager.Instance.RemoveEventListener(EventNameEnum.Save, Save);
        EventManager.Instance.RemoveEventListener<GameData>(EventNameEnum.Load, Load);
    }
    */
    //可能用不到的两个方法
    protected void Save()
    {
        if (num != SaveLoadSystem.Instance.gameData.num)
            return;
        Debug.Log(11);
        // 尝试查找现有数据
        //data = SaveLoadSystem.Instance.gameData.otherData;
        SaveLoadSystem.Instance.gameData.Name = num.ToString();

        if (data == null)//没有记录，要新建
        {
            data = new OtherData { };
            data.ThisTime = DateTime.Now.ToString("yyyy/M/d/H：mm");
            ReadDown.instance.ThisTime = DateTime.Now.ToString("yyyy/M/d/H：mm");
            ReadDown.instance.playTime = 0;
            //SaveLoadSystem.Instance.gameData.otherData = data;
            SaveLoadSystem.Instance.gameData.num = num;
        }
        //更新记录状态
        data.playTime = ReadDown.instance.playTime;
        data.ThisTime = ReadDown.instance.ThisTime;
        // 更新其他状态...
    }

    protected void Load(GameData gameData)
    {
        //data = SaveLoadSystem.Instance.gameData.otherData;
        SaveLoadSystem.Instance.gameData.Name = num.ToString();
        if (data == null) return;
        //后续改成枚举 方便直接加
        ReadDown.instance.ThisTime = data.ThisTime;
        //继续存档的进度去累加
        ReadDown.instance.playTime = data.playTime;
        // 设置其他状态（血量等）
        SaveLoadSystem.Instance.LoadGame(num.ToString());
    }
}
