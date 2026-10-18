using ClueBoard.Model;
using System;
using System.Collections.Generic;

/// <summary>
/// 游戏数据类
/// </summary>
[Serializable]
public class GameData
{
    public string Name;             // 游戏存档名称
    public int num;                 //占的存档格位置
    public string CurrentLevelName;  // 当前场景名称
    /// <summary>玩家数据</summary>
    public PlayerData playerData;
    /// <summary>敌人/道具数据列表</summary>
    public List<EnemyData> enemiesData = new List<EnemyData>();
    /// <summary>其他数据</summary>
    public OtherData otherData;
    /// <summary>背包数据的存储/// </summary>
    public BagData bagData;
    /// <summary>线索板线索数据的存储/// </summary>
    public List<ClueSaveData> collectedClues = new List<ClueSaveData>();
    /// <summary>线索板已完成推理链的存储/// </summary>
    public List<string> finishedChains = new List<string>();
    /// <summary>用序号对应事件，序号变化则存档存储错误/// </summary>
    public List<bool> eventKey = new List<bool>();
}

