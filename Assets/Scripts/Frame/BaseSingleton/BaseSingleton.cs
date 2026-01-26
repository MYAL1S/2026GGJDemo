using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 不继承MonoBehaviour的单例基类
/// 主要用于管理类的单例
/// 由于不继承MonoBehaviour 所以需要手动调用Dispose方法进行资源释放
/// 并且需要保证类有一个私有的无参构造函数
/// </summary>
/// <typeparam name="T">继承BaseSingleton的管理类</typeparam>
public abstract class BaseSingleton<T> where T : class
{
    private static T instance;
    // 锁对象 用于线程安全
    protected static readonly object lockObj = new object();

    public static T Instance
    {
        get
        {
            //双重锁定检查 线程安全的单例模式
            if (instance == null)
            {
                lock (lockObj)
                {
                    //再次检查实例是否存在
                    //不存在则通过反射调用私有的无参构造函数创建实例
                    if (instance == null)
                    {
                        Type type = typeof(T);
                        ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                                            null,
                                            Type.EmptyTypes,
                                            null
                                            );
                        if (constructor != null)
                            instance = constructor.Invoke(null) as T;
                        else
                            Debug.LogError("没有得到对应的无参构造函数");
                    }
                }
            }
            return instance;
        }
    }
}
