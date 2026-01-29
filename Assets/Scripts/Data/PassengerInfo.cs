using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 角色信息类 记录每个乘客的信息
/// </summary>
public class PassengerInfo
{
    /// <summary>
    /// 乘客ID
    /// </summary>
    public int passengerID;
    /// <summary>
    /// 乘客姓名
    /// </summary>
    public string name;
    /// <summary>
    /// 关联的预设体路径
    /// </summary>
    public string resPath;
    /// <summary>
    /// 是否为鬼魂乘客
    /// </summary>
    public bool isGhost;
    /// <summary>
    /// 信任度
    /// </summary>
    public int trustValue;
}
