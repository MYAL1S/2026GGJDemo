using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public abstract class ResInfoBase
{
    //引用计数
    public int refCount = 0;
}

/// <summary>
/// 自定义资源信息类（泛型类）
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public class ResInfo<T> : ResInfoBase where T : UnityEngine.Object
{
    //资源
    public T asset;
    //主要用于异步加载结束后 传递资源到外部的委托
    public UnityAction<T> callback;
    //用于存储异步加载时 开启的协同程序
    public Coroutine coroutine;
    public bool isDel = false;

    /// <summary>
    /// 增加引用计数
    /// </summary>
    public void AddRefCount()
    {
        refCount++;
    }

    /// <summary>
    /// 减少引用计数
    /// </summary>
    public void SubRedCount()
    {
        refCount--;
        if (refCount < 0)
            Debug.LogError("请检查资源加载和卸载是否匹配");
    }
}

public class ResMgr : BaseSingleton<ResMgr>
{
    public Dictionary<string,ResInfoBase> resDic = new Dictionary<string, ResInfoBase>();

    private ResMgr() { }

    /// <summary>
    /// 同步加载资源的泛型方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        string key = path + "_" + typeof(T).Name;
        ResInfo<T> info;
        //检查字典中是否存在该资源信息
        //如果不存在 则同步加载资源并记录进字典
        if (!resDic.ContainsKey(key))
        {
            info = new ResInfo<T>();
            info.AddRefCount();
            info.asset = Resources.Load<T>(path);
            resDic.Add(key, info);
            //返回给调用者加载的资源
            return info.asset;
        }
        //如果存在 则需要考虑资源是否已经加载完成
        else
        {
            info = resDic[key] as ResInfo<T>;
            info.AddRefCount();
            //如果资源未加载完成 则需要停止协程 同步加载资源 并调用回调函数
            if (info.asset == null)
            {
                MonoMgr.Instance.StopCoroutine(info.coroutine);
                info.asset = Resources.Load<T>(path);
                info.callback?.Invoke(info.asset);
                info.callback = null;
                info.coroutine = null;
                return info.asset;
            }
            //如果资源已经加载完成 直接返回资源
            else
            {
                return info.asset;
            }
        }
    }

    /// <summary>
    /// 异步加载资源的泛型方法
    /// </summary>
    /// <typeparam name="T">加载的资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <param name="callback">资源加载完成后调用的回调函数(传出加载完成的资源并使用)</param>
    public void LoadAsync<T>(string path,UnityAction<T> callback) where T : UnityEngine.Object
    {
        //// 开启协程，真正进行异步加载
        //MonoMgr.Instance.StartCoroutine(ReallyLoadAsync<T>(path, callback));
        string key = path + "_" + typeof(T).Name;
        ResInfo<T> resInfo;
        if (!resDic.ContainsKey(key))
        {
            resInfo = new ResInfo<T>();
            resDic.Add(key, resInfo);
            resInfo.AddRefCount();
            resInfo.callback += callback;
            resInfo.coroutine = MonoMgr.Instance.StartCoroutine(ReallyLoadAsync<T>(path));
        }
        else
        {
            resInfo = resDic[key] as ResInfo<T>;
            resInfo.AddRefCount();
            if (resInfo.asset == null)
                resInfo.callback += callback;
            else
                //资源已经加载完成，直接调用回调函数
                callback?.Invoke(resInfo.asset);
        }
    }

    /// <summary>
    /// 实际进行异步加载资源的协程方法(泛型)
    /// </summary>
    /// <typeparam name="T">加载的资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns></returns>
    private IEnumerator ReallyLoadAsync<T>(string path) where T : UnityEngine.Object
    {
        //异步加载资源
        ResourceRequest rq = Resources.LoadAsync<T>(path);
        yield return rq;

        //加载完成后，再次确认字典中是否存在该资源信息
        string key = path + "_" + typeof(T).Name;
        if (resDic.ContainsKey(key))
        {
            //将加载到的资源赋值给资源信息类，并调用回调函数
            ResInfo<T> resInfo = resDic[key] as ResInfo<T>;
            resInfo.asset = rq.asset as T;

            //根据引用计数决定是否调用回调函数还是卸载资源
            if (resInfo.refCount == 0)
                UnloadAeest<T>(path,null,resInfo.isDel,false);
            else
            {
                resInfo.callback?.Invoke(resInfo.asset);
                //清空资源信息类中的回调函数和协程引用
                //避免内存泄漏
                resInfo.callback = null;
                resInfo.coroutine = null;
            }
        }
    }

    /// <summary>
    /// 异步加载资源的方法（非泛型）
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="type">资源类型</param>
    /// <param name="callback">资源加载完成后调用的回调函数(传出加载完成的资源并使用)</param>
    [Obsolete("该方法已被弃用，如果一定要使用，那么一定不能和泛型异步加载方法混用来加载同一资源")]
    public void LoadAsync(string path, Type type, UnityAction<UnityEngine.Object> callback)
    {
        ////通过协程去异步加载资源
        //MonoMgr.Instance.StartCoroutine(ReallyLoadaAsync(path, type, callback));
        string key = path + "_" + type.Name;
        ResInfo<UnityEngine.Object> resInfo;
        if (!resDic.ContainsKey(key))
        {
            resInfo = new ResInfo<UnityEngine.Object>();
            resDic.Add(key, resInfo);
            resInfo.AddRefCount();
            resInfo.callback += callback;
            resInfo.coroutine = MonoMgr.Instance.StartCoroutine(ReallyLoadaAsync(path,type));
        }
        else
        {
            resInfo = resDic[key] as ResInfo<UnityEngine.Object>;
            resInfo.AddRefCount();
            if (resInfo.asset == null)
                resInfo.callback += callback;
            else         
                //资源已经加载完成，直接调用回调函数
                callback?.Invoke(resInfo.asset);
        }
    }

    /// <summary>
    /// 实际进行异步加载资源的协程方法（非泛型）
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="type">资源类型</param>
    /// <returns></returns>
    private IEnumerator ReallyLoadaAsync(string path, Type type)
    {
        ResourceRequest rq = Resources.LoadAsync(path,type);
        yield return rq;

        string key = path + "_" + type.Name;
        if (resDic.ContainsKey(key))
        {
            ResInfo<UnityEngine.Object> resInfo = resDic[key] as ResInfo<UnityEngine.Object>;
            resInfo.asset = rq.asset;

            //根据引用计数决定是否调用回调函数还是卸载资源
            //如果引用计数为0 则卸载资源
            if (resInfo.refCount == 0)
                UnloadAeest(path, type, null, resInfo.isDel, false);
            //否则调用回调函数
            else
            {
                resInfo.callback?.Invoke(resInfo.asset);

                resInfo.callback = null;
                resInfo.coroutine = null;
            }
        }
    }


    /// <summary>
    /// 同步卸载资源的方法（泛型）
    /// </summary>
    /// <typeparam name="T">需要卸载的资源类型</typeparam>
    /// <param name="name">需要卸载的资源名</param>
    /// <param name="callback">需要卸载的回调函数</param>
    /// <param name="isSub">是否减少引用计数(默认为true,在异步加载资源时内部设置为false，避免引用计数为负数)</param>
    /// <param name="isDel">是否立刻删除资源</param>
    public void UnloadAeest<T>(string name,UnityAction<T> callback, bool isDel = false, bool isSub = true) where T : UnityEngine.Object
    {
        string key = name + "_" + typeof(T).Name;
        ResInfo<T> info;
        if (resDic.ContainsKey(key))
        {
            info = resDic[key] as ResInfo<T>;
            //减少引用计数
            if (isSub)
                info.SubRedCount();
            //设置资源的待删除状态
            info.isDel = isDel;
            //如果资源已经加载完成 且引用计数为0 则真正卸载资源
            if (info.asset != null && info.refCount == 0 && isDel)
            {
                resDic.Remove(key);
                Resources.UnloadAsset(info.asset);
            }
            //如果资源未加载完成 则标记该资源为待删除状态
            else if (info.asset == null)
            {
                //从回调函数中移除该卸载请求的回调
                if (callback != null)
                    info.callback -= callback;
            }
        }
    }

    /// <summary>
    /// 同步卸载资源的方法（泛型）
    /// </summary>
    /// <typeparam name="T">需要卸载的资源类型</typeparam>
    /// <param name="name">需要卸载的资源名</param>
    /// <param name="callback">需要卸载的回调函数</param>
    /// <param name="isSub">是否减少引用计数(默认为true,在异步加载资源时内部设置为false，避免引用计数为负数)</param>
    /// <param name="isDel">是否立刻删除资源</param>
    public void UnloadAeest(string name,Type type, UnityAction<UnityEngine.Object> callback, bool isDel = false, bool isSub = true)
    {
        string key = name + "_" + type.Name;
        ResInfo<UnityEngine.Object> info;
        if (resDic.ContainsKey(key))
        {
            info = resDic[key] as ResInfo<UnityEngine.Object>;
            //减少引用计数
            if (isSub)
                info.SubRedCount();
            //设置资源的待删除状态
            info.isDel = isDel;
            //如果资源已经加载完成 且引用计数为0 则真正卸载资源
            if (info.asset != null && info.refCount == 0 && isDel)
            {
                resDic.Remove(key);
                Resources.UnloadAsset(info.asset);
            }
            //如果资源未加载完成 则标记该资源为待删除状态
            else if (info.asset == null)
            {
                //从回调函数中移除该卸载请求的回调
                if (callback != null)
                    info.callback -= callback;
            }
        }
    }

    /// <summary>
    /// 异步卸载未使用资源的方法
    /// </summary>
    /// <param name="callback">资源卸载完成后调用的回调函数</param>
    public void UnloadUnusedAeests(UnityAction callback)
    {
        MonoMgr.Instance.StartCoroutine(ReallyUnloadUnusedAssets(callback));
    }

    /// <summary>
    /// 实际进行异步卸载未使用资源的协程方法
    /// </summary>
    /// <param name="callback">资源卸载完成后调用的回调函数</param>
    /// <returns></returns>
    private IEnumerator ReallyUnloadUnusedAssets(UnityAction callback)
    {
        List<string> list = new List<string>();
        //遍历字典，找出引用计数为0的资源，加入待删除列表
        foreach (var key in resDic.Keys)
        {
            if (resDic[key].refCount == 0)
            {
                list.Add(key);
            }
        }
        //遍历待删除列表，从字典中移除记录
        foreach (var item in list)
        {
            resDic.Remove(item);
        }
        //遍历待删除列表，真正卸载资源
        AsyncOperation asyncOperation = Resources.UnloadUnusedAssets();
        yield return asyncOperation;
        callback?.Invoke();
    }

    /// <summary>
    /// 得到字典中某资源的引用计数
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns></returns>
    public int GetRefCount<T>(string path)
    {
        string key = path + "_" + typeof(T).Name;
        if (resDic.ContainsKey(key))
        {
            return resDic[key].refCount;
        }
        return 0;
    }

    /// <summary>
    /// 清除字典中的所有资源记录
    /// </summary>
    /// <param name="callback">卸载完毕后调用的回调函数</param>
    public void ClearDic(UnityAction callback)
    {
        MonoMgr.Instance.StartCoroutine(ReallyClearDic(callback));
    }

    /// <summary>
    /// 实际清除字典中的所有资源记录的协程方法
    /// </summary>
    /// <param name="callback">卸载完毕后调用的回调函数</param>
    /// <returns></returns>
    public IEnumerator ReallyClearDic(UnityAction callback)
    {
        resDic.Clear();
        AsyncOperation ao = Resources.UnloadUnusedAssets();
        yield return ao;
        //卸载完毕后调用回调函数
        callback?.Invoke();
    }
}
