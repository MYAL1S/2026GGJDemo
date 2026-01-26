using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 测试面板类 开始面板
/// </summary>
public class BeginPanel : BasePanel
{
    /// <summary>
    /// 事件监听的注册
    /// </summary>
    private void Start()
    {
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_LoadScene, LoadSceneEvent_BeginPanel);
    }

    /// <summary>
    /// 事件监听的移除
    /// </summary>
    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_LoadScene, LoadSceneEvent_BeginPanel);
    }

    public override void HideMe()
    {
        base.HideMe();
        print("隐藏BeginPanel");
    }

    public override void ShowMe()
    {
        base.ShowMe();
        print("显示BeginPanel");
    }

    /// <summary>
    /// 测试面板中按钮点击事件响应
    /// 
    /// </summary>
    /// <param name="name"></param>
    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnStart":
                print(name + "的事件响应");
                SceneMgr.Instance.LoadSceneAsync("GameScene", () =>
                {
                    print("场景加载完成 回调执行");
                });
                break;
            case "BtnEnd":
                print(name + "的事件响应");
                break;
        }
    }

    public void LoadSceneEvent_BeginPanel(float progress)
    {
        print($"场景加载进度：{progress}");
    }
}
