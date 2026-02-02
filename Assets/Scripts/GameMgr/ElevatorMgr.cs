using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum E_ElevatorState
{
    Moving,
    Arriving,
    Stopped,
    Departing,
    Abnormal
}

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

    private int activeTimerId = 0;

    private int previousLevel = 0;
    private int currentLevel = 0;
    private int countdownRemaining;

    private bool isRunning = false;

    /// <summary>
    /// 是否处于异常状态（不影响电梯流程，只影响音效和UI）
    /// </summary>
    private bool isAbnormalState = false;
    public bool IsAbnormalState => isAbnormalState;

    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        isAbnormalState;

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;

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
        isAbnormalState = false;

        GameLevelMgr.Instance.ResetRuntimeCounters();

        waveNum = 0;
        levelNum = 0;
        previousLevel = 0;
        currentLevel = 0;

        // ? 监听异常事件
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        // ? 播放正常背景音乐
       // MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
        
        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);

        EnterMovingState();
    }

    private void ClearActiveTimer()
    {
        if (activeTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(activeTimerId);
            activeTimerId = 0;
        }
    }

    /// <summary>
    /// 异常事件开始（不暂停电梯，只改变状态）
    /// </summary>
    private void OnUnnormalEventStart()
    {
        Debug.Log("[ElevatorMgr] 进入异常状态");
        isAbnormalState = true;

        // ? 切换为异常背景音乐
        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_amb_abnormal");

        // ? 通知UI更新（时间文本变红）
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, true);
    }

    /// <summary>
    /// 异常事件解决
    /// </summary>
    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常状态结束");
        isAbnormalState = false;

        // ? 恢复正常背景音乐
        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_normal");

        // ? 通知UI更新（时间文本恢复）
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
    }

    public void EnterMovingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("[ElevatorMgr] 电梯开始运行...");

        var config = ResourcesMgr.Instance;
        int minTime = config.elevatorMovingTimeMin * 1000;
        int maxTime = config.elevatorMovingTimeMax * 1000;
        int movingDuration = Random.Range(minTime, maxTime + 1);

        StartCountdown(movingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            // ? 触发异常事件（不影响流程，继续进入到达状态）
            bool triggerAbnormal = Random.value < config.abnormalEventChance;
            if (triggerAbnormal && !isAbnormalState)
            {
                EventMgr.Instance.UnnormalEvent();
            }

            EnterArrivingState();
        });
    }

    public void EnterArrivingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Arriving;
        Debug.Log("[ElevatorMgr] 电梯即将到达...");

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

        currentLevelIndex = levelNum;
        currentLevelDetail = currentWave.levelDetails[currentLevelIndex];
        GameLevelMgr.Instance.currentLevelDetail = currentLevelDetail;
        levelNum++;

        pendingMirrorEvent = currentWave.createMirrorLevelIndex != null &&
                             currentWave.createMirrorLevelIndex.Contains(currentLevelIndex);

        previousLevel = currentLevel;
        currentLevel = currentLevelDetail.level;

        bool isGoingUp = currentLevel > previousLevel;
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);
        Debug.Log($"[ElevatorMgr] 电梯方向: {(isGoingUp ? "上升" : "下降")} ({previousLevel} -> {currentLevel})");

        int arrivingDuration = ResourcesMgr.Instance.elevatorArrivingTime * 1000;
        StartCountdown(arrivingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            // ? 根据异常状态播放不同音效
            string beepSound = isAbnormalState 
                ? "Music/26GGJsound/elevator_beep_abnormal" 
                : "Music/26GGJsound/elevator_beep";
            MusicMgr.Instance.PlaySound(beepSound, false);

            ChangeLevelUI(currentLevelDetail.level);
            EnterStoppedState();
        }, 400,
        () =>
        {
            if (isRunning)
                ChangeLevelUI(Random.Range(2, 18));
        });
    }

    public void EnterStoppedState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Stopped;
        Debug.Log("[ElevatorMgr] 电梯已停靠");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, true);

        // ? 根据异常状态播放不同开门音效
        string doorOpenSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_dooropen_abnormal" 
            : "Music/26GGJsound/elevator_dooropen";
        MusicMgr.Instance.PlaySound(doorOpenSound, false);

        PassengerMgr.Instance.SpawnWave();

        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        if (canTriggerMirror)
        {
            UIMgr.Instance.ShowPanel<MirrorPanel>(E_UILayer.Top);
            Debug.Log("[ElevatorMgr] 触发铜镜事件");
        }
        pendingMirrorEvent = false;

        int dockingDuration = ResourcesMgr.Instance.elevatorDockingTime * 1000;
        StartCountdown(dockingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, dockingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            EnterDepartingState();
        });
    }

    public void EnterDepartingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;
        Debug.Log("[ElevatorMgr] 电梯正在离开...");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        // ? 根据异常状态播放不同关门音效
        string doorCloseSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_doorclose_abnormal" 
            : "Music/26GGJsound/elevator_doorclose";
        MusicMgr.Instance.PlaySound(doorCloseSound, false);

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;

        activeTimerId = TimerMgr.Instance.CreateTimer(false, departingDuration, () =>
        {
            activeTimerId = 0;

            if (!isRunning) return;

            CheckResults();

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

    private void CheckResults()
    {
        var list = PassengerMgr.Instance.passengerList;
        int totalSpawnPoints = ResourcesMgr.Instance.globalPassengerSpawnPoints.Count;
        
        // 统计当前乘客情况
        int totalPassengers = 0;
        int ghostCount = 0;
        int normalOrSpecialCount = 0;
        
        if (list != null)
        {
            foreach (var p in list)
            {
                if (p == null || p.passengerInfo == null || !p.gameObject.activeSelf)
                    continue;
                
                totalPassengers++;
                
                if (p.passengerInfo.isGhost)
                {
                    ghostCount++;
                }
                else
                {
                    // 普通乘客或特殊乘客
                    normalOrSpecialCount++;
                }
            }
        }

        Debug.Log($"[ElevatorMgr] 结算 - 总点位:{totalSpawnPoints}, 当前乘客:{totalPassengers}, 鬼魂:{ghostCount}, 普通/特殊:{normalOrSpecialCount}");

        // ? 胜利条件：所有点位都有乘客，且没有鬼魂
        if (totalPassengers >= totalSpawnPoints && ghostCount == 0)
        {
            Debug.Log("[ElevatorMgr] ?? 电梯满载且无鬼魂，游戏胜利！");
            StopElevator();
            MusicMgr.Instance.StopBKMusic();
            UIMgr.Instance.ShowPanel<GameOverPanel>(E_UILayer.Top, (panel) =>
            {
                panel.ShowResult(true);  // 显示胜利
            });
            return;
        }

        // ? 失败条件：有鬼魂未被驱逐，扣除信任度
        if (ghostCount > 0)
        {
            GameDataMgr.Instance.SubTrustValue(ghostCount);
            Debug.Log($"[ElevatorMgr] 本层有 {ghostCount} 个鬼魂未被驱逐，扣除信任度");

            if (GameDataMgr.Instance.IsTrustDepleted)
            {
                Debug.Log("[ElevatorMgr] 信任度耗尽，游戏失败");
                StopElevator();
                EventMgr.Instance.FallIntoAbyss();
                return;
            }
        }
        else
        {
            Debug.Log("[ElevatorMgr] 本层安全通过");
        }
    }

    public void StopElevator()
    {
        if (!isRunning) return;

        isRunning = false;
        isAbnormalState = false;

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
