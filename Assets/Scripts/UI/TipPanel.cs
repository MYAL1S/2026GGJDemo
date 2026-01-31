using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipPanel : BasePanel
{
    /// <summary>
    /// 淡入淡出速度
    /// </summary>
    private float alphaSpeed = 5f;

    /// <summary>
    /// 面板是否在显示
    /// </summary>
    private bool isPanelShowing = false;

    /// <summary>
    /// CanvasGroup 组件
    /// </summary>
    private CanvasGroup panelCanvasGroup;

    /// <summary>
    /// 是否正在淡出中
    /// </summary>
    private bool isFadingOut = false;

    public override void Init()
    {
        base.Init();
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public override void ShowMe()
    {
        // 不调用 base.ShowMe()，自己控制淡入
        isPanelShowing = true;
        isFadingOut = false;
        panelCanvasGroup.alpha = 0;

        // 暂停游戏
        Time.timeScale = 0f;
    }

    public override void HideMe()
    {
        // 不调用 base.HideMe()，自己控制淡出
        isPanelShowing = false;
        isFadingOut = true;
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                // 开始淡出，淡出完成后才恢复 TimeScale
                HideMe();
                break;
        }
    }

    /// <summary>
    /// 使用 unscaledDeltaTime 实现不受 TimeScale 影响的淡入淡出
    /// </summary>
    protected override void Update()
    {
        // 不调用 base.Update()，使用自己的淡入淡出逻辑

        if (isPanelShowing && panelCanvasGroup.alpha < 1)
        {
            // 淡入（使用 unscaledDeltaTime 不受 TimeScale 影响）
            panelCanvasGroup.alpha += alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha >= 1)
                panelCanvasGroup.alpha = 1;
        }
        else if (isFadingOut)
        {
            // 淡出（使用 unscaledDeltaTime 不受 TimeScale 影响）
            panelCanvasGroup.alpha -= alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha <= 0)
            {
                panelCanvasGroup.alpha = 0;
                isFadingOut = false;

                // 淡出完成后恢复 TimeScale 并隐藏面板
                Time.timeScale = 1f;
                UIMgr.Instance.HidePanel<TipPanel>();
            }
        }
    }
}
