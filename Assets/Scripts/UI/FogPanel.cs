using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FogPanel : BasePanel
{
    /// <summary>
    /// FogPanel 排序层级（必须高于乘客的 90）
    /// </summary>
    private const int FOG_SORTING_ORDER = 100;

    public override void Init()
    {
        base.Init();
        SetupCanvasSorting();
    }

    /// <summary>
    /// 设置 Canvas 排序层级，确保遮挡乘客
    /// </summary>
    private void SetupCanvasSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = FOG_SORTING_ORDER;

        // 确保有 GraphicRaycaster（如果需要交互）
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    public override void ShowMe()
    {
        base.ShowMe();
    }

    public override void HideMe()
    {
        base.HideMe();
    }
}
