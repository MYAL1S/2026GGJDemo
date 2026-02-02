using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundPanel : BasePanel
{
    private Image doorLeft;
    private Image doorRight;

    public override void Init()
    {
        base.Init();
        
        // 获取左右门
        doorLeft = GetControl<Image>("DoorLeft");
        doorRight = GetControl<Image>("DoorRight");
        
        // 监听电梯门状态变化
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);
    }

    public override void ShowMe()
    {
        base.ShowMe();
        
        // 显示时初始化门为关闭状态（显示门）
        SetDoorsVisible(true);
    }

    /// <summary>
    /// 电梯门状态变化回调
    /// </summary>
    /// <param name="isOpen">true=开门（隐藏门），false=关门（显示门）</param>
    private void OnDoorStateChanged(bool isOpen)
    {
        // 开门时隐藏门，关门时显示门
        SetDoorsVisible(!isOpen);
    }

    /// <summary>
    /// 设置门的可见性
    /// </summary>
    private void SetDoorsVisible(bool visible)
    {
        if (doorLeft != null)
            doorLeft.gameObject.SetActive(visible);
        if (doorRight != null)
            doorRight.gameObject.SetActive(visible);
        
        Debug.Log($"[UIBackgroundPanel] 电梯门: {(visible ? "关闭(显示)" : "开启(隐藏)")}");
    }

    public override void HideMe()
    {
        // 移除事件监听
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);
        base.HideMe();
    }

    /// <summary>
    /// UIBackgroundPanel 不播放出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        // 不播放音效
    }
}
