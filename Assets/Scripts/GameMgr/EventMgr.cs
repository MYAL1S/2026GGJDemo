using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventMgr : BaseSingleton<EventMgr>
{
    private int timerID;
    private Coroutine fogCoroutine;
    private FogPanel fogPanel;
    private int abnormalTimerId;
    private bool isUnnormalState;
    public bool IsInUnnormalState => isUnnormalState;

    private int abnormalCountdownRemaining;

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
    /// 触发异常事件（不暂停电梯流程）
    /// </summary>
    public void UnnormalEvent()
    {
        if (isUnnormalState)
            return;

        isUnnormalState = true;

        Debug.Log("[EventMgr] 异常事件触发，需要玩家处理");

        // ? 触发异常事件开始（只改变状态和音效，不暂停电梯）
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventStart);

        // 设置异常事件超时
        int timeout = ResourcesMgr.Instance.abnormalEventTimeout * 1000;
        abnormalCountdownRemaining = timeout;

        // 超时未处理则坠入深渊
        abnormalTimerId = TimerMgr.Instance.CreateTimer(false, timeout, () =>
        {
            if (!isUnnormalState) return;
            FallIntoAbyss();
        });
    }

    public void ResolveUnnormalBySubdueMask()
    {
        ResolveUnnormal();
    }

    public void ResolveUnnormalByBell()
    {
        if (!isUnnormalState)
            return;

        Debug.Log("[EventMgr] 铃铛成功解决异常事件");
        MusicMgr.Instance.PlaySound("Music/26GGJsound/bell_ring", false);
        ResolveUnnormal();
    }

    private void ResolveUnnormal()
    {
        if (!isUnnormalState)
            return;

        isUnnormalState = false;

        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        Debug.Log("[EventMgr] 异常事件已解决");

        // ? 触发异常事件解决
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventResolved);
    }

    public void FallIntoAbyss()
    {
        isUnnormalState = false;

        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        Debug.Log("[EventMgr] 坠入深渊，游戏失败");

        ElevatorMgr.Instance.StopElevator();

        UIMgr.Instance.ShowPanel<GameOverPanel>(E_UILayer.Top, (panel) =>
        {
            panel.ShowResult(false);
        });
    }

    /// <summary>
    /// 重置事件管理器状态
    /// </summary>
    public void ResetState()
    {
        // 停止注视铜镜
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        // 停止异常事件
        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        // 停止迷雾淡入淡出
        if (fogCoroutine != null)
        {
            MonoMgr.Instance.StopCoroutine(fogCoroutine);
            fogCoroutine = null;
        }

        isUnnormalState = false;
        fogPanel = null;

        Debug.Log("[EventMgr] 状态已重置");
    }

    private EventMgr() { }
}
