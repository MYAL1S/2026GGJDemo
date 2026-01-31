using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件类型枚举 供事件中心类使用
/// 如果需要添加新的事件类型 在此枚举中添加即可
/// </summary>
public enum E_EventType
{
    /// <summary>
    /// 怪物死亡事件-不传入任何参数
    /// </summary>
    E_MonsterDead,
    /// <summary>
    /// 测试事件-不传入参数
    /// </summary>
    E_Test,
    /// <summary>
    /// 场景加载进度变化事件-传入的float百分比值
    /// </summary>
    E_LoadScene,

    /// <summary>
    /// 技能1事件-不传入参数
    /// </summary>
    E_Skill_1,
    /// <summary>
    /// 技能2事件-不传入参数
    /// </summary>
    E_Skill_2,
    /// <summary>
    /// 水平方向输入事件-传入的float轴值
    /// </summary>
    E_Input_Horizontal,
    /// <summary>
    /// 垂直方向输入事件-传入的float轴值
    /// </summary>
    E_Input_Vertical,
    /// <summary>
    /// 面具1切换事件-不传入参数
    /// </summary>
    E_ItemChangeMask1,
    /// <summary>
    /// 面具2切换事件-不传入参数
    /// </summary>
    E_ItemChangeMask2,
    /// <summary>
    /// 面具3切换事件-不传入参数
    /// </summary>
    E_ItemChangeMask3,
    /// <summary>
    /// 面具UI更新事件-传入的int面具ID
    /// </summary>
    E_UpdateMaskUI,
    /// <summary>
    /// 乘客点击事件-传入的Passenger对象
    /// </summary>
    E_PassengerClicked,
    /// <summary>
    /// 面具镜子UI更新事件-不传入参数
    /// </summary>
    E_MirrorUIUpdate,
    /// <summary>
    /// 乘客UI出现事件-不传入参数
    /// </summary>
    E_PassengerUIAppear,
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
    /// 稳定度变化事件-传入int当前稳定度
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
}
