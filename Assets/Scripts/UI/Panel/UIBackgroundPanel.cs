using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundPanel : BasePanel
{
    private Image doorLeft;
    private Image doorRight;
    /// <summary>
    /// 门的动画组件
    /// </summary>
    private Animator dooraAnimator;

    public override void Init()
    {
        base.Init();
        
        // 获取左右门
        doorLeft = GetControl<Image>("DoorLeft");
        doorRight = GetControl<Image>("DoorRight");

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

    /// <summary>
    /// UIBackgroundPanel 不播放出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        // 不播放音效
    }
}
