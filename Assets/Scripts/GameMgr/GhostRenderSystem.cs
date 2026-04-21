using System;
using UnityEngine;

/// <summary>
/// 鬼魂渲染系统（简化版 - 透视逻辑已移至 PhoneItem）
/// </summary>
[Obsolete("GhostRenderSystem is obsolete. Use Main instead.")]
public class GhostRenderSystem : BaseSingleton<GhostRenderSystem>
{
    private bool isInitialized = false;

    private GhostRenderSystem() { }

    public void Setup()
    {
        isInitialized = true;
        Debug.Log("[GhostRenderSystem] Setup 完成");
    }

    public void StartGhostRendering()
    {
        if (!isInitialized) Setup();
        Debug.Log("[GhostRenderSystem] 透视模式开启");
    }

    public void StopGhostRendering()
    {
        Debug.Log("[GhostRenderSystem] 透视模式关闭");
    }

    public void Cleanup()
    {
        isInitialized = false;
    }
}