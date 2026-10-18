using System;
using UnityEngine;

/// <summary>
/// 玩家数据类 - 包含需要保存的玩家数据
/// </summary>
[Serializable]
public class PlayerData
{
    /// <summary>玩家位置</summary>
    public Vector3 position;
    /// <summary>玩家旋转</summary>
    public Quaternion rotation;
}

