using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡信息类 记录每一波关卡的信息
/// </summary>
public class LevelInfo
{
    /// <summary>
    /// 用于标识关卡的ID
    /// </summary>
    public int levelID;
    /// <summary>
    /// 关卡中乘客的ID列表 用逗号分隔 例如 "1,2,3,4"
    /// 通过调用TextUtil.StringSpilt2IntArray方法可以将其转换为整数数组
    /// 再进行相应的处理
    /// </summary>
    public string passengerIDs;
    /// <summary>
    /// 在该层的停泊时间 单位为秒
    /// </summary>
    public int dockingTime;
}
