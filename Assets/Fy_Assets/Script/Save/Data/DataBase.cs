using UnityEngine;

//抽象数据基类
[RequireComponent(typeof(UniqueID))]//确保我们不会漏填加UniqueID脚本
public abstract class DataBase : MonoBehaviour
{

    protected string _id => GetComponent<UniqueID>().ID;

    //注册和注销事件 需要复制
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

    //保存抽象方法
    protected abstract void Save();
    //加载抽象方法
    protected abstract void Load(GameData gameData);
}

