using UnityEngine;

public class Singleton2<T> : MonoBehaviour where T : Singleton2<T>
{
    private static T instance;

    public static T Instance
    {
        get => instance;
    }

    protected virtual void Awake()
    {
            instance = (T)this;
    }

}

