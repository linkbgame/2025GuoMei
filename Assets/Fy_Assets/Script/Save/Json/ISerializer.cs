/// <summary>
/// 序列化接口
/// </summary>
public interface ISerializer
{
    /// <summary>序列化对象为字符串</summary>
    string Serialize<T>(T obj);

    /// <summary>反序列化字符串为对象</summary>
    T Deserialize<T>(string json);
}

