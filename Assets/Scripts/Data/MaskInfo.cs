using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面具信息类
/// </summary>
public class MaskInfo
{
    /// <summary>
    /// 面具id
    /// </summary>
    public int maskID;
    /// <summary>
    /// 面具名称
    /// </summary>
    public string maskName;
    /// <summary>
    /// 消耗的灵能值
    /// </summary>
    public int psychicPowerValue;
    /// <summary>
    /// 技能持续时间 毫秒为单位
    /// </summary>
    public int durationInMilliseconds;
    /// <summary>
    /// 冷却时间 毫秒为单位
    /// </summary>
    public int cooldownInMilliseconds;
    /// <summary>
    /// 是否可以在电梯内使用
    /// </summary>
    public bool canUseInElevator;
    /// <summary>
    /// 面具描述
    /// </summary>
    public string maskDescription;
    /// <summary>
    /// 面具资源路径
    /// </summary>
    public string maskResPath;
}
