using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBackgroundPanel : BasePanel
{
    /// <summary>
    /// 无门背景（覆盖在门之上）
    /// </summary>
    private RawImage noDoorBK;

    public override void Init()
    {
        base.Init();

        // 获取无门背景控件
        noDoorBK = GetControl<RawImage>("NoDoorBK");

        // 注册电梯门状态事件
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);

        // 初始状态：门关闭，隐藏无门背景
        SetDoorOpen(false);
    }

    /// <summary>
    /// 设置门的开关状态
    /// </summary>
    /// <param name="isOpen">true=开门，false=关门</param>
    public void SetDoorOpen(bool isOpen)
    {
        if (noDoorBK != null)
        {
            noDoorBK.gameObject.SetActive(isOpen);
        }
    }

    /// <summary>
    /// 门状态变化回调
    /// </summary>
    private void OnDoorStateChanged(bool isOpen)
    {
        SetDoorOpen(isOpen);
    }

    public override void HideMe()
    {
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDoorStateChanged, OnDoorStateChanged);
        base.HideMe();
    }
}
