using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面具管理器
/// </summary>
public class MaskMgr : BaseSingleton<MaskMgr>
{
    /// <summary>
    /// 普通面具ID
    /// </summary>
    private const int NormalMaskId = 1;

    /// <summary>
    /// 是否可以切换面具
    /// </summary>
    private bool canSwitchMask = true;

    /// <summary>
    /// 是否有面具效果正在进行中
    /// </summary>
    private bool isMaskEffectActive = false;

    private MaskMgr() { }

    /// <summary>
    /// 尝试使用指定面具（切换 + 触发效果）
    /// </summary>
    public void TryUseMask(int maskID)
    {
        Debug.Log($"[MaskMgr] TryUseMask({maskID})");

        // 1. 检查电梯状态（只能在电梯运行状态下使用）
        if (!ElevatorMgr.Instance.CanUseMask)
        {
            Debug.Log("[MaskMgr] 电梯状态不允许使用面具");
            return;
        }

        // 2. 检查是否有效果正在进行
        if (isMaskEffectActive)
        {
            Debug.Log("[MaskMgr] 面具效果进行中，无法切换");
            return;
        }

        // 3. 检查切换冷却
        if (!canSwitchMask)
        {
            Debug.Log("[MaskMgr] 面具切换冷却中");
            return;
        }

        // 4. 检查玩家数据
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[MaskMgr] PlayerInfo 为空");
            return;
        }

        // 5. 检查是否拥有该面具
        if (playerInfo.gotMaskIDList == null || !playerInfo.gotMaskIDList.Contains(maskID))
        {
            Debug.Log($"[MaskMgr] 未拥有面具: {maskID}");
            return;
        }

        // 6. 获取面具配置并检查冷却
        var maskInfo = GetMaskInfoById(maskID);
        if (maskInfo != null && !maskInfo.canUseInElevator)
        {
            Debug.Log($"[MaskMgr] 面具 {maskID} 正在冷却中");
            return;
        }

        // === 通过所有检查，执行切换 ===

        // 切换面具并更新UI
        SetCurrentMask(maskID);
        Debug.Log($"[MaskMgr] 成功切换到面具: {maskID}");

        // 触发面具效果
        TriggerMaskEffect(maskID);

        // 普通面具无需持续时间和冷却控制
        if (maskID == NormalMaskId)
            return;

        // 特殊面具：效果期间禁止切换
        isMaskEffectActive = true;
        canSwitchMask = false;

        // 获取持续时间（默认3秒）
        int duration = (maskInfo != null && maskInfo.durationInMilliseconds > 0)
            ? maskInfo.durationInMilliseconds
            : 3000;

        // 效果结束后：切回普通面具 + 进入冷却
        TimerMgr.Instance.CreateTimer(false, duration, () =>
        {
            Debug.Log($"[MaskMgr] 面具 {maskID} 效果结束");

            // 切回普通面具
            SetCurrentMask(NormalMaskId);

            // 恢复切换能力
            isMaskEffectActive = false;
            canSwitchMask = true;

            // 该面具进入冷却
            if (maskInfo != null)
                StartMaskCooldown(maskInfo);
        });
    }

    /// <summary>
    /// 触发面具效果
    /// </summary>
    private void TriggerMaskEffect(int maskID)
    {
        switch (maskID)
        {
            case 0:
                Debug.Log("[MaskMgr] 无面具效果");
                break;
            case 1:
                Debug.Log("[MaskMgr] 普通面具效果");
                break;
            case 2:
                Debug.Log("[MaskMgr] 破妄面具效果");
                EffectDelusionBreak();
                break;
            case 3:
                Debug.Log("[MaskMgr] 镇邪面具效果");
                EffectSubdueEvil();
                break;
        }
    }

    /// <summary>
    /// 破妄面具效果：显示隐藏层
    /// </summary>
    private void EffectDelusionBreak()
    {
        var maskInfo = GetMaskInfoById(2);
        int duration = (maskInfo != null) ? maskInfo.durationInMilliseconds : 3000;
        int cost = (maskInfo != null) ? maskInfo.psychicPowerValue : 0;

        // 扣除灵能值
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (cost > 0)
        {
            if (playerInfo.nowPsychicPowerValue < cost)
            {
                Debug.Log("[MaskMgr] 灵能值不足");
                return;
            }
            playerInfo.nowPsychicPowerValue -= cost;
            Debug.Log($"[MaskMgr] 消耗灵能 {cost}，剩余 {playerInfo.nowPsychicPowerValue}");
        }

        // 显示隐藏层UI
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            panel.ShowRenderTextureUI(duration);
        });
    }

    /// <summary>
    /// 镇邪面具效果：驱散幽灵
    /// </summary>
    private void EffectSubdueEvil()
    {
        // 若处于异常事件，解除异常
        if (EventMgr.Instance.IsInUnnormalState)
        {
            EventMgr.Instance.ResolveUnnormalBySubdueMask();
            return;
        }

        // 播放音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
    }

    /// <summary>
    /// 设置当前面具并更新UI
    /// </summary>
    private void SetCurrentMask(int maskID)
    {
        GameDataMgr.Instance.PlayerInfo.nowMaskID = maskID;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_UpdateMaskUI, maskID);
    }

    /// <summary>
    /// 根据ID获取面具配置
    /// </summary>
    private MaskInfo GetMaskInfoById(int maskID)
    {
        var list = GameDataMgr.Instance.MaskInfoList;
        if (list == null || list.Count == 0)
            return null;
        return list.Find(m => m.maskID == maskID);
    }

    /// <summary>
    /// 开始面具冷却
    /// </summary>
    private void StartMaskCooldown(MaskInfo maskInfo)
    {
        maskInfo.canUseInElevator = false;

        int cooldown = (maskInfo.cooldownInMilliseconds > 0)
            ? maskInfo.cooldownInMilliseconds
            : 5000;

        Debug.Log($"[MaskMgr] 面具 {maskInfo.maskID} 进入冷却 {cooldown}ms");

        TimerMgr.Instance.CreateTimer(false, cooldown, () =>
        {
            maskInfo.canUseInElevator = true;
            Debug.Log($"[MaskMgr] 面具 {maskInfo.maskID} 冷却结束");
        });
    }

    // 保留空方法兼容旧代码
    public void Start() { }
    public void Stop() { }
}
