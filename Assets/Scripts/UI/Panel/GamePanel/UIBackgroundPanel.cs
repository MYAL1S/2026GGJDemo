using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIBackgroundPanel 类负责管理电梯内背景的显示和动画效果
/// 配合GamePanel使用
/// 特别是电梯门的开关动画。它监听电梯门状态变化事件，并根据状态切换门的动画参数，实现门的开合效果。
/// </summary>
public class UIBackgroundPanel : BasePanel
{
    /// <summary>
    /// 门的动画组件
    /// </summary>
    private Animator dooraAnimator;

    public override void Init()
    {
        base.Init();
        
        // 获取门的动画组件
        dooraAnimator = GetComponent<Animator>();

        // 监听电梯门状态变化
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);
    }


    /// <summary>
    /// 电梯门状态变化回调
    /// </summary>
    /// <param name="isOpen">true=开门（隐藏门），false=关门（显示门）</param>
    private void OnDoorStateChanged(bool isOpen)
    {
        dooraAnimator.SetBool("isDoorOpen", isOpen);
    }


    private void OnDestroy()
    {
        // 移除事件监听
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);
    }
}
