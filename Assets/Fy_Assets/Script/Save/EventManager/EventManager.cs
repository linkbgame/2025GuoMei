using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Events;
/// <summary>
/// 事件管理器，之所以这么多函数，主要是出于性能考虑，避免产生GC、装箱问题
/// </summary>
public class EventManager : Singleton<EventManager>
{
    Dictionary<string, IEventInfo> eventsDictionary = new Dictionary<string, IEventInfo>();

    /// <summary>
    /// 获取事件名称
    /// </summary>
    /// <param name="eventNameEnum">事件枚举</param>
    /// <returns>事件名称</returns>
    private string GetEnventName(object EventNameEnum)
    {
        return EventNameEnum.GetType().Name + "_" + EventNameEnum.ToString();
    }

    #region 事件监听
    /// <summary>
    /// 无参数的事件监听
    /// </summary>
    /// <param name="EventNameEnum">事件枚举</param>
    /// <param name="action">监听方法</param>
    public void AddEventListener(object EventNameEnum, UnityAction call)
    {
        //UnityEngine.Debug.Log("1");
        string eventName = GetEnventName(EventNameEnum);
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo).action += call;
        }
        else
        {
            eventsDictionary.Add(eventName, new EventInfo(call));
        }
    }

    /// <summary>
    /// 带1个参数的事件监听
    /// </summary>
    public void AddEventListener<T>(object EventNameEnum, UnityAction<T> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T).Name;
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo<T>).action += call;
        }
        else
        {
            eventsDictionary.Add(eventName, new EventInfo<T>(call));
        }
    }

    /// <summary>
    /// 带2个参数的事件监听
    /// </summary>
    public void AddEventListener<T0, T1>(object EventNameEnum, UnityAction<T0, T1> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name;
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo<T0, T1>).action += call;
        }
        else
        {
            eventsDictionary.Add(eventName, new EventInfo<T0, T1>(call));
        }
    }

    /// <summary>
    /// 带3个参数的事件监听
    /// </summary>
    public void AddEventListener<T0, T1, T2>(object EventNameEnum, UnityAction<T0, T1, T2> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name;
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2>).action += call;
        }
        else
        {
            eventsDictionary.Add(eventName, new EventInfo<T0, T1, T2>(call));
        }
    }

    /// <summary>
    /// 带4个参数的事件监听
    /// </summary>
    public void AddEventListener<T0, T1, T2, T3>(object EventNameEnum, UnityAction<T0, T1, T2, T3> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name + "_" + typeof(T3).Name;
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2, T3>).action += call;
        }
        else
        {
            eventsDictionary.Add(eventName, new EventInfo<T0, T1, T2, T3>(call));
        }
    }
    #endregion

    #region 触发事件
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="EventNameEnum">事件枚举</param>
    public void Dispatch(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum);
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo).action?.Invoke();
        }
    }
    /// <summary>
    /// 触发带1个参数事件
    /// </summary>
    public void Dispatch<T>(object EventNameEnum, T parameter)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T).Name;
        //如果字典中该事件的名字存在，且该事件不为空，则执行该事件，不存在则什么也不做。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T>).action?.Invoke(parameter);
    }
    /// <summary>
    /// 触发带2个参数事件
    /// </summary>
    public void Dispatch<T0, T1>(object EventNameEnum, T0 parameter0, T1 parameter1)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name;
        //如果字典中该事件的名字存在，且该事件不为空，则执行该事件，不存在则什么也不做。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1>).action?.Invoke(parameter0, parameter1);
    }
    /// <summary>
    /// 触发带3个参数事件
    /// </summary>
    public void Dispatch<T0, T1, T2>(object EventNameEnum, T0 parameter0, T1 parameter1, T2 parameter2)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name;
        //如果字典中该事件的名字存在，且该事件不为空，则执行该事件，不存在则什么也不做。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2>).action?.Invoke(parameter0, parameter1, parameter2);
    }
    /// <summary>
    /// 触发带4个参数事件
    /// </summary>
    public void Dispatch<T0, T1, T2, T3>(object EventNameEnum, T0 parameter0, T1 parameter1, T2 parameter2, T3 parameter3)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name + "_" + typeof(T3).Name;
        //如果字典中该事件的名字存在，且该事件不为空，则执行该事件，不存在则什么也不做。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2, T3>).action?.Invoke(parameter0, parameter1, parameter2, parameter3);
    }
    #endregion

    #region 移除事件某个监听方法
    /// <summary>
    /// 移除无参数事件某个监听方法
    /// </summary>
    /// <param name="EventNameEnum">事件枚举</param>
    /// <param name="call">监听方法</param>
    public void RemoveEventListener(object EventNameEnum, UnityAction call)
    {
        string eventName = GetEnventName(EventNameEnum);
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo).action -= call;
        }
    }
    /// <summary>
    /// 移除带1个参数事件某个监听方法
    /// </summary>
    public void RemoveEventListener<T>(object EventNameEnum, UnityAction<T> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T).Name;
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T>).action -= call;
    }
    /// <summary>
    /// 移除带2个参数事件某个监听方法
    /// </summary>
    public void RemoveEventListener<T0, T1>(object EventNameEnum, UnityAction<T0, T1> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name;
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1>).action -= call;
    }
    /// <summary>
    /// 移除带3个参数事件某个监听方法
    /// </summary>
    public void RemoveEventListener<T0, T1, T2>(object EventNameEnum, UnityAction<T0, T1, T2> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name;
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2>).action -= call;
    }
    /// <summary>
    /// 移除带4个参数事件某个监听方法
    /// </summary>
    public void RemoveEventListener<T0, T1, T2, T3>(object EventNameEnum, UnityAction<T0, T1, T2, T3> call)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name + "_" + typeof(T3).Name;
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2, T3>).action -= call;
    }
    #endregion

    #region 移除整个事件
    /// <summary>
    /// 移除整个不带参数的事件
    /// </summary>
    /// <param name="EventNameEnum">事件枚举</param>
    public void RemoveEvent(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum);
        if (eventsDictionary.ContainsKey(eventName))
        {
            (eventsDictionary[eventName] as EventInfo).action = null;
        }
    }
    /// <summary>
    /// 移除整个带1个参数的事件
    /// </summary>
    public void RemoveEvent<T>(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T).Name;
        //如果字典中存在要移除的命令，则把这个命令的所有事件移除掉。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T>).action = null;
    }
    /// <summary>
    /// 移除整个带2个参数的事件
    /// </summary>
    public void RemoveEvent<T0, T1>(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name;
        //如果字典中存在要移除的命令，则把这个命令的所有事件移除掉。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1>).action = null;
    }
    /// <summary>
    /// 移除整个带3个参数的事件
    /// </summary>
    public void RemoveEvent<T0, T1, T2>(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name;
        //如果字典中存在要移除的命令，则把这个命令的所有事件移除掉。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2>).action = null;
    }
    /// <summary>
    /// 移除整个带4个参数的事件
    /// </summary>
    public void RemoveEvent<T0, T1, T2, T3>(object EventNameEnum)
    {
        string eventName = GetEnventName(EventNameEnum) + "_" + typeof(T0).Name + "_" + typeof(T1).Name + "_" + typeof(T2).Name + "_" + typeof(T3).Name;
        //如果字典中存在要移除的命令，则把这个命令的所有事件移除掉。
        if (eventsDictionary.ContainsKey(eventName))
            (eventsDictionary[eventName] as EventInfo<T0, T1, T2, T3>).action = null;
    }
    #endregion

    /// <summary>
    /// 移除事件中心的所有事件。可以考虑在切换场景时调用。
    /// </summary>
    public void RemoveAllEvent()
    {
        eventsDictionary.Clear();
    }

    #region 里氏替换原则
    private interface IEventInfo { }

    private class EventInfo : IEventInfo
    {
        public UnityAction action;

        public EventInfo(UnityAction call)
        {
            action += call;
        }
    }

    private class EventInfo<T> : IEventInfo
    {
        public UnityAction<T> action;

        public EventInfo(UnityAction<T> call)
        {
            action += call;
        }
    }

    private class EventInfo<T0, T1> : IEventInfo
    {
        public UnityAction<T0, T1> action;

        public EventInfo(UnityAction<T0, T1> call)
        {
            action += call;
        }
    }

    private class EventInfo<T0, T1, T2> : IEventInfo
    {
        public UnityAction<T0, T1, T2> action;

        public EventInfo(UnityAction<T0, T1, T2> call)
        {
            action += call;
        }
    }

    private class EventInfo<T0, T1, T2, T3> : IEventInfo
    {
        public UnityAction<T0, T1, T2, T3> action;

        public EventInfo(UnityAction<T0, T1, T2, T3> call)
        {
            action += call;
        }
    }
    #endregion
}
