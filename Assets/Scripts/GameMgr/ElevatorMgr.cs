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
    
    private int delayRandomDisplayTimerId = 0;

    private int previousLevel = 0;
    private int currentLevel = 0;
    private int countdownRemaining;           // 电梯正常倒计时
    private int abnormalCountdownRemaining;   // 异常事件倒计时

    private bool isRunning = false;

    private bool isAbnormalState = false;
    public bool IsAbnormalState => isAbnormalState;

    // ⭐ 胜利状态标记
    private bool isWinPending = false;

    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        isAbnormalState;

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped && !isFirstDocking;

    private float floorDisplayTimer = 0f;
    private bool isRandomDisplaying = false;

    // ⭐ 异常事件相关
    private bool isAbnormalCountdown = false;  // 是否显示异常倒计时

    // 添加字段
    private bool isGoingUp = true;  // ⭐ 当前电梯方向
    private bool isFirstDocking = true;  // ⭐ 是否是第一次停靠

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
        isWinPending = false;
        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;
        isFirstDocking = true;  // ⭐ 重置第一次停靠标记

        GameLevelMgr.Instance.ResetRuntimeCounters();
        PassengerMgr.Instance.ClearAllPassengers();

        waveNum = 0;
        levelNum = 0;
        previousLevel = 0;
        currentLevel = 0;

        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);
        
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            if (panel != null)
            {
                txtFloor = panel.GetControl<Text>("TxtFloor");
                ChangeLevelUI(18);
            }
            
            EnterInitialDepartingState();
        });
        
        Debug.Log("[ElevatorMgr] 电梯已启动，游戏开始");
    }

    /// <summary>
    /// 游戏开始时的初始离开状态
    /// </summary>
    private void EnterInitialDepartingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;
        
        Debug.Log("[ElevatorMgr] 游戏开始 - 电梯正在离开初始楼层...");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, false);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_doorclose", false);

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;
        
        // ⭐ 初始离开状态显示倒计时
        StartCountdown(departingDuration);
        
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

    /// <summary>
    /// 预先计算下一层信息
    /// </summary>
    private void PrepareNextLevel()
    {
        // 如果胜利待定，目标楼层设为1
        if (isWinPending)
        {
            previousLevel = currentLevel;
            currentLevel = 1;
            
            isGoingUp = currentLevel > previousLevel;
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);
            Debug.Log($"[ElevatorMgr] 胜利！前往1层 ({(isGoingUp ? "上升" : "下降")})");
            return;
        }

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

        // ⭐ 计算方向
        isGoingUp = currentLevel > previousLevel;
        
        Debug.Log($"[ElevatorMgr] 下一楼层: {currentLevel} ({(isGoingUp ? "上升" : "下降")}) [从{previousLevel}层]");
    }

    private void StartRandomFloorDisplay()
    {
        StopRandomFloorDisplay();
        isRandomDisplaying = true;
        floorDisplayTimer = 0f;
        MonoMgr.Instance.AddUpdateListener(UpdateRandomFloorDisplay);
        Debug.Log("[ElevatorMgr] 开始随机显示楼层");
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
            Debug.Log("[ElevatorMgr] 停止随机显示楼层");
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

    private void RefreshUIReferences()
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            if (panel != null)
                txtFloor = panel.GetControl<Text>("TxtFloor");
        });
    }

    public void ChangeLevelUI(int level)
    {
        if (txtFloor != null)
            txtFloor.text = level.ToString();
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
        Debug.Log("[ElevatorMgr] 进入异常状态");
        isAbnormalState = true;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_amb_abnormal");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, true);
        
        // ⭐ 切换到异常事件倒计时
        isAbnormalCountdown = true;
        
        // ⭐ 开始异常事件倒计时
        abnormalCountdownRemaining = ResourcesMgr.Instance.abnormalEventTimeout * 1000;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, abnormalCountdownRemaining / 1000);
    }

    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常状态结束");
        isAbnormalState = false;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
        
        // ⭐ 恢复显示电梯正常倒计时
        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;
        
        // ⭐ 立即更新显示当前电梯倒计时
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
    }

    #region 倒计时逻辑

    private void StartCountdown(int durationMs)
    {
        countdownRemaining = durationMs;
        
        // ⭐ 只有非异常状态才更新显示
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
        }
    }

    private void StopCountdown()
    {
        countdownRemaining = 0;
        
        // ⭐ 只有非异常状态才更新显示
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);
        }
    }

    private void UpdateCountdown()
    {
        // ⭐ 更新电梯正常倒计时（始终在后台运行）
        if (countdownRemaining > 0)
        {
            countdownRemaining -= (int)(Time.deltaTime * 1000);
            if (countdownRemaining < 0) countdownRemaining = 0;
        }
        
        // ⭐ 更新异常事件倒计时
        if (isAbnormalCountdown && abnormalCountdownRemaining > 0)
        {
            int previousSeconds = abnormalCountdownRemaining / 1000;
            abnormalCountdownRemaining -= (int)(Time.deltaTime * 1000);
            int currentSeconds = Mathf.Max(0, abnormalCountdownRemaining / 1000);

            if (currentSeconds != previousSeconds)
            {
                EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, currentSeconds);
            }
            
            // ⭐ 异常倒计时结束时，触发失败
            if (abnormalCountdownRemaining <= 0)
            {
                Debug.Log("[ElevatorMgr] 异常事件超时，游戏失败！");
                isAbnormalCountdown = false;
                StopElevator();
                EventMgr.Instance.FallIntoAbyss();
            }
            return;
        }
        
        // ⭐ 非异常状态，更新电梯倒计时显示
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
        int normalOrSpecialCount = 0;
        
        // ⭐ 需要结算伤害的鬼魂列表
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
                    
                    // ⭐ 检查是否需要结算伤害：
                    // 1. 不是本轮新进入的乘客
                    // 2. 还没有结算过伤害
                    if (!p.isNewThisRound && !p.hasDamageSettled)
                    {
                        ghostsToSettle.Add(p);
                    }
                }
                else
                {
                    normalOrSpecialCount++;
                }
            }
        }

        // ⭐ 清除所有乘客的"本轮新进入"标记，为下一轮做准备
        PassengerMgr.Instance.ClearAllNewThisRoundMarks();

        Debug.Log($"[ElevatorMgr] 结算 - 总点位:{totalSpawnPoints}, 当前乘客:{totalPassengers}, 鬼魂总数:{ghostCount}, 需结算鬼魂:{ghostsToSettle.Count}, 普通/特殊:{normalOrSpecialCount}");

        // ⭐ 胜利条件：所有点位都有乘客，且没有鬼魂
        if (totalPassengers >= totalSpawnPoints && ghostCount == 0)
        {
            Debug.Log("[ElevatorMgr] 🎉 电梯满载且无鬼魂，触发胜利流程！");
            
            isWinPending = true;
            
            if (isAbnormalState)
            {
                isAbnormalState = false;
                EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
            }
            
            return;
        }

        // ⭐ 失败条件：有需要结算的鬼魂，扣除信任度
        if (ghostsToSettle.Count > 0)
        {
            // 标记这些鬼魂已结算
            foreach (var ghost in ghostsToSettle)
            {
                ghost.MarkDamageSettled();
            }
            
            GameDataMgr.Instance.SubTrustValue(ghostsToSettle.Count);
            Debug.Log($"[ElevatorMgr] 本层有 {ghostsToSettle.Count} 个鬼魂未被驱逐，扣除信任度");

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
            Debug.Log("[ElevatorMgr] 本层安全通过（无需结算的鬼魂）");
        }
    }

    public void StopElevator()
    {
        if (!isRunning) return;

        isRunning = false;
        isAbnormalState = false;
        isWinPending = false;
        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;
        isFirstDocking = true;  // ⭐ 重置

        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);
        MonoMgr.Instance.RemoveUpdateListener(UpdateCountdown);
        MonoMgr.Instance.RemoveFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        
        ClearActiveTimer();
        
        countdownRemaining = 0;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);
        
        StopRandomFloorDisplay();
        ClearDelayTimer();

        Debug.Log("[ElevatorMgr] 电梯已停止");
    }

    public void EnterMovingState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("[ElevatorMgr] 电梯开始运行...");

        // ⭐ 移动状态更新方向箭头
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
        Debug.Log("[ElevatorMgr] 电梯即将到达...");

        // ⭐ 到达状态保持方向箭头
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);

        int arrivingDuration = ResourcesMgr.Instance.elevatorArrivingTime * 1000;

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

            string beepSound = isAbnormalState 
                ? "Music/26GGJsound/elevator_beep_abnormal" 
                : "Music/26GGJsound/elevator_beep";
            MusicMgr.Instance.PlaySound(beepSound, false);

            EnterStoppedState();
        });
    }

    public void EnterStoppedState()
    {
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Stopped;
        
        // ⭐ 判断是否是第一次停靠
        if (isFirstDocking)
        {
            Debug.Log("[ElevatorMgr] 电梯首次停靠（无法与乘客交互）");
        }
        else
        {
            Debug.Log("[ElevatorMgr] 电梯已停靠");
        }

        // ⭐ 停靠状态：两个箭头都不亮
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, true);

        string doorOpenSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_dooropen_abnormal" 
            : "Music/26GGJsound/elevator_dooropen";
        MusicMgr.Instance.PlaySound(doorOpenSound, false);

        if (isWinPending)
        {
            Debug.Log("[ElevatorMgr] 🎉 到达1层，游戏胜利！");
            
            PassengerMgr.Instance.ClearAllPassengers();
            StopElevator();
            MusicMgr.Instance.StopBKMusic();
            
            int delayMs = ResourcesMgr.Instance.winPanelDelay * 1000;
            TimerMgr.Instance.CreateTimer(false, delayMs, () =>
            {
                UIMgr.Instance.ShowPanel<GameOverPanel>(E_UILayer.Top, (panel) =>
                {
                    panel.ShowResult(true);
                });
            });
            return;
        }

        PassengerMgr.Instance.SpawnWave();

        // ⭐ 只有非第一次停靠才触发铜镜事件
        if (!isFirstDocking)
        {
            bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
            if (canTriggerMirror)
            {
                UIMgr.Instance.ShowPanel<MirrorPanel>(E_UILayer.Top);
                Debug.Log("[ElevatorMgr] 触发铜镜事件");
            }
        }
        pendingMirrorEvent = false;

        // ⭐ 根据是否第一次停靠使用不同的停靠时间
        int dockingDuration = isFirstDocking 
            ? ResourcesMgr.Instance.firstDockingTime * 1000 
            : ResourcesMgr.Instance.elevatorDockingTime * 1000;
    
        StartCountdown(dockingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, dockingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;
            
            // ⭐ 第一次停靠结束后，设置为非第一次
            if (isFirstDocking)
            {
                isFirstDocking = false;
                Debug.Log("[ElevatorMgr] 第一次停靠结束，后续可与乘客交互");
            }

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
        
        // ⭐ 离开状态：两个箭头都亮
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        string doorCloseSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_doorclose_abnormal" 
            : "Music/26GGJsound/elevator_doorclose";
        MusicMgr.Instance.PlaySound(doorCloseSound, false);

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;
        StartCountdown(departingDuration);
        
        StartRandomFloorDisplay();

        activeTimerId = TimerMgr.Instance.CreateTimer(false, departingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();
            
            StopRandomFloorDisplay();

            if (!isRunning) return;

            if (!isWinPending)
            {
                CheckResults();
            }

            if (isRunning)
            {
                PrepareNextLevel();
                ChangeLevelUI(currentLevel);
                EnterMovingState();
            }
        });
    }

    // 在类的最后，确保有这个私有构造函数
    private ElevatorMgr()
    {
        // 私有构造函数，防止外部实例化
    }
}
