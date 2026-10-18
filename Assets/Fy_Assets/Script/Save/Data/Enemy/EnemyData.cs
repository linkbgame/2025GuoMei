// 新增敌人数据类
using System;
using UnityEngine;

[Serializable]
public class EnemyData
{
    public string Id { get; set; }// 唯一标识
    public Vector3 position;
    public Quaternion rotation;
    public float health;
    public bool isInThisScene;
    public string nowTalkingName;

    public void GetNPCData(NPCTakeSave npcTakeSave)
    {
        isInThisScene = npcTakeSave.isInThisScene;
        nowTalkingName = npcTakeSave.nowTalkingName;
    }
    public void SetNPCData(NPCTakeSave npcTakeSave)
    {
        npcTakeSave.isInThisScene = isInThisScene;
        npcTakeSave.nowTalkingName = nowTalkingName;
    }
}

