using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 电梯状态枚举
/// </summary>
public enum E_ElevatorState
{
    Moving,
    Arriving,
    Stopped,
    Departing,
    Abnormal
}

/// <summary>
/// 电梯管理器
/// </summary>
public class ElevatorMgr : BaseSingleton<ElevatorMgr>
{
    private E_ElevatorState currentElevatorState = E_ElevatorState.Stopped;
    public E_ElevatorState CurrentState => currentElevatorState;

    private Text txtFloor;

    private int waveNum = 0;
    private int levelNum = 0;

    private WaveDetailSO currentWave;
    private LevelDetailSO currentLevelDetail;
    private int currentLevelIndex;
    private bool pendingMirrorEvent;

    private E_ElevatorState stateBeforeAbnormal;

    /// <summary>
    /// 当前活动的定时器ID
    /// </summary>
    private int activeTimerId = 0;

    private int previousLevel = 0;
    private int currentLevel = 0;
    private int countdownRemaining;

    /// <summary>
    /// 电梯是否正在运行
    /// </summary>
    private bool isRunning = false;

    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        currentElevatorState == E_ElevatorState.Abnormal;

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;
    public bool IsInAbnormalState => currentElevatorState == E_ElevatorState.Abnormal;

    /// <summary>
    /// 启动电梯
    /// </summary>
    public void StartElevator()
    {
        if (isRunning)
        {
            Debug.LogWarning("[ElevatorMgr] 电梯已在运行中");
            return;
        }

        isRunning = true;

        // 重置本局的配额计数
        GameLevelMgr.Instance.ResetRuntimeCounters();

        // 重置波数和关卡
        waveNum = 0;
        levelNum = 0;
        previousLevel = 0;
        currentLevel = 0;

        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience");
        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);

