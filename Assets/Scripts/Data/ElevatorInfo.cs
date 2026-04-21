using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 电梯信息类
/// </summary>
[Obsolete("已弃用 由GameDataMgr统一管理")]
public class ElevatorInfo
{
    /// <summary>
    /// 电梯的最大稳定值
    /// </summary>
    public int maxStability;
    /// <summary>
    /// 电梯的当前稳定值
    /// </summary>
    public int nowStability;
}
