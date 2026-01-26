using UnityEngine;

/// <summary>
/// 后处理效果接口
/// 所有后处理效果必须实现此接口才能被 UIMgr 管理
/// </summary>
public interface IPostProcessEffect
{
    /// <summary>
    /// 初始化方法（无参）
    /// 用于加载材质、配置参数等
    /// </summary>
    void Initialize();

    /// <summary>
    /// 是否启用该后处理效果
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 后处理效果的名称（用于调试和管理）
    /// </summary>
    string EffectName { get; }

    /// <summary>
    /// 清理资源（可选实现）
    /// </summary>
    void Cleanup();
}