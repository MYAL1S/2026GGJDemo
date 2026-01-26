using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 缓存池中的数据结构
/// </summary>
public class PoolData
{
    /// <summary>
    /// 缓存池对象栈(抽屉)
    /// 存放未被使用的对象
    /// </summary>
    private Stack<GameObject> dataStack = new Stack<GameObject>();
    /// <summary>
    /// 缓存池对象的根节点(抽屉)
    /// 所有该缓存池对象的对象都存放在该节点下
    /// </summary>
    private GameObject rootObj;
    /// <summary>
    /// 缓存池中被使用的对象列表
    /// </summary>
    private List<GameObject> usedList = new List<GameObject>();
    /// <summary>
    /// 缓存池对象栈(抽屉)中未使用的对象数量
    /// </summary>
    public int Count => dataStack.Count;
    /// <summary>
    /// 缓存池中被使用的对象数量
    /// </summary>
    public int UsedCount => usedList.Count;
    ///缓存池对象的上限数量
    private int maxNum;
    /// <summary>
    /// 是否需要创建新对象
    /// </summary>
    public bool NeedCreate => usedList.Count < maxNum;

    /// <summary>
    /// 缓存池对象数据结构构造函数
    /// </summary>
    /// <param name="name">缓存池数据对象的名字(根节点名)</param>
    /// <param name="root">缓存池对象</param>
    /// <param name="usedGameobject">被使用的数据对象</param>
    public PoolData(string name,GameObject root,GameObject usedGameobject)
    {
        //创建缓存池对象的根节点(抽屉)
        rootObj = new GameObject(name);
        //将缓存池对象的根节点(抽屉)的父对象设置为缓存池(柜子)的根节点
        rootObj.transform.parent = root.transform;
        //将被使用的数据对象添加到使用中的对象列表中
        PushUsedList(usedGameobject);

        PoolObj poolObj = usedGameobject.GetComponent<PoolObj>();
        if (poolObj == null)
            Debug.LogError("请为使用缓存池功能的预设体对象挂载PoolObj脚本 用于设置数量上限");
        else
            //得到对象的上限数量
            maxNum = poolObj.maxNum;
    }

    /// <summary>
    /// 将对象压入缓存池对象栈(抽屉)中
    /// </summary>
    /// <param name="obj"></param>
    public void PushObject(GameObject obj)
    {
        //失活对象
        obj.SetActive(false);
        //将对象的父节点设置为缓存池对象的根节点(抽屉)
        obj.transform.parent = rootObj.transform;
        //将对象压入缓存池对象栈(抽屉)中
        dataStack.Push(obj);
    }

    /// <summary>
    /// 将对象从缓存池对象栈(抽屉)中弹出
    /// </summary>
    /// <returns></returns>
    public GameObject GetObject()
    {
        //将对象从缓存池对象栈(抽屉)中弹出
        GameObject obj = dataStack.Pop();
        //激活对象
        obj.SetActive(true);

        //将对象的父节点设置为null
        obj.transform.parent = null;

        return obj;
    }

    /// <summary>
    /// 将使用中的对象添加到使用中的对象列表中
    /// </summary>
    /// <param name="obj"></param>
    public void PushUsedList(GameObject obj)
    {
        usedList.Add(obj);
    }

    /// <summary>
    /// 将对象从缓存池对象栈(抽屉)中弹出
    /// </summary>
    /// <returns></returns>
    public GameObject Pop()
    {
        GameObject obj;
        //如果缓存池对象栈(抽屉)中有对象，则从栈中弹出对象
        if (Count > 0)
        {
            obj = dataStack.Pop();
        }
        //否则从使用中的对象列表中取出第一个对象
        else
        {
            obj = usedList[0];
            usedList.RemoveAt(0);
        }

        obj.SetActive(true);
        obj.transform.SetParent(null);
        usedList.Add(obj);

        return obj;
    }

    /// <summary>
    /// 将对象放回缓存池对象栈(抽屉)中
    /// </summary>
    /// <param name="obj"></param>
    public void Push(GameObject obj)
    {
        //失活对象
        obj.SetActive(false);
        //将对象从使用中的对象列表中移除
        usedList.Remove(obj);
        //将对象压入缓存池对象栈(抽屉)中
        dataStack.Push(obj);
        //将对象的父节点设置为缓存池对象的根节点(抽屉)
        obj.transform.SetParent(rootObj.transform);
    }
}

public class BasePoolObject{}

/// <summary>
/// 缓存池中的数据结构类对象(并未挂载到对象上,仅被引用)
/// </summary>
/// <typeparam name="T">数据结构类型</typeparam>
public class PoolDataReference<T>: BasePoolObject where T : class,IPoolObject,new()
{
    public Queue<T> poolObjs = new Queue<T>();
}

/// <summary>
/// 继承该接口的类对象 可以被缓存池管理器回收复用
/// </summary>
public interface IPoolObject
{
    void Reset();
}

