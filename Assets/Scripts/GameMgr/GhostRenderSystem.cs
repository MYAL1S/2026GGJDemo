using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 鬼魂渲染系统 - 遮罩方案
/// </summary>
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
        if (!isInitialized) return;

        var list = PassengerMgr.Instance.passengerList;
        if (list == null) return;

        foreach (var p in list)
            p?.SetGhostFeatureVisible(true);

        Debug.Log("[GhostRenderSystem] 开始渲染");
    }

    public void StopGhostRendering()
    {
        var list = PassengerMgr.Instance.passengerList;
        if (list == null) return;

        foreach (var p in list)
            p?.SetGhostFeatureVisible(false);

        Debug.Log("[GhostRenderSystem] 停止渲染");
    }

    public void Cleanup()
    {
        isInitialized = false;
    }
}