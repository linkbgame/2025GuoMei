// 敌人脚本（实现数据绑定）
using System.Linq;
using UnityEngine;

//确保我们不会漏填加UniqueID脚本
[RequireComponent(typeof(UniqueID))]
[RequireComponent(typeof(NPCTakeSave))]
public class Enemy : DataBase
{
    [SerializeField] protected EnemyData data;
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

    protected override void Save()
    {
        //Debug.Log(11);
        // 尝试查找现有数据
        data = SaveLoadSystem.Instance.gameData.enemiesData.FirstOrDefault(d => d.Id == _id);
        if (data == null)
        {
            data = new EnemyData { Id = _id };
            SaveLoadSystem.Instance.gameData.enemiesData.Add(data);
        }
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.GetNPCData(GetComponent<NPCTakeSave>());
        // 更新其他状态...
    }

    protected override void Load(GameData gameData)
    {
        data = gameData.enemiesData.FirstOrDefault(d => d.Id == _id);
        if (data == null) return;
        if (data.position != Vector3.zero) {
            transform.position = data.position;
            transform.rotation = data.rotation;
        }
        data.SetNPCData(GetComponent<NPCTakeSave>());
        // 设置其他状态（血量等）

    }
}

