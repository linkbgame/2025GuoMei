using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 文件数据服务实现
/// </summary>
public class FileDataService : IDataService
{
    private readonly ISerializer serializer;
    private readonly string dataPath;
    private readonly string fileExtension;

    public FileDataService(ISerializer serializer)
    {
        this.dataPath = Application.persistentDataPath;
        this.fileExtension = ".json";
        this.serializer = serializer;
        Debug.Log($"FileDataService已用路径初始化: {dataPath}");
    }

    /// <summary>获取文件完整路径</summary>
    private string GetPathToFile(string fileName)
    {
        return Path.Combine(dataPath, string.Concat(fileName, fileExtension));
    }

    /// <summary>
    /// 保存游戏数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="overwrite">是否允许覆盖</param>
    public void Save(GameData data, bool overwrite = true)
    {
        string fileLocation = GetPathToFile(data.Name);

        if (!overwrite && File.Exists(fileLocation))
        {
            throw new IOException($"这个文件'{data.Name}.{fileExtension}' 已经存在，无法覆盖。");
        }

        Debug.Log($"保存{data.Name}存档数据成功");

        File.WriteAllText(fileLocation, serializer.Serialize(data));
    }

    /// <summary>加载游戏数据</summary>
    public GameData Load(string name)
    {
        string fileLocation = GetPathToFile(name);

        if (!File.Exists(fileLocation))
        {
            throw new ArgumentException($"没有名为'{name}'的持久游戏数据");
        }

        Debug.Log($"加载{name}存档数据成功");

        return serializer.Deserialize<GameData>(File.ReadAllText(fileLocation));
    }

    /// <summary>删除指定存档</summary>
    public void Delete(string name)
    {
        string fileLocation = GetPathToFile(name);

        if (File.Exists(fileLocation))
        {
            File.Delete(fileLocation);
        }
        Debug.Log($"删除{name}存档存档成功");
    }

    /// <summary>删除所有存档</summary>
    public void DeleteAll()
    {
        foreach (string filePath in Directory.GetFiles(dataPath))
        {
            File.Delete(filePath);
        }
        Debug.Log("删除所有存档成功");
    }

    /// <summary>列出所有存档名称</summary>
    public IEnumerable<string> ListSaves()
    {
        Debug.Log("列出所有存档名称");

        //遍历 dataPath（存档目录）下的所有文件路径
        foreach (string path in Directory.EnumerateFiles(dataPath))
        {
            // 检查文件扩展名是否匹配
            if (Path.GetExtension(path) == fileExtension)
            {
                //提取文件名（去掉 .json 扩展名）
                yield return Path.GetFileNameWithoutExtension(path);
            }
        }
    }
}
