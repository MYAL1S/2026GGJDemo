using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipPanel : BasePanel
{
    private float alphaSpeed = 5f;
    private bool isPanelShowing = false;
    private CanvasGroup panelCanvasGroup;
    private bool isFadingOut = false;

    /// <summary>
    /// TipPanel 排序层级（必须高于所有游戏UI）
    /// </summary>
    private const int PANEL_SORTING_ORDER = 300;
    private const int MASK_SORTING_ORDER = 250;

    public override void Init()
    {
        base.Init();
        
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetupCanvasSorting();
        SetupBlockingMask();
    }

    /// <summary>
    /// 设置 Canvas 排序层级
    /// </summary>
    private void SetupCanvasSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = PANEL_SORTING_ORDER;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 设置全屏遮罩，阻挡底层UI交互
    /// </summary>
    private void SetupBlockingMask()
    {
        Transform existingMask = transform.Find("BlockingMask");
        if (existingMask != null)
        {
            Canvas existingCanvas = existingMask.GetComponent<Canvas>();
            if (existingCanvas == null)
            {
                existingCanvas = existingMask.gameObject.AddComponent<Canvas>();
                existingCanvas.overrideSorting = true;
                existingCanvas.sortingOrder = MASK_SORTING_ORDER;
                existingMask.gameObject.AddComponent<GraphicRaycaster>();
            }
            return;
        }

        // 创建全屏遮罩
        GameObject maskObj = new GameObject("BlockingMask");
        maskObj.transform.SetParent(transform, false);
        maskObj.transform.SetAsFirstSibling();

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        // 设置遮罩 Canvas
        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = MASK_SORTING_ORDER;
        maskObj.AddComponent<GraphicRaycaster>();

        // 半透明黑色遮罩
        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = new Color(0, 0, 0, 0.5f);
        maskImage.raycastTarget = true;  // ? 阻挡底层点击
    }

    public override void ShowMe()
    {
        isPanelShowing = true;
        isFadingOut = false;
        panelCanvasGroup.alpha = 0;
        Time.timeScale = 0f;
    }

    public override void HideMe()
    {
        isPanelShowing = false;
        isFadingOut = true;
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                HideMe();
                break;
        }
    }

    protected override void Update()
    {
        // 淡入
        if (isPanelShowing && panelCanvasGroup.alpha < 1)
        {
            panelCanvasGroup.alpha += alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha >= 1)
                panelCanvasGroup.alpha = 1;
        }
        // 淡出
        else if (isFadingOut && panelCanvasGroup.alpha > 0)
        {
            panelCanvasGroup.alpha -= alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha <= 0)
            {
                panelCanvasGroup.alpha = 0;
                isFadingOut = false;
                Time.timeScale = 1f;
                UIMgr.Instance.HidePanel<TipPanel>();
            }
        }
    }

    /// <summary>
    /// 播放面板出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        MusicMgr.Instance.PlaySound("Music/26GGJsound/window_appear", false);
    }
}
