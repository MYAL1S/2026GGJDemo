using System;
using UnityEngine;

/// <summary>
/// 游戏管理器 - 负责初始化游戏系统
/// </summary>
[Obsolete("GameManager 已经弃用")]
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

        Debug.Log("[GameManager] 各个系统初始化完成");
    }
}