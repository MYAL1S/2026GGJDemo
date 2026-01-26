using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自动挂载式单例 MonoBehaviour基类
/// 继承该类的单例类 在第一次调用Instance属性时 会自动创建一个新的GameObject并挂载该单例脚本
/// 创建的GameObject名称为类的全名 并且不会在场景切换时被销毁
/// </summary>
/// <typeparam name="T">继承BaseSingleAutoMono的类</typeparam>
public class BaseSingleAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get 
        {
            //如果实例不存在 则创建一个新的GameObject并挂载该单例脚本
            if (instance == null)
            {
                GameObject go = new GameObject();
                go.name = typeof(T).ToString();
                instance = go.AddComponent<T>();
                DontDestroyOnLoad(go);
            }
            //返回实例
            return instance;
        }
    }
}
