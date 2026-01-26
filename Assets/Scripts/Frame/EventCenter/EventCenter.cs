using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 事件信息基类 用于 里式替换原则 装载 子类的父类
/// </summary>
public abstract class EventInfoBase { }

/// <summary>
/// 事件信息类 用来包裹 对应订阅者 函数委托的 类
/// 用于传递有参事件
/// </summary>
/// <typeparam name="T"></typeparam>
public class EventInfo<T> : EventInfoBase
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

/// <summary>
/// 事件信息类 主要用来记录无参无返回值委托
/// </summary>
public class EventInfo : EventInfoBase
{
    public UnityAction actions;
    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

/// <summary>
/// 事件中心模块
/// </summary>
public class EventCenter : BaseSingleton<EventCenter>
{
    /// <summary>
    /// 事件字典 用于存储 事件类型枚举 和 事件信息类 的键值对
    /// </summary>
    public Dictionary<E_EventType,EventInfoBase> eventDic = new Dictionary<E_EventType,EventInfoBase>();
    private EventCenter() { }

    /// <summary>
    /// 发布者调用 触发事件
    /// </summary>
    /// <param name="eventName">事件名</param>
    public void EventTrigger<T>(E_EventType eventName,T obj)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T>).actions?.Invoke(obj);
    }

    /// <summary>
    /// 发布者调用 触发无参事件
    /// </summary>
    /// <param name="eventName"></param>
    public void EventTrigger(E_EventType eventName)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo).actions?.Invoke();
    }

    /// <summary>
    /// 订阅者调用 添加有参事件监听者
    /// </summary>
    /// <param name="eventName">监听的事件名</param>
    /// <param name="func">传入的回调函数</param>
    public void AddEventListener<T>(E_EventType eventName, UnityAction<T> func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T>).actions += func;
        else
            eventDic.Add(eventName, new EventInfo<T>(func)); 
    }

    /// <summary>
    /// 订阅者调用 添加无参事件监听者
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="func"></param>
    public void AddEventListener(E_EventType eventName, UnityAction func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo).actions += func;
        else
            eventDic.Add(eventName, new EventInfo(func));
    }

    /// <summary>
    /// 订阅者调用 移除有参事件监听者
    /// </summary>
    /// <param name="eventName">监听的事件名</param>
    /// <param name="func">移除的函数</param>
    public void RemoveEventListener<T>(E_EventType eventName,UnityAction<T> func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T>).actions -= func;
    }

    /// <summary>
    /// 订阅者调用 移除无参事件监听者
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="func"></param>
    public void RemoveEventListener(E_EventType eventName, UnityAction func)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo).actions -= func;
    }

    /// <summary>
    /// 清空所有事件的监听
    /// </summary>
    public void Clear()
    {
        eventDic.Clear();
    }

    /// <summary>
    /// 清空指定事件的监听
    /// </summary>
    /// <param name="eventName"></param>
    public void Clear(E_EventType eventName)
    {
        if (eventDic.ContainsKey(eventName))
            eventDic.Remove(eventName);
    }
}
