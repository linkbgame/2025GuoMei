using System;

/// <summary>
/// 不继承MonoBehaviour的泛型单例基类
/// </summary>
public class Singleton<T> where T : Singleton<T>
{
    //线程锁。当多线程访问时，同一时刻仅允许一个线程访问
    private static object locker = new object();

    //volatile关键字修饰的字段，当多个线程都对它进行修改时，可以确保这个字段在任何时刻呈现的都是最新的值
    private volatile static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    // 保证对象的唯一性
                    if (instance == null)
                    {
                        instance = Activator.CreateInstance(typeof(T), true) as T; // 使用反射创建实例
                    }
                }
            }
            return instance;
        }
    }

    // 私有构造函数，防止外部实例化
    protected Singleton() { }
}

