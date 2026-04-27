using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 乘客交互面板
/// </summary>
[Obsolete("PassengerPanel 已废弃，请使用新的乘客交互系统")]
public class PassengerPanel : BasePanel
{
    private Transform expelObj;
    private Passenger selectedPassenger;

    private static bool isShowing = false;
    public static bool IsShowing => isShowing;

    /// <summary>
    /// 面板的 Canvas 排序层级（必须高于乘客的最大值）
    /// 乘客最大约为 5 + 50 = 55，设置为 200 确保在上层
    /// </summary>
    private const int PANEL_SORTING_ORDER = 200;

    /// <summary>
    /// 遮罩的排序层级
    /// </summary>
    private const int MASK_SORTING_ORDER = 150;

    public override void Init()
    {
        base.Init();

        var expelRawImage = GetControl<RawImage>("Expel");
        if (expelRawImage != null)
            expelObj = expelRawImage.transform;

        // 添加背景遮罩用于阻止点击穿透（先添加遮罩）
        SetupBlockingMask();

        // 设置面板层级高于乘客（遮罩之后设置面板）
        SetupCanvasSorting();

        ShowExpelUI();
        isShowing = true;
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

        // 确保有 GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 设置阻挡遮罩（防止点击穿透到乘客）
    /// </summary>
    private void SetupBlockingMask()
    {
        // 检查是否已有遮罩
        Transform existingMask = transform.Find("BlockingMask");
        if (existingMask != null)
        {
            // 确保遮罩有正确的 Canvas 排序
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

        // 创建全屏透明遮罩
        GameObject maskObj = new GameObject("BlockingMask");
        maskObj.transform.SetParent(transform, false);
        maskObj.transform.SetAsFirstSibling();

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        // 为遮罩添加独立 Canvas 确保层级正确
        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = MASK_SORTING_ORDER;
        maskObj.AddComponent<GraphicRaycaster>();

        // 添加透明 Image 用于接收点击
        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = new Color(0, 0, 0, 0.3f);
        maskImage.raycastTarget = true;

        // 添加按钮用于点击遮罩关闭面板
        Button maskButton = maskObj.AddComponent<Button>();
        maskButton.transition = Selectable.Transition.None;
        maskButton.onClick.AddListener(OnMaskClicked);
    }

    /// <summary>
    /// 点击遮罩时关闭面板
    /// </summary>
    private void OnMaskClicked()
    {
        CancelSelection();
    }

    /// <summary>
    /// 取消选择并关闭面板
    /// </summary>
    private void CancelSelection()
    {
        if (selectedPassenger != null)
        {
            selectedPassenger.SetHighlight(false);
            selectedPassenger = null;
        }
        UIMgr.Instance.HidePanel<PassengerPanel>(true);
    }

    public void SetSelectedPassenger(Passenger passenger)
    {
        if (selectedPassenger != null && selectedPassenger != passenger)
            selectedPassenger.SetHighlight(false);

        selectedPassenger = passenger;
    }

    public void ShowExpelUI()
    {
        if (expelObj != null)
            expelObj.gameObject.SetActive(true);
    }

    public void HideExpelUI()
    {
        if (expelObj != null)
            expelObj.gameObject.SetActive(false);
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnExpel":
                if (selectedPassenger != null)
                {
                    selectedPassenger.SetHighlight(false);
                    //PassengerMgr.Instance.OnPassengerKicked(selectedPassenger);
                    selectedPassenger = null;
                }
                UIMgr.Instance.HidePanel<PassengerPanel>(true);
                break;
            case "BtnCancel":
                CancelSelection();
                break;
        }
    }

    public override void HideMe()
    {
        if (selectedPassenger != null)
        {
            selectedPassenger.SetHighlight(false);
            selectedPassenger = null;
        }
        isShowing = false;
        base.HideMe();
    }

    private void OnDestroy()
    {
        isShowing = false;
    }
}
