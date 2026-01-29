using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面具管理器
/// </summary>
public class MaskMgr : BaseSingleton<MaskMgr>
{
    /// <summary>
    /// 是否可以切换面具
    /// </summary>
    private bool canSwitchMask = true;
    public void Start()
    {
        //开启输入检测
        //可以通过InputMgr的相关方法来更改按键映射
        InputMgr.Instance.StartOrStopDetectInput(true);
        InputMgr.Instance.ChangeKeyCodeInfo(E_EventType.E_ItemChangeMask1, KeyCode.Alpha1, InputStatus.Down);
        InputMgr.Instance.ChangeKeyCodeInfo(E_EventType.E_ItemChangeMask2, KeyCode.Alpha2, InputStatus.Down);
        InputMgr.Instance.ChangeKeyCodeInfo(E_EventType.E_ItemChangeMask3, KeyCode.Alpha3, InputStatus.Down);
        EventCenter.Instance.AddEventListener<KeyCode>(E_EventType.E_ItemChangeMask1, MaskSwitching);
        EventCenter.Instance.AddEventListener<KeyCode>(E_EventType.E_ItemChangeMask2, MaskSwitching);
        EventCenter.Instance.AddEventListener<KeyCode>(E_EventType.E_ItemChangeMask3, MaskSwitching);
    }

    /// <summary>
    /// 根据面具id分发面具事件
    /// </summary>
    /// <param name="maskID">面具id</param>
    public void MaskEventHandler(int maskID)
    {
        switch (maskID)
        {
            case 0:
                MaskEventNoMask();
                break;
            case 1:
                MaskEventNormal();
                break;
            case 2:
                MaskEventDelusionBreak();
                break;
            case 3:
                MaskEventSubdueEvilGhost();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 没有面具时使用面具的事件
    /// </summary>
    public void MaskEventNoMask()
    {
        //TODO:没有面具时使用面具的事件
    }

    /// <summary>
    /// 使用普通面具的事件
    /// </summary>
    public void MaskEventNormal()
    {
        //TODO:使用普通面具的事件
    }

    /// <summary>
    /// 破妄面具事件 显示隐藏的精灵
    /// </summary>
    public void MaskEventDelusionBreak()
    {
        MaskInfo maskInfo = GameDataMgr.Instance.MaskInfoList[1];
        //扣除角色灵能值
        if (GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue <= maskInfo.psychicPowerValue)
            return;
        GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue -= maskInfo.psychicPowerValue;

        //TODO:
        //更新灵能值UI

        //播放面具特效
        #region TestCode
        //此处仅为测试使用 测试完成后请删除
        Debug.Log(GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue);
        #endregion

        //将面具设置为不可用
        GameDataMgr.Instance.MaskInfoList[1].canUseInElevator = false;
        //等待冷却时间结束后将面具设置为可用
        TimerMgr.Instance.CreateTimer(false, maskInfo.cooldownInMilliseconds, () =>
        {
            GameDataMgr.Instance.MaskInfoList[1].canUseInElevator = true;
        });


        //将隐藏层显示出来
        Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("HidenLayer"));
        //持续时间结束后将隐藏层重新隐藏
        TimerMgr.Instance.CreateTimer(false, maskInfo.durationInMilliseconds, () =>
        {
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("HidenLayer"));
        });
    }

    /// <summary>
    /// 震慑面具事件 驱散范围内的幽灵
    /// </summary>
    public void MaskEventSubdueEvilGhost()
    {
        //播放驱散幽灵特效

        //播放面具特效

        //驱散范围内的幽灵

        #region TestCode
        //此处仅为测试使用 测试完成后请删除
        PassengerMgr.Instance.passengerList.ForEach(x =>
        {
            if (x.passengerInfo.isGhost)
            {
                //驱散该幽灵
                x.gameObject.SetActive(false);
                //从关卡乘客列表中移除该幽灵
            }
        });
        #endregion
    }

    /// <summary>
    /// 根据按键切换面具
    /// </summary>
    /// <param name="keycode">监听的按键</param>
    public void MaskSwitching(KeyCode keycode)
    {
        if (!canSwitchMask || GameDataMgr.Instance.PlayerInfo.gotMaskIDList == null)
            return;

        switch (keycode)
        {
            case KeyCode.Alpha1:
                ReallyMaskSwitching(1);
                break;
            case KeyCode.Alpha2:
                ReallyMaskSwitching(2);
                break;
            case KeyCode.Alpha3:
                ReallyMaskSwitching(3);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 实际进行面具切换的逻辑处理
    /// </summary>
    /// <param name="maskID"></param>
    private void ReallyMaskSwitching(int maskID)
    {
        int maskChangedID = GameDataMgr.Instance.PlayerInfo.gotMaskIDList.Find(x => x == maskID);
        if (maskChangedID == 0)
        {
            //没有该面具的逻辑处理

            return;
        }
        //有该面具则进行切换
        //改变角色当前面具id
        GameDataMgr.Instance.PlayerInfo.nowMaskID = maskChangedID;
        //触发面具UI更新事件
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_UpdateMaskUI, maskChangedID);
        //进入切换冷却
        canSwitchMask = false;
        //冷却时间结束后允许切换面具
        TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            canSwitchMask = true;
        });
    }

    public void Stop()
    {
        //移除面具切换监听
        EventCenter.Instance.RemoveEventListener<KeyCode>(E_EventType.E_ItemChangeMask1, MaskSwitching);
        EventCenter.Instance.RemoveEventListener<KeyCode>(E_EventType.E_ItemChangeMask2, MaskSwitching);
        EventCenter.Instance.RemoveEventListener<KeyCode>(E_EventType.E_ItemChangeMask3, MaskSwitching);
    }
    private MaskMgr() { }
}
