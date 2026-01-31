using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件类型枚举 在事件中心中使用
/// 如果需要添加新的事件类型 在此枚举中添加即可
/// </summary>
public enum E_EventType
{
    /// <summary>
    /// 怪物死亡事件-参数：怪物对象
    /// </summary>
    E_MonsterDead,
    /// <summary>
    /// 测试事件-参数：无
    /// </summary>
    E_Test,
    /// <summary>
    /// 场景加载进度变化事件-参数：float进度值
    /// </summary>
    E_LoadScene,

    /// <summary>
    /// 技能1事件-参数：无
    /// </summary>
    E_Skill_1,
    /// <summary>
    /// 技能2事件-参数：无
    /// </summary>
    E_Skill_2,
    /// <summary>
    /// 水平轴输入事件-参数：float轴值
    /// </summary>
    E_Input_Horizontal,
    /// <summary>
    /// 竖直轴输入事件-参数：float轴值
    /// </summary>
    E_Input_Vertical,
    /// <summary>
    /// 面具1切换事件-参数：无
    /// </summary>
    E_ItemChangeMask1,
    /// <summary>
    /// 面具2切换事件-参数：无
    /// </summary>
    E_ItemChangeMask2,
    /// <summary>
    /// 面具3切换事件-参数：无
    /// </summary>
    E_ItemChangeMask3,
    /// <summary>
    /// 面具UI更新事件-参数：int面具ID
    /// </summary>
    E_UpdateMaskUI,
    /// <summary>
    /// 乘客点击事件-参数：Passenger对象
    /// </summary>
    E_PassengerClicked,
    /// <summary>
    /// 面具镜面UI更新事件-参数：无
    /// </summary>
    E_MirrorUIUpdate,
    /// <summary>
    /// 乘客UI出现事件-参数：无
    /// </summary>
    E_PassengerUIAppear,
    E_UnnormalEventStart,
    E_UnnormalEventResolved,
}
