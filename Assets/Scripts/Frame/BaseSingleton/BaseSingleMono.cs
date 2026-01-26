using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手动挂载式单例 MonoBehaviour基类
/// 继承该类的单例类 需要手动将脚本挂载到场景中的某个GameObject上
/// 同样不会在场景切换时被销毁
/// </summary>
/// <typeparam name="T">继承BaseSingleMono的类</typeparam>
public class BaseSingleMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        //如果已经存在实例 则销毁当前脚本
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        //否则将当前脚本设为实例
        instance = this as T;
        DontDestroyOnLoad(this.gameObject);
    }
}
