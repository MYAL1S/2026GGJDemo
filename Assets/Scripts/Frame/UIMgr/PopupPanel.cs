using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 弹出面板基类 - 带遮罩，显示在角色上方
/// </summary>
public abstract class PopupPanel : BasePanel
{
    /// <summary>
    /// 面板排序层级
    /// </summary>
    protected virtual int PanelSortingOrder => 200;

    /// <summary>
    /// 遮罩排序层级
    /// </summary>
    protected virtual int MaskSortingOrder => 150;

    /// <summary>
    /// 遮罩颜色
    /// </summary>
    protected virtual Color MaskColor => new Color(0, 0, 0, 0.3f);

    /// <summary>
    /// 点击遮罩是否关闭面板
    /// </summary>
    protected virtual bool CloseOnMaskClick => true;

    private static bool isAnyPopupShowing = false;
    public static bool IsAnyPopupShowing => isAnyPopupShowing;

    public override void Init()
    {
        base.Init();
        SetupBlockingMask();
        SetupCanvasSorting();
        isAnyPopupShowing = true;
    }

    /// <summary>
    /// 设置 Canvas 排序
    /// </summary>
    protected virtual void SetupCanvasSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = PanelSortingOrder;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 设置阻挡遮罩
    /// </summary>
    protected virtual void SetupBlockingMask()
    {
        Transform existingMask = transform.Find("BlockingMask");
        if (existingMask != null)
            return;

        GameObject maskObj = new GameObject("BlockingMask");
        maskObj.transform.SetParent(transform, false);
        maskObj.transform.SetAsFirstSibling();

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = MaskSortingOrder;
        maskObj.AddComponent<GraphicRaycaster>();

        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = MaskColor;
        maskImage.raycastTarget = true;

        if (CloseOnMaskClick)
        {
            Button maskButton = maskObj.AddComponent<Button>();
            maskButton.transition = Selectable.Transition.None;
            maskButton.onClick.AddListener(OnMaskClicked);
        }
    }

    /// <summary>
    /// 点击遮罩时
    /// </summary>
    protected virtual void OnMaskClicked()
    {
        ClosePanel();
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    protected abstract void ClosePanel();

    public override void HideMe()
    {
        isAnyPopupShowing = false;
        base.HideMe();
    }

    private void OnDestroy()
    {
        isAnyPopupShowing = false;
    }
}