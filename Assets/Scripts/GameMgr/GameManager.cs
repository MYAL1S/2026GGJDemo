using UnityEngine;

/// <summary>
/// 游戏管理器 - 负责初始化所有系统
/// </summary>
public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // 初始化各个系统
        InitializeSystems();
    }

    private void InitializeSystems()
    {
        // 初始化鬼魂渲染系统
        GhostRenderSystem.Instance.Setup();

        Debug.Log("[GameManager] 所有系统初始化完成");
    }
}