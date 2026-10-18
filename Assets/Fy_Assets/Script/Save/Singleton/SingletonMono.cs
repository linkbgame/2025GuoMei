using UnityEngine;

/// <summary>
/// 继承MonoBehaviour的泛型单例基类
/// </summary>
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    //记录单例对象是否存在。用于防止在OnDestroy方法中访问单例对象报错
    public static bool IsExisted { get; private set; } = false;

    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name); // 创建游戏对象
                    instance = go.AddComponent<T>(); // 挂载脚本
                }
            }
            IsExisted = true;
            return instance;
        }
    }

    protected virtual void Awake()
    {
        DontDestroyOnLoad(this);
    }


    // 构造方法私有化，防止外部 new 对象
    protected SingletonMono() { }

    private void OnDestroy()
    {
        IsExisted = false;
    }
}

