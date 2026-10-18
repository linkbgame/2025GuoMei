using System.Collections.Generic;

/// <summary>
/// 数据服务接口
/// </summary>
public interface IDataService
{
    /// <summary>保存游戏数据</summary>
    /// <param name="overwrite">是否覆盖已存在的文件</param>
    void Save(GameData data, bool overwrite = true);

    /// <summary>加载指定名称的游戏数据</summary>
    GameData Load(string name);

    /// <summary>删除指定名称的存档</summary>
    void Delete(string name);

    /// <summary>删除所有存档</summary>
    void DeleteAll();

    /// <summary>列出所有存档名称</summary>
    IEnumerable<string> ListSaves();
}

