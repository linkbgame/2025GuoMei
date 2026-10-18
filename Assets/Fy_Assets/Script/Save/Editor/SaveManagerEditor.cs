
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器编辑器扩展
/// </summary>
[CustomEditor(typeof(SaveLoadSystem))]
public class SaveManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        SaveLoadSystem saveLoadSystem = (SaveLoadSystem)target;
        string gameName = saveLoadSystem.gameData.Name;

        DrawDefaultInspector();

        if (GUILayout.Button("保存游戏数据"))
        {
            saveLoadSystem.SaveGame();
        }

        if (GUILayout.Button("加载游戏数据"))
        {
            saveLoadSystem.LoadGame(gameName);
        }

        if (GUILayout.Button("重新加载当前游戏"))
        {
            saveLoadSystem.ReloadGame();
        }

        if (GUILayout.Button("删除该游戏数据"))
        {
            saveLoadSystem.DeleteGame(gameName);
        }

        if (GUILayout.Button("删除所有存档"))
        {
            saveLoadSystem.DeleteAllGame();
        }

        if (GUILayout.Button("列出所有存档名称"))
        {
            saveLoadSystem.ListSavesGame();
        }

        if (GUILayout.Button("跳转到Fy_Text_1场景"))
        {
            SceneManager.LoadScene("Fy_Text_1");
        }

        if (GUILayout.Button("跳转到Fy_Text场景"))
        {
            SceneManager.LoadScene("Fy_Text");
        }
    }
}