        EnterMovingState();
    }

    /// <summary>
    /// 清除当前活动的定时器
    /// </summary>
    private void ClearActiveTimer()
    {
        if (activeTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(activeTimerId);
            activeTimerId = 0;
        }
    }

    /// <summary>
    /// 电梯进入移动状态
    /// </summary>
    public void EnterMovingState()
    {
        if (!isRunning) return;

        // 清除之前的定时器
        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("[ElevatorMgr] 电梯开始运行...");

        int movingDuration = Random.Range(5, 10) * 1000;
        StartCountdown(movingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            // 20% 概率触发异常事件
            bool triggerAbnormal = Random.value < 0.2f;
            if (triggerAbnormal)
            {
                EventMgr.Instance.UnnormalEvent();
                return;
            }

            EnterArrivingState();
        });
    }

    private void OnUnnormalEventStart()
    {
        Debug.Log("[ElevatorMgr] 异常事件开始，电梯进入异常状态");
        stateBeforeAbnormal = currentElevatorState;
        currentElevatorState = E_ElevatorState.Abnormal;

        // 停止倒计时
        StopCountdown();

        // 清除当前定时器
        ClearActiveTimer();
    }

    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常事件已解决，电梯恢复运行");
        
        if (!isRunning) return;

        // 异常解决后进入到达状态
        EnterArrivingState();
    }

    /// <summary>
    /// 电梯进入到达状态
    /// </summary>
    public void EnterArrivingState()
    {
        if (!isRunning) return;

        // 清除之前的定时器
        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Arriving;
        Debug.Log("[ElevatorMgr] 电梯即将到达...");

        // 选波
        if (ResourcesMgr.Instance.waveSOList == null || ResourcesMgr.Instance.waveSOList.Count == 0)
        {
            Debug.LogError("[ElevatorMgr] waveSOList 为空!");
            return;
        }

        currentWave = ResourcesMgr.Instance.waveSOList[waveNum];
        if (levelNum >= currentWave.levelDetails.Count)
        {
            levelNum = 0;
            waveNum = Random.Range(0, ResourcesMgr.Instance.waveSOList.Count);
            currentWave = ResourcesMgr.Instance.waveSOList[waveNum];
        }

        // 选层
        currentLevelIndex = levelNum;
        currentLevelDetail = currentWave.levelDetails[currentLevelIndex];
        GameLevelMgr.Instance.currentLevelDetail = currentLevelDetail;
        levelNum++;

        pendingMirrorEvent = currentWave.createMirrorLevelIndex != null &&
                             currentWave.createMirrorLevelIndex.Contains(currentLevelIndex);

        // 保存上一楼层，更新当前楼层
        previousLevel = currentLevel;
        currentLevel = currentLevelDetail.level;

        // 判断电梯方向并触发事件
        bool isGoingUp = currentLevel > previousLevel;
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);
        Debug.Log($"[ElevatorMgr] 电梯方向: {(isGoingUp ? "上升" : "下降")} ({previousLevel} -> {currentLevel})");

        // 开始到达状态倒计时（3秒）
        StartCountdown(3000);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_beep", false);
            ChangeLevelUI(currentLevelDetail.level);
            EnterStoppedState();
        }, 400,
        () =>
        {
            // 楼层数字滚动效果
            if (isRunning)
                ChangeLevelUI(Random.Range(2, 18));
        });
    }

    /// <summary>
    /// 电梯进入停靠状态
    /// </summary>
    public void EnterStoppedState()
    {
        if (!isRunning) return;

        // 清除之前的定时器
        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Stopped;
        Debug.Log("[ElevatorMgr] 电梯已停靠");

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_dooropen", false);
        PassengerMgr.Instance.SpawnWave();

        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        UIMgr.Instance.GetPanel<GamePanel>(panel =>
        {
            if (panel == null) return;
            if (canTriggerMirror)
                panel.ShowMirrorUI();
            else
                panel.HideMirrorUI();
        });
        pendingMirrorEvent = false;

        // 开始停靠状态倒计时
        int dockingDuration = GameLevelMgr.Instance.currentLevelDetail.dockingTime * 1000;
        StartCountdown(dockingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, dockingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            EnterDepartingState();
        });
    }

    /// <summary>
    /// 电梯进入离开状态
    /// </summary>
    public void EnterDepartingState()
    {
        if (!isRunning) return;

        // 清除之前的定时器
        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;
        Debug.Log("[ElevatorMgr] 电梯正在离开...");

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_doorclose", false);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, 1000, () =>
        {
            activeTimerId = 0;

            if (!isRunning) return;

            CheckResults();

            // 如果游戏未结束，继续下一轮
            if (isRunning)
                EnterMovingState();
        });
    }

    #region 倒计时逻辑

    private void StartCountdown(int durationMs)
    {
        countdownRemaining = durationMs;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
    }

    private void StopCountdown()
    {
        countdownRemaining = 0;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);
    }

    private void UpdateCountdown()
    {
        if (countdownRemaining <= 0)
            return;

        int previousSeconds = countdownRemaining / 1000;
        countdownRemaining -= (int)(Time.deltaTime * 1000);
        int currentSeconds = Mathf.Max(0, countdownRemaining / 1000);

        if (currentSeconds != previousSeconds)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, currentSeconds);
        }
    }

    #endregion

    public void ChangeLevelUI(int level)
    {
        if (txtFloor != null)
            txtFloor.text = level.ToString();
    }

    /// <summary>
    /// 检查本层结果
    /// </summary>
    private void CheckResults()
    {
        int ghostCount = 0;
        var list = PassengerMgr.Instance.passengerList;
        if (list != null)
        {
            foreach (var p in list)
            {
                if (p != null && p.passengerInfo != null && p.passengerInfo.isGhost && p.gameObject.activeSelf)
                    ghostCount++;
            }
        }

        // 有鬼魂未被驱逐，扣除信任度
        if (ghostCount > 0)
        {
            GameDataMgr.Instance.SubTrustValue(ghostCount);
            Debug.Log($"[ElevatorMgr] 本层有 {ghostCount} 个鬼魂未被驱逐，扣除信任度");

            // 检查是否信任度耗尽
            if (GameDataMgr.Instance.IsTrustDepleted)
            {
                Debug.Log("[ElevatorMgr] 信任度耗尽，游戏失败");
                StopElevator();
                EventMgr.Instance.FallIntoAbyss();
            }
        }
        else
        {
            Debug.Log("[ElevatorMgr] 本层安全通过");
        }
    }

    /// <summary>
    /// 停止电梯
    /// </summary>
    public void StopElevator()
    {
        if (!isRunning) return;

        isRunning = false;

        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);
        MonoMgr.Instance.RemoveUpdateListener(UpdateCountdown);
        MonoMgr.Instance.RemoveFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);

        ClearActiveTimer();
        StopCountdown();

        Debug.Log("[ElevatorMgr] 电梯已停止");
    }

    private ElevatorMgr()
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            if (panel != null)
                txtFloor = panel.GetControl<Text>("TxtFloor");
        });
    }
}
