using UnityEditor;
using UnityEngine;

/// <summary>
/// 打开存储目录
/// </summary>
public class OpenSaveInFolder
{
    [MenuItem("SaveTool/打开存储目录")]
    private static void OpenInSaveFolder()
    {
        OpenInFolder(Application.persistentDataPath);
    }

    /// <summary>
    /// 打开某个文件夹
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    public static void OpenInFolder(string folderPath)
    {
        Application.OpenURL("file:///" + folderPath);
    }
}

