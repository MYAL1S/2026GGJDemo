using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景管理器
/// </summary>
public class SceneMgr : BaseSingleton<SceneMgr>
{
    private SceneMgr() { }

    /// <summary>
    /// 同步加载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="callback">回调函数</param>
    public void LoadScene(string sceneName, UnityAction callback = null)
    {
        SceneManager.LoadScene(sceneName);
        callback?.Invoke();
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="callback">回调函数</param>
    public void LoadSceneAsync(string sceneName, UnityAction callback = null)
    {
        MonoMgr.Instance.StartCoroutine(ReallyLoadSceneAsync(sceneName, callback));
    }

    /// <summary>
    /// 实际异步加载场景的协程
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="callback">回调函数</param>
    /// <returns></returns>
    private IEnumerator ReallyLoadSceneAsync(string sceneName,UnityAction callback = null)
    {
        // 开始异步加载场景
        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
        // 当场景加载未完成时，持续触发进度事件
        while (!ao.isDone)
        {
            //通过事件中心，触发加载进度事件，并传递当前进度值
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_LoadScene,ao.progress);
            yield return 0;
        }
        // 场景加载完成后，触发加载进度为1的事件 避免最后一帧没有触发
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_LoadScene, 1f);
        // 调用回调函数
        callback?.Invoke();
    }
}
