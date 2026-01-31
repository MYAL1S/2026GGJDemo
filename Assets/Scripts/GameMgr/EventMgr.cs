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
    /// 异常事件定时
    /// </summary>
    private int abnormalTimerId;
    /// <summary>
    /// 是否在异常状态
    /// </summary>
    private bool isUnnormalState;
    public bool IsInUnnormalState => isUnnormalState;

    /// <summary>
    /// 异常事件超时时间（毫秒）
    /// </summary>
    private const int AbnormalTimeout = 10000;

    /// <summary>
    /// 开始观看铜镜（灵能恢复，鬼魂靠近）
    /// </summary>
    public void StartWatchMirror()
    {
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        timerID = TimerMgr.Instance.CreateTimer(false, 10000, () =>
        {
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

        PassengerMgr.Instance.GhostApproaching();

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
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        PassengerMgr.Instance.StopGhostApproaching();

        StartFogFade(fogPanel != null ? fogPanel.GetComponent<CanvasGroup>()?.alpha ?? 1f : 1f,
                     0f, 1.0f, hideAfterFade: true);
    }

    /// <summary>
    /// 雾气面板淡入淡出
    /// </summary>
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
        if (isUnnormalState)
            return;

        isUnnormalState = true;

        // 楼层显示异常
        ElevatorMgr.Instance.ChangeLevelUI(-18);

        Debug.Log("[EventMgr] 异常事件触发！需要晃动铃铛解决");

        // 触发异常事件开始事件（通知电梯暂停）
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventStart);

        // 超时未解决则坠入深渊
        abnormalTimerId = TimerMgr.Instance.CreateTimer(false, AbnormalTimeout, () =>
        {
            if (!isUnnormalState) return;
            FallIntoAbyss();
        });
    }

    /// <summary>
    /// 使用镇邪面具解决异常（保留兼容旧代码）
    /// </summary>
    public void ResolveUnnormalBySubdueMask()
    {
        ResolveUnnormal();
    }

    /// <summary>
    /// 使用铃铛解决异常事件
    /// </summary>
    public void ResolveUnnormalByBell()
    {
        if (!isUnnormalState)
            return;

        Debug.Log("[EventMgr] 铃铛成功解决异常事件");

        // 播放解决音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/bell_ring", false);

        ResolveUnnormal();
    }

    /// <summary>
    /// 解决异常事件的通用方法
    /// </summary>
    private void ResolveUnnormal()
    {
        if (!isUnnormalState)
            return;

        isUnnormalState = false;

        // 取消超时定时器
        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        Debug.Log("[EventMgr] 异常事件已解决");

        // 触发异常事件解决事件（通知电梯继续）
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventResolved);
    }

    /// <summary>
    /// 坠入深渊 游戏失败
    /// </summary>
    public void FallIntoAbyss()
    {
        isUnnormalState = false;

        // 取消超时定时器
        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        Debug.Log("[EventMgr] 坠入深渊，游戏失败");

        // 停止电梯
        ElevatorMgr.Instance.StopElevator();

        UIMgr.Instance.ShowPanel<GameOverPanel>(E_UILayer.Top, (panel) =>
        {
            panel.ShowResult(false);
        });
    }

    private EventMgr() { }
}
