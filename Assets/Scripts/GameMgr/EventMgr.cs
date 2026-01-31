using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件管理器
/// </summary>
public class EventMgr : BaseSingleton<EventMgr>
{
    /// <summary>
    /// 观看铜镜的定时器
    /// </summary>
    private int timerID;
    private Coroutine fogCoroutine;
    /// <summary>
    /// 雾气面板
    /// </summary>
    private FogPanel fogPanel;
    /// <summary>
    /// 异常倒计时
    /// </summary>
    private int abnormalTimerId;
    /// <summary>
    /// 是否处于异常状态
    /// </summary>
    private bool isUnnormalState;
    public bool IsInUnnormalState => isUnnormalState;  // 新增只读访问

    /// <summary>
    /// 开始观看铜镜（受镜像次数配额限制）
    /// </summary>
    public void StartWatchMirror()
    {
        // 配额检查：超出则直接返回
        if (!GameLevelMgr.Instance.TryConsumeMirrorOccurence())
        {
            Debug.Log("镜像次数已达上限");
            return;
        }

        // 若已有定时器，先清理
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        // 凝视镜面：每3秒恢复30%灵能，持续10秒
        timerID = TimerMgr.Instance.CreateTimer(false, 10000, () =>
        {
            // 结束时刷新UI
            EventCenter.Instance.EventTrigger(E_EventType.E_MirrorUIUpdate);
            timerID = 0;
        }, 3000,
        () =>
        {
            int max = GameDataMgr.Instance.PlayerInfo.maxPsychicPowerValue;
            GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue += (int)(max * 0.3f);
            if (GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue > max)
                GameDataMgr.Instance.PlayerInfo.nowPsychicPowerValue = max;
            EventCenter.Instance.EventTrigger(E_EventType.E_MirrorUIUpdate);
        });

        // 触发鬼魂靠近事件
        PassengerMgr.Instance.GhostApproaching();

        // 显示雾气面板（缓慢淡入），如无 CanvasGroup 则补一个
        UIMgr.Instance.ShowPanel<FogPanel>(E_UILayer.Top, panel =>
        {
            fogPanel = panel;
            var cg = fogPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = fogPanel.gameObject.AddComponent<CanvasGroup>();
            StartFogFade(0f, 1f, 1.5f, hideAfterFade: false);
        });
    }

    /// <summary>
    /// 停止/取消观看铜镜
    /// </summary>
    public void StopWatchMirror()
    {
        // 停止灵能恢复
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        // 鬼魂停止靠近
        PassengerMgr.Instance.StopGhostApproaching();

        // 雾气缓慢淡出并隐藏面板
        StartFogFade(fogPanel != null ? fogPanel.GetComponent<CanvasGroup>()?.alpha ?? 1f : 1f,
                     0f, 1.0f, hideAfterFade: true);
    }

    /// <summary>控制雾气淡入淡出</summary>
    private void StartFogFade(float from, float to, float duration, bool hideAfterFade)
    {
        if (fogCoroutine != null)
            MonoMgr.Instance.StopCoroutine(fogCoroutine);

        fogCoroutine = MonoMgr.Instance.StartCoroutine(FogFadeRoutine(from, to, duration, hideAfterFade));
    }

    private IEnumerator FogFadeRoutine(float from, float to, float duration, bool hideAfterFade)
    {
        if (fogPanel == null)
            yield break;

        var cg = fogPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            yield break;

        cg.alpha = from;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, lerp);
            yield return null;
        }
        cg.alpha = to;

        if (hideAfterFade)
        {
            UIMgr.Instance.HidePanel<FogPanel>();
            fogPanel = null;
        }
        fogCoroutine = null;
    }

    /// <summary>
    /// 异常事件触发
    /// </summary>
    public void UnnormalEvent()
    {
        // 楼层显示异常
        ElevatorMgr.Instance.ChangeLevelUI(-18);

        isUnnormalState = true;

        // 3 秒后未解除则坠入异界
        abnormalTimerId = TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            if (!isUnnormalState) return;
            FallIntoAbyss();
        });
    }

    /// <summary>
    /// 使用镇邪面具解除异常
    /// </summary>
    public void ResolveUnnormalBySubdueMask()
    {
        if (!isUnnormalState)
            return;

        isUnnormalState = false;

        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }
        // 可在此恢复楼层显示等处理
    }

    private void FallIntoAbyss()
    {
        Debug.Log("电梯坠入异界，游戏结束");
        // TODO: 结束流程/结算
    }

    private EventMgr() { }
}
