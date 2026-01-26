using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 公共Mono管理器 用于提供Update FixedUpdate LateUpdate等函数的监听接口
/// 外部没有继承monobehaviour的类 可以通过这个类来注册监听函数
/// 也可以通过公共mono管理器的协程接口来开启协程
/// </summary>
public class MonoMgr : BaseSingleAutoMono<MonoMgr>
{
    private UnityAction MyFixedUpdateEvent;
    private UnityAction MyUpdateEvent;
    private UnityAction MyLateUpdateEvent;

    /// <summary>
    /// 提供给外部添加FixedUpdate监听函数的接口
    /// </summary>
    /// <param name="fixedUpdateFun"></param>
    public void AddFixedUpdateListener(UnityAction fixedUpdateFun)
    {
        MyFixedUpdateEvent += fixedUpdateFun;
    }

    /// <summary>
    /// 提供给外部移除FixedUpdate监听函数的接口
    /// </summary>
    /// <param name="fixedUpdateFun"></param>
    public void RemoveFixedUpdateListener(UnityAction fixedUpdateFun)
    {
        MyFixedUpdateEvent -= fixedUpdateFun;
    }

    /// <summary>
    /// 提供给外部添加LateUpdate监听函数的接口
    /// </summary>
    /// <param name="updateFun"></param>
    public void AddUpdateListener(UnityAction updateFun)
    {
        MyUpdateEvent += updateFun;
    }

    /// <summary>
    /// 提供给外部移除LateUpdate监听函数的接口
    /// </summary>
    /// <param name="updateFun"></param>
    public void RemoveUpdateListener(UnityAction updateFun)
    {
        MyUpdateEvent -= updateFun;
    }

    /// <summary>
    /// 提供给外部添加LateUpdate监听函数的接口
    /// </summary>
    /// <param name="lateUpdateFun"></param>
    public void AddLateUpdateListener(UnityAction lateUpdateFun)
    {
        MyLateUpdateEvent += lateUpdateFun;
    }

    /// <summary>
    /// 提供给外部移除LateUpdate监听函数的接口
    /// </summary>
    /// <param name="lateUpdateFun"></param>
    public void RemoveLateUpdateListener(UnityAction lateUpdateFun)
    {
        MyLateUpdateEvent -= lateUpdateFun;
    }

    /// <summary>
    /// 在FixedUpdate中调用外部注册的函数
    /// </summary>
    private void FixedUpdate()
    {
        MyFixedUpdateEvent?.Invoke();
    }

    /// <summary>
    /// 在Update中调用外部注册的函数
    /// </summary>
    private void Update()
    {
        MyUpdateEvent?.Invoke();
    }

    /// <summary>
    /// 在LateUpdate中调用外部注册的函数
    /// </summary>
    private void LateUpdate()
    {
        MyLateUpdateEvent?.Invoke();
    }
}
