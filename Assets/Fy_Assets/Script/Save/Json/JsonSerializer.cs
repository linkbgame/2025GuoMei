using Newtonsoft.Json;

/// <summary>
/// JSON序列化实现
/// </summary>
public class JsonSerializer : ISerializer
{
    /// <summary>序列化对象为JSON字符串</summary>
    public string Serialize<T>(T obj)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
        };
        return JsonConvert.SerializeObject(obj, settings);
    }

    /// <summary>从JSON字符串反序列化为对象</summary>
    public T Deserialize<T>(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
        };
        return JsonConvert.DeserializeObject<T>(json, settings);
    }
}
