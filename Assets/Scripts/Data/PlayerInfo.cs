using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色信息类
/// </summary>
public class PlayerInfo
{
    /// <summary>
    /// 最大灵能值
    /// </summary>
    public int maxPsychicPowerValue;
    /// <summary>
    /// 当前灵能值
    /// </summary>
    public int nowPsychicPowerValue;
    /// <summary>
    /// 当前使用的MaskID
    /// 如果没有使用面具则为0
    /// </summary>
    public int nowMaskID;
    /// <summary>
    /// 拥有的面具列表
    /// 如果没有面具则为空列表
    /// </summary>
    public List<int> gotMaskIDList;
}
