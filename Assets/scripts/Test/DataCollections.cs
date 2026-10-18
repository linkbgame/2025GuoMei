using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TileProperty
{
    public Vector2Int tileCoordinate;
    public GridType gridType;
    public bool boolTypeValue;
}

[System.Serializable]
public class TileDetails
{
    public int girdX, gridY;
    public bool canPlaceFurniture;
    public bool isNPCObstacle;
    public int daysSinceDug = -1;
    public int daysSinceWatered = -1;
}

[System.Serializable]
public class NPCPosition
{
    public Transform npc;
    public string startScene;
    public Vector3 position;
}
//³¡¾°Â·¾¶
[System.Serializable]
public class SceneRoute
{
    public string fromSceneName;
    public string gotoSceneName;
    public List<ScenePath> scenePathList;
}

[System.Serializable]
public class ScenePath
{
    public string sceneName;
    public Vector2Int fromGridCell;
    public Vector2Int gotoGridCell;
}
