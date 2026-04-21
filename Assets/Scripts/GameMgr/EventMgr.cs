using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件管理器：负责“注视铜镜”与“异常事件”两条核心流程的状态与计时控制。
///
/// 主要职责：
/// 1) 管理注视铜镜时的周期回血、UI刷新、鬼影逼近与迷雾表现；
/// 2) 管理异常事件的开启、限时、解决与失败结算；
/// 3) 在关卡切换/重开时统一清理内部状态（计时器、协程、UI引用）。
/// </summary>
public class EventMgr : BaseSingleton<EventMgr>
{
    /// <summary>
    /// 注视铜镜流程计时器 ID。
    /// 非 0 表示当前已有有效计时器在运行。
    /// </summary>
    private int timerID;

    /// <summary>
    /// 迷雾淡入淡出协程句柄，用于避免重复开协程。
    /// </summary>
    private Coroutine fogCoroutine;

    /// <summary>
    /// 当前显示中的迷雾面板引用。
    /// </summary>
    private FogPanel fogPanel;

    /// <summary>
    /// 异常事件超时计时器 ID。
    /// 非 0 表示当前存在未结束的异常倒计时。
    /// </summary>
    private int abnormalTimerId;

    /// <summary>
    /// 是否处于异常事件状态。
    /// </summary>
    private bool isUnnormalState;

    /// <summary>
    /// 对外只读：当前是否处于异常事件中。
    /// </summary>
    public bool IsInUnnormalState => isUnnormalState;


    /// <summary>
    /// 开始“注视铜镜”流程：
    /// - 清理旧计时器，避免重复；
    /// - 启动 10 秒流程：每 3 秒回复一次精神值并刷新 UI；
    /// - 流程结束后再次刷新铜镜 UI；
    /// - 触发鬼影逼近；
    /// - 显示迷雾面板并淡入。
    /// </summary>
    public void StartWatchMirror()
    {
        // 防止重复开启导致多个计时器并发。
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        // 计时器参数含义（结合当前调用约定）：
        // - 总时长 10000ms；
        // - 结束回调：刷新 UI 并清空 timerID；
        // - 间隔 3000ms；
        // - 间隔回调：恢复 30% 最大精神值并刷新 UI。
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

        // 视觉/氛围：注视铜镜时鬼影开始逼近。
        PassengerMgr.Instance.GhostApproaching();

        // 显示迷雾并淡入（0 -> 1）。
        UIMgr.Instance.ShowPanel<FogPanel>(E_UILayer.Top, panel =>
        {
            fogPanel = panel;
            var cg = fogPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = fogPanel.gameObject.AddComponent<CanvasGroup>();
            StartFogFade(0f, 1f, 1.5f, hideAfterFade: false);
        });
    }

    /// <summary>
    /// 停止“注视铜镜”流程：
    /// - 停止铜镜计时器；
    /// - 停止鬼影逼近；
    /// - 迷雾从当前透明度淡出并隐藏面板。
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
    /// 启动迷雾淡入淡出。
    /// 若已有淡入淡出协程，先停止再启动新的，确保过程唯一。
    /// </summary>
    private void StartFogFade(float from, float to, float duration, bool hideAfterFade)
    {
        if (fogCoroutine != null)
            MonoMgr.Instance.StopCoroutine(fogCoroutine);

        fogCoroutine = MonoMgr.Instance.StartCoroutine(FogFadeRoutine(from, to, duration, hideAfterFade));
    }

    /// <summary>
    /// 迷雾透明度插值协程。
    /// hideAfterFade = true 时，结束后隐藏面板并释放 fogPanel 引用。
    /// </summary>
    private IEnumerator FogFadeRoutine(float from, float to, float duration, bool hideAfterFade)
    {
        // 面板未准备好时直接退出，避免空引用。
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
    /// 触发异常事件（不暂停电梯流程）：
    /// - 仅在“非异常状态”下允许触发；
    /// - 广播异常开始事件；
    /// - 启动超时计时器，超时未处理则触发失败结算。
    /// </summary>
    public void UnnormalEvent()
    {
        // 已在异常状态时不重复触发，避免叠加多个超时计时器。
        if (isUnnormalState) return;

        isUnnormalState = true;

        // 配置秒数转毫秒。
        int timeout = ResourcesMgr.Instance.abnormalEventTimeout * 1000;

        // 兜底：配置异常（<=0）时使用默认 30 秒，防止触发即失败。
        if (timeout <= 0)
        {
            Debug.LogWarning("[EventMgr] 警告：异常超时时间配置为 0！已自动修正为 30秒");
            timeout = 30000;
        }

        // 广播异常开始，供 UI/系统进入异常态表现。
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventStart);

        // 先清理旧倒计时，确保同一时刻只有一个异常超时计时器。
        if (abnormalTimerId != 0) TimerMgr.Instance.RemoveTimer(abnormalTimerId);

        // 超时回调：仅在仍处于异常态时触发失败。
        abnormalTimerId = TimerMgr.Instance.CreateTimer(false, timeout, () =>
        {
            abnormalTimerId = 0;
            if (isUnnormalState)
            {
                Debug.LogError("[EventMgr] 异常事件超时！触发坠崖！(这是导致电梯卡死的直接原因)");
                FallIntoAbyss();
            }
        });

        Debug.Log($"[EventMgr] 异常事件开始，限时: {timeout / 1000}秒");
    }

    /// <summary>
    /// 通过“镇压面具”解决异常事件。
    /// </summary>
    public void ResolveUnnormalBySubdueMask()
    {
        ResolveUnnormal();
    }

    /// <summary>
    /// 通过“铃铛”解决异常事件：
    /// - 播放铃声；
    /// - 统一走 ResolveUnnormal 收敛逻辑。
    /// </summary>
    public void ResolveUnnormalByBell()
    {
        if (!isUnnormalState)
            return;
        MusicMgr.Instance.PlaySound("Music/26GGJsound/bell_ring", false);
        ResolveUnnormal();
    }

    /// <summary>
    /// 异常事件统一收敛逻辑：
    /// - 退出异常状态；
    /// - 停止异常倒计时；
    /// - 广播异常已解决。
    /// </summary>
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

        // 触发异常事件解决
        EventCenter.Instance.EventTrigger(E_EventType.E_UnnormalEventResolved);
    }

    /// <summary>
    /// 异常超时失败结算：
    /// - 清理异常状态与计时器；
    /// - 停止电梯；
    /// - 打开失败结算面板。
    /// </summary>
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
    /// 重置事件管理器状态。
    /// 用于重新开始关卡/流程切换时的强制清理，避免历史计时器或协程残留。
    /// </summary>
    public void ResetState()
    {
        // 退出异常状态。
        isUnnormalState = false;

        // 清理异常超时计时器。
        if (abnormalTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(abnormalTimerId);
            abnormalTimerId = 0;
        }

        // 清理注视铜镜计时器。
        if (timerID != 0)
        {
            TimerMgr.Instance.RemoveTimer(timerID);
            timerID = 0;
        }

        // 停止迷雾淡入淡出协程。
        if (fogCoroutine != null)
        {
            MonoMgr.Instance.StopCoroutine(fogCoroutine);
            fogCoroutine = null;
        }

        // 释放状态引用。
        fogPanel = null;

        Debug.Log("[EventMgr] 状态已重置");
    }

    private EventMgr() { }
}
