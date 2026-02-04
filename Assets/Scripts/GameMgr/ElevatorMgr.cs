using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    private int waveNum = 0;
    private int levelNum = 0;

    public int CurrentWaveIndex => waveNum;

    private WaveDetailSO currentWave;
    private LevelDetailSO currentLevelDetail;
    private int currentLevelIndex;
    private bool pendingMirrorEvent;

    private int activeTimerId = 0;
    private int delayRandomDisplayTimerId = 0;

    private int previousLevel = 0;
    private int currentLevel = 0;
    private int countdownRemaining;
    private int abnormalCountdownRemaining;

    private bool isRunning = false;

    private bool isAbnormalState = false;
    public bool IsAbnormalState => isAbnormalState;

    private bool isWinPending = false;

    // 状态判断属性
    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        isAbnormalState;

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped && !isFirstDocking;

    private float floorDisplayTimer = 0f;
    private bool isRandomDisplaying = false;

    private bool isAbnormalCountdown = false;

    private bool isGoingUp = true;
    private bool isFirstDocking = true;

    private List<MirrorEventConfig> currentMirrorEvents = new List<MirrorEventConfig>();
    private List<int> mirrorEventTimerIds = new List<int>();

    /// <summary>
    /// 启动电梯
    /// </summary>
    public void StartElevator()
    {
        // ⭐【修复1】启动前先强制执行一次清理，防止上一局脏数据干扰
        StopElevator();

        // 2. ⭐【新增】强制重置 ResourcesMgr 的死亡判定
        // 如果不加这一行，ResourcesMgr 会在下一帧立刻再次杀死电梯
        ResourcesMgr.Instance.ResetRuntimeData();

        // 3. 确保事件管理器也重置
        EventMgr.Instance.ResetState();

        isRunning = true;
        isAbnormalState = false;
        isWinPending = false;
        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;
        isFirstDocking = true;

        GameLevelMgr.Instance.ResetRuntimeCounters();
        PassengerMgr.Instance.ClearAllPassengers();

        waveNum = 0;
        levelNum = 0;
        previousLevel = 0;
        currentLevel = 0;

        EventMgr.Instance.ResetState();

        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);

        ChangeLevelUI(18);

        EnterInitialDepartingState();

        UnityEngine.Debug.Log("[ElevatorMgr] 游戏启动 - 状态重置完成");
    }

    private void EnterInitialDepartingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;

        // ⭐ 防止初始缩放状态卡死
        try { PassengerMgr.Instance.SwitchToMovingPositions(); } catch { }

        UnityEngine.Debug.Log("[ElevatorMgr] 进入初始离开状态");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_doorclose", false);

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;
        StartCountdown(departingDuration);

        // 随机楼层显示逻辑
        int delayMs = ResourcesMgr.Instance.floorRandomDisplayDelay * 1000;
        delayRandomDisplayTimerId = TimerMgr.Instance.CreateTimer(false, delayMs, () =>
        {
            delayRandomDisplayTimerId = 0;
            if (isRunning && currentElevatorState == E_ElevatorState.Departing)
            {
                StartRandomFloorDisplay();
            }
        });

        activeTimerId = TimerMgr.Instance.CreateTimer(false, departingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();
            StopRandomFloorDisplay();
            ClearDelayTimer();

            if (!isRunning) return;

            PrepareNextLevel();
            ChangeLevelUI(currentLevel);
            EnterMovingState();
        });
    }

    private void PrepareNextLevel()
    {
        if (isWinPending)
        {
            previousLevel = currentLevel;
            currentLevel = 1;
            isGoingUp = currentLevel > previousLevel;
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);
            return;
        }

        if (ResourcesMgr.Instance.waveSOList == null || ResourcesMgr.Instance.waveSOList.Count == 0)
        {
            UnityEngine.Debug.LogError("[ElevatorMgr] waveSOList 为空!");
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

        // ⭐【修复2】清理旧的铜镜事件计时器
        currentMirrorEvents.Clear();
        foreach (var id in mirrorEventTimerIds) TimerMgr.Instance.RemoveTimer(id);
        mirrorEventTimerIds.Clear();

        if (currentWave != null && currentWave.mirrorEvents != null)
        {
            foreach (var evt in currentWave.mirrorEvents)
            {
                if (evt.levelIndex == currentLevelIndex)
                    currentMirrorEvents.Add(evt);
            }
        }

        isGoingUp = currentLevel > previousLevel;
    }

    private void StartRandomFloorDisplay()
    {
        StopRandomFloorDisplay();
        isRandomDisplaying = true;
        floorDisplayTimer = 0f;
        MonoMgr.Instance.AddUpdateListener(UpdateRandomFloorDisplay);
    }

    private void UpdateRandomFloorDisplay()
    {
        if (!isRandomDisplaying || !isRunning || currentElevatorState != E_ElevatorState.Departing)
            return;

        floorDisplayTimer += Time.deltaTime * 1000f;
        float interval = ResourcesMgr.Instance.floorRandomDisplayInterval;
        if (floorDisplayTimer >= interval)
        {
            floorDisplayTimer = 0f;
            ChangeLevelUI(Random.Range(2, 19));
        }
    }

    private void StopRandomFloorDisplay()
    {
        if (isRandomDisplaying)
        {
            isRandomDisplaying = false;
            MonoMgr.Instance.RemoveUpdateListener(UpdateRandomFloorDisplay);
        }
    }

    private void ClearDelayTimer()
    {
        if (delayRandomDisplayTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(delayRandomDisplayTimerId);
            delayRandomDisplayTimerId = 0;
        }
    }

    public void ChangeLevelUI(int level)
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            if (panel == null) return;
            panel.TrySetFloor(level);
        });
    }

    private void ClearActiveTimer()
    {
        if (activeTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(activeTimerId);
            activeTimerId = 0;
        }
    }

    private void OnUnnormalEventStart()
    {
        UnityEngine.Debug.Log("[ElevatorMgr] 异常事件开始");
        isAbnormalState = true;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_amb_abnormal");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, true);

        isAbnormalCountdown = true;
        abnormalCountdownRemaining = ResourcesMgr.Instance.abnormalEventTimeout * 1000;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, abnormalCountdownRemaining / 1000);
    }

    private void OnUnnormalEventResolved()
    {
        UnityEngine.Debug.Log("[ElevatorMgr] 异常事件解决");
        isAbnormalState = false;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);

        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;

        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
        ChangeLevelUI(currentLevel);
    }

    #region 倒计时逻辑

    private void StartCountdown(int durationMs)
    {
        countdownRemaining = durationMs;
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
        }
    }

    private void StopCountdown()
    {
        countdownRemaining = 0;
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);
        }
    }

    private void UpdateCountdown()
    {
        if (countdownRemaining > 0)
        {
            countdownRemaining -= (int)(Time.deltaTime * 1000);
            if (countdownRemaining < 0) countdownRemaining = 0;
        }

        if (isAbnormalCountdown && abnormalCountdownRemaining > 0)
        {
            int previousSeconds = abnormalCountdownRemaining / 1000;
            abnormalCountdownRemaining -= (int)(Time.deltaTime * 1000);
            int currentSeconds = Mathf.Max(0, abnormalCountdownRemaining / 1000);

            if (currentSeconds != previousSeconds)
            {
                EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, currentSeconds);
            }

            if (abnormalCountdownRemaining <= 0)
            {
                UnityEngine.Debug.Log("[ElevatorMgr] 异常超时，触发失败");
                isAbnormalCountdown = false;
                StopElevator();
                EventMgr.Instance.FallIntoAbyss();
            }
            return;
        }

        if (!isAbnormalCountdown && countdownRemaining > 0)
        {
            int previousSeconds = (countdownRemaining + (int)(Time.deltaTime * 1000)) / 1000;
            int currentSeconds = countdownRemaining / 1000;

            if (currentSeconds != previousSeconds)
            {
                EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, currentSeconds);
            }
        }
    }

    #endregion

    private void CheckResults()
    {
        var list = PassengerMgr.Instance.passengerList;
        int totalSpawnPoints = ResourcesMgr.Instance.globalPassengerSpawnPoints.Count;

        int totalPassengers = 0;
        int ghostCount = 0;

        List<Passenger> ghostsToSettle = new List<Passenger>();

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
                    if (!p.isNewThisRound && !p.hasDamageSettled)
                    {
                        ghostsToSettle.Add(p);
                    }
                }
            }
        }

        PassengerMgr.Instance.ClearAllNewThisRoundMarks();

        if (totalPassengers >= totalSpawnPoints && ghostCount == 0)
        {
            isWinPending = true;
            if (isAbnormalState)
            {
                isAbnormalState = false;
                EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
            }
            return;
        }

        if (ghostsToSettle.Count > 0)
        {
            foreach (var ghost in ghostsToSettle) ghost.MarkDamageSettled();
            GameDataMgr.Instance.SubTrustValue(ghostsToSettle.Count);
            if (GameDataMgr.Instance.IsTrustDepleted)
            {
                StopElevator();
                EventMgr.Instance.FallIntoAbyss();
                return;
            }
        }
    }

    /// <summary>
    /// 停止电梯（彻底清理状态）
    /// </summary>
    public void StopElevator()
    {
        UnityEngine.Debug.LogError($"[侦探模式] 谁调用了 StopElevator？堆栈追踪:\n{System.Environment.StackTrace}");

        // ⭐【修复3】去掉了 if (!isRunning) return; 确保清理逻辑总是执行！
        // 这是修复“死亡后状态未重置”的关键

        isRunning = false;

        // 强制重置标记
        isAbnormalState = false;
        isAbnormalCountdown = false;
        isWinPending = false;
        isFirstDocking = true;
        abnormalCountdownRemaining = 0;
        countdownRemaining = 0;

        // 移除监听
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);
        MonoMgr.Instance.RemoveUpdateListener(UpdateCountdown);
        MonoMgr.Instance.RemoveFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);

        // 清理计时器
        ClearActiveTimer();
        StopRandomFloorDisplay();
        ClearDelayTimer();

        // 清理铜镜事件
        if (mirrorEventTimerIds != null)
        {
            foreach (var timerId in mirrorEventTimerIds)
                TimerMgr.Instance.RemoveTimer(timerId);
            mirrorEventTimerIds.Clear();
        }

        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);

        UnityEngine.Debug.Log("[ElevatorMgr] 电梯已停止并强制清理所有数据");
    }

    public void EnterMovingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Moving;
        UnityEngine.Debug.Log("[ElevatorMgr] 正在运行...");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);

        var config = ResourcesMgr.Instance;
        int minTime = config.elevatorMovingTimeMin * 1000;
        int maxTime = config.elevatorMovingTimeMax * 1000;
        int movingDuration = Random.Range(minTime, maxTime + 1);

        int arrivingDuration = config.elevatorArrivingTime * 1000;
        int totalDuration = movingDuration + arrivingDuration;
        StartCountdown(totalDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            activeTimerId = 0;
            if (!isRunning) return;

            if (!isWinPending)
            {
                // 触发异常事件判断
                bool triggerAbnormal = Random.value < config.abnormalEventChance;
                if (triggerAbnormal && !isAbnormalState)
                {
                    EventMgr.Instance.UnnormalEvent();
                }
            }

            EnterArrivingState();
        });
    }

    public void EnterArrivingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Arriving;
        UnityEngine.Debug.Log("[ElevatorMgr] 即将到达...");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);

        int arrivingDuration = ResourcesMgr.Instance.elevatorArrivingTime * 1000;

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            // ⭐⭐⭐【新增调试代码】⭐⭐⭐
            if (!isRunning)
            {
                UnityEngine.Debug.LogError("[侦探模式] 抓到了！倒计时结束时，isRunning 已经被设为 false！无法进入停靠状态。请看上面的 StopElevator 报错是谁干的。");
                return;
            }

            UnityEngine.Debug.Log("[侦探模式] 倒计时结束，准备进入停靠状态...");

            if (!isRunning) return;

            // ⭐【修复4】使用 Try-Catch 防止音效缺失导致卡死
            try
            {
                string beepSound = isAbnormalState
                    ? "Music/26GGJsound/elevator_beep_abnormal"
                    : "Music/26GGJsound/elevator_beep";
                MusicMgr.Instance.PlaySound(beepSound, false);
            }
            catch { }

            EnterStoppedState();
        });
    }

    public void EnterStoppedState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        // 1. 先切换状态枚举
        currentElevatorState = E_ElevatorState.Stopped;

        UnityEngine.Debug.Log($"[ElevatorMgr] 进入停靠状态 (异常状态: {isAbnormalState})");

        // ⭐⭐⭐【防弹修复 1】UI和乘客逻辑保护 ⭐⭐⭐
        try
        {
            // 如果 GamePanel 还没加载好，这里可能会空引用报错，必须 try 住
            PassengerMgr.Instance.SwitchToDockingPositions();

            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, true);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[ElevatorMgr] 停靠时UI/乘客出错(已忽略，继续流程): {e.Message}");
        }

        // ⭐⭐⭐【防弹修复 2】音效逻辑保护 (最可能的崩溃点) ⭐⭐⭐
        try
        {
            string doorOpenSound = isAbnormalState
                ? "Music/26GGJsound/elevator_dooropen_abnormal"
                : "Music/26GGJsound/elevator_dooropen";

            MusicMgr.Instance.PlaySound(doorOpenSound, false);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[ElevatorMgr] 播放开门音效失败(可能是文件缺失): {e.Message}");
        }

        // 胜利判断
        if (isWinPending)
        {
            // ... (胜利逻辑保持不变)
            UnityEngine.Debug.Log("[ElevatorMgr] 触发胜利");
            PassengerMgr.Instance.ClearAllPassengers();
            StopElevator();
            MusicMgr.Instance.StopBKMusic();
            int delayMs = ResourcesMgr.Instance.winPanelDelay * 1000;
            TimerMgr.Instance.CreateTimer(false, delayMs, () =>
            {
                UIMgr.Instance.ShowPanel<GameOverPanel>(E_UILayer.Top, (panel) => panel.ShowResult(true));
            });
            return;
        }

        TimerMgr.Instance.CreateTimer(false, 500, () =>
        {
            // ⭐⭐⭐【防弹修复 3】生成乘客保护 ⭐⭐⭐
            try
            {
                PassengerMgr.Instance.SpawnWave();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[ElevatorMgr] 生成乘客失败: {e.Message}");
            }
        });


        // ⭐⭐⭐ 核心：无论上面报不报错，这行代码必须执行，否则电梯就卡死了！ ⭐⭐⭐
        int dockingDuration = isFirstDocking
            ? ResourcesMgr.Instance.firstDockingTime * 1000
            : ResourcesMgr.Instance.elevatorDockingTime * 1000;

        StartCountdown(dockingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, dockingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;
            if (isFirstDocking) isFirstDocking = false;
            EnterDepartingState();
        });
    }

    public void EnterDepartingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;
        UnityEngine.Debug.Log("[ElevatorMgr] 正在离开...");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        // 安全调用
        try { PassengerMgr.Instance.SwitchToMovingPositions(); } catch { }

        string doorCloseSound = isAbnormalState
            ? "Music/26GGJsound/elevator_doorclose_abnormal"
            : "Music/26GGJsound/elevator_doorclose";
        try { MusicMgr.Instance.PlaySound(doorCloseSound, false); } catch { }

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;
        StartCountdown(departingDuration);

        StartRandomFloorDisplay();
        ScheduleMirrorEvents();

        activeTimerId = TimerMgr.Instance.CreateTimer(false, departingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();
            StopRandomFloorDisplay();

            if (!isRunning) return;

            if (!isWinPending) CheckResults();

            if (isRunning)
            {
                PrepareNextLevel();
                ChangeLevelUI(currentLevel);
                EnterMovingState();
            }
        });
    }

    private ElevatorMgr() { }

    private void ScheduleMirrorEvents()
    {
        foreach (var timerId in mirrorEventTimerIds) TimerMgr.Instance.RemoveTimer(timerId);
        mirrorEventTimerIds.Clear();

        foreach (var evt in currentMirrorEvents)
        {
            float delay = Mathf.Max(0, evt.triggerDelay);
            int timerId = TimerMgr.Instance.CreateTimer(false, (int)(delay * 1000), () =>
            {
                if (!isRunning || currentElevatorState != E_ElevatorState.Departing) return;
                UIMgr.Instance.ShowPanel<MirrorPanel>(E_UILayer.Top);
            });
            mirrorEventTimerIds.Add(timerId);
        }
    }

    public bool ShouldSpawnSpecialPassenger
    {
        get
        {
            if (currentWave == null) return false;

            // 检查当前层索引是否等于配置的生成索引
            // 注意：这里会引用你在第一步中添加的变量 specialPassengerSpawnIndex
            return currentLevelIndex == currentWave.specialPassengerSpawnIndex;
        }
    }
}