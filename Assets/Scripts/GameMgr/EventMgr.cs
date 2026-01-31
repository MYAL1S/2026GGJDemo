using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件管理器
/// </summary>
public class EventMgr : BaseSingleton<EventMgr>
{
    private int timerID;
    private Coroutine fogCoroutine;
    private FogPanel fogPanel;

<<<<<<< HEAD
    /// <summary>
    /// 开始观看铜镜
    /// </summary>
    public void StartWatchMirror()
    {
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

        // 显示雾气面板（缓慢淡入）
        UIMgr.Instance.ShowPanel<FogPanel>(E_UILayer.Top, panel =>
        {
            fogPanel = panel;
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
        StartFogFade( fogPanel != null ? fogPanel.GetComponent<CanvasGroup>()?.alpha ?? 1f : 1f,
                      0f, 1.0f, hideAfterFade: true);
    }

    /// <summary>
    /// 控制雾气淡入淡出
    /// </summary>
    private void StartFogFade(float from, float to, float duration, bool hideAfterFade)
    {
        if (fogCoroutine != null)
            MonoMgr.Instance.StopCoroutine(fogCoroutine);

        fogCoroutine = MonoMgr.Instance.StartCoroutine(FogFadeRoutine(from, to, duration, hideAfterFade));
    }

    /// <summary>
    /// 控制雾气淡入淡出协程
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="duration"></param>
    /// <param name="hideAfterFade"></param>
    /// <returns></returns>
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

=======
>>>>>>> c10605e05a37127ce0294002779683d49e93f6c0
    private EventMgr() { }
}
