using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件类型枚举 供事件中心类使用
/// </summary>
public enum E_EventType
{
    /// <summary>
    /// 场景加载进度变化事件-传入float百分比值
    /// </summary>
    E_LoadScene,
    /// <summary>
    /// 水平方向输入事件-传入float轴值
    /// </summary>
    E_Input_Horizontal,
    /// <summary>
    /// 垂直方向输入事件-传入float轴值
    /// </summary>
    E_Input_Vertical,
    /// <summary>
    /// 乘客点击事件-传入Passenger对象
    /// </summary>
    E_PassengerClicked,
    /// <summary>
    /// 镜子UI更新事件-不传入参数
    /// </summary>
    E_MirrorUIUpdate,
    /// <summary>
    /// 异常事件开始
    /// </summary>
    E_UnnormalEventStart,
    /// <summary>
    /// 异常事件解决
    /// </summary>
    E_UnnormalEventResolved,
    /// <summary>
    /// 灵能值变化事件-传入int当前灵能值
    /// </summary>
    E_PsychicPowerChanged,
    /// <summary>
    /// 信任度变化事件-传入int当前信任度
    /// </summary>
    E_StabilityChanged,
    /// <summary>
    /// 乘客数量变化事件-传入int当前乘客数量
    /// </summary>
    E_PassengerCountChanged,
    /// <summary>
    /// 电梯方向变化事件-传入bool，true为上升，false为下降
    /// </summary>
    E_ElevatorDirectionChanged,
    /// <summary>
    /// 倒计时更新事件-传入int剩余秒数
    /// </summary>
    E_CountdownUpdate,
    /// <summary>
    /// 特殊乘客能力耗尽
    /// </summary>
    E_SpecialPassengerExpired,
}