/// <summary>
/// 缓存池管理器
/// </summary>
public class PoolMgr : BaseSingleton<PoolMgr>
{
    /// <summary>
    /// 存储缓存池对象的数据结构字典(场景上的对象)
    /// </summary>
    private Dictionary<string,PoolData> poolDic = new Dictionary<string,PoolData>();
    /// <summary>
    /// 存储缓存池对象的数据结构字典(不挂载到场景上的类对象)
    /// </summary>
    private Dictionary<string, BasePoolObject> referencePoolDic = new Dictionary<string, BasePoolObject>();
    /// <summary>
    /// 对象池的根节点(柜子)
    /// </summary>
    private GameObject pool;
    private PoolMgr()
    {
        pool = new GameObject("Pool");
    }

    /// <summary>
    /// 从对象池中取出对象
    /// </summary>
    /// <param name="name">要取出的对象名</param>
    /// <returns></returns>
    public GameObject GetObj(string name)
    {
        GameObject obj;

        #region 未添加对象上限
        ////如果对象池中有该对象的缓存(抽屉)，则直接取出
        //if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        //{
        //    obj = poolDic[name].GetObject();
        //}
        ////否则实例化一个新的对象
        //else
        //{
        //    obj = GameObject.Instantiate(Resources.Load<GameObject>(name));
        //    obj.name = name;
        //}
        //return obj;
        #endregion
        //如果缓存池对象的根节点(柜子)不存在，则创建一个新的缓存池对象的根节点(柜子)
        //如果对象池中没有该对象的缓存(抽屉)，或者该对象的缓存(抽屉)中没有对象且使用中的对象数量未达到上限
        if (!poolDic.ContainsKey(name) || poolDic[name].Count == 0 && poolDic[name].NeedCreate)
        {
            //实例化一个新的对象
            obj = GameObject.Instantiate(Resources.Load<GameObject>(name));
            obj.name = name;

            //如果对象池中没有该对象的缓存(抽屉)，则创建一个新的缓存(抽屉)
            if (!poolDic.ContainsKey(name))
                poolDic.Add(name, new PoolData(name, pool, obj));
            else
                //将被使用的数据对象添加到使用中的对象列表中
                poolDic[name].PushUsedList(obj);
        }
        //如果对象池中有该对象的缓存(抽屉)，且该对象的缓存(抽屉)中有对象或者被使用中的对象数量已达到上限
        else
        {
            obj = poolDic[name].Pop();
        }

        return obj;
    }


    /// <summary>
    /// 将对象放回对象池
    /// </summary>
    /// <param name="obj">放回对象池的对象</param>
    public void PushObj(GameObject obj)
    {
        if (pool == null)
            pool = new GameObject("Pool");

        poolDic[obj.name].Push(obj);

        #region 未加入对象上限时的逻辑
        ////如果对象池中没有该对象的缓存(抽屉)，则创建一个新的缓存(抽屉)
        //if (!poolDic.ContainsKey(obj.name))
        //    poolDic.Add(obj.name,new PoolData(obj.name,pool));

        ////将对象放回对象池
        //poolDic[obj.name].PushObject(obj);
        #endregion

    }



    /// <summary>
    /// 获取不挂载到场景上的类对象
    /// </summary>
    /// <typeparam name="T">类对象类型</typeparam>
    /// <param name="nameSpace">命名空间</param>
    /// <returns></returns>
    public T GetObj<T>(string nameSpace = null) where T : class,IPoolObject,new()
    {
        string name = nameSpace + "_" + typeof(T).Name;
        T obj;
        //如果对象池中有该对象的缓存
        if (referencePoolDic.ContainsKey(name))
        {
            if ((referencePoolDic[name] as PoolDataReference<T>).poolObjs.Count > 0)
                obj = (referencePoolDic[name] as PoolDataReference<T>).poolObjs.Dequeue();
            else
                obj = new T();
        }
        //如果对象池中没有该对象的缓存
        else
            obj = new T();

        return obj;
    }

    /// <summary>
    /// 将不挂载到场景对象上的类对象放回对象池
    /// </summary>
    /// <typeparam name="T">类对象类型</typeparam>
    /// <param name="obj">类对象</param>
    /// <param name="nameSpace">命名空间</param>
    public void PushObj<T>(T obj, string nameSpace = null) where T : class,IPoolObject,new()
    {
        //如果传入的对象为空 则直接返回
        if (obj == null)
            return;
        string name = nameSpace + "_" + typeof(T).Name;
        PoolDataReference<T> poolDataRef;
        //如果对象池中没有该对象的缓存，则创建一个新的缓存
        if (!referencePoolDic.ContainsKey(name))
        {
            poolDataRef = new PoolDataReference<T>();
            referencePoolDic.Add(name, poolDataRef);
        }
        //将对象放回对象池
        else
            poolDataRef = referencePoolDic[name] as PoolDataReference<T>;
        obj.Reset();
        poolDataRef.poolObjs.Enqueue(obj);
    }

    /// <summary>
    /// 清空缓存池
    /// </summary>
    public void ClearPool()
    {
        poolDic.Clear();
        referencePoolDic.Clear();
        pool = null;
    }
}
