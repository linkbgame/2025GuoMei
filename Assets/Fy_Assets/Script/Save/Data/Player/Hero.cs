using UnityEngine;

/// <summary>
/// 英雄角色类
/// </summary>
public class Hero : MonoBehaviour
{
    /// <summary>角色数据</summary>
    [SerializeField]
    private PlayerData data;

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
        data = SaveLoadSystem.Instance.gameData.playerData;
        if (data == null)
        {
            data = new PlayerData { };
            SaveLoadSystem.Instance.gameData.playerData = data;
        }
        // 更新角色位置和旋转到数据
        data.position = transform.position;
        data.rotation = transform.rotation;
    }

    public void Load(GameData gameData)
    {
        this.data = gameData.playerData;
        if (data == null) return;
        if(data.position != Vector3.zero) {
            transform.position = data.position;
            transform.rotation = data.rotation;
        }
    }
}

