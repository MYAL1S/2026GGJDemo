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
    private int countdownRemaining;

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

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;

    private float floorDisplayTimer = 0f;
    private bool isRandomDisplaying = false;

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
        isWinPending = false;  // ⭐ 重置胜利状态

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
        
        // ⭐ 添加调试按键监听
        MonoMgr.Instance.AddUpdateListener(DebugInput);

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
        // ⭐ 如果胜利待定，目标楼层设为1
        if (isWinPending)
        {
            previousLevel = currentLevel;
            currentLevel = 1;
            
            bool isGoingUp = currentLevel > previousLevel;
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

        bool isGoingUp2 = currentLevel > previousLevel;
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp2);
        Debug.Log($"[ElevatorMgr] 下一楼层: {currentLevel} ({(isGoingUp2 ? "上升" : "下降")})");
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
    }

    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常状态结束");
        isAbnormalState = false;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
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
        
        // ⭐ 移动+到达合并显示（移动时间 + 到达时间）
        int arrivingDuration = config.elevatorArrivingTime * 1000;
        int totalDuration = movingDuration + arrivingDuration;
        StartCountdown(totalDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            activeTimerId = 0;
            // ⭐ 不停止倒计时，让它继续倒数

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

        int arrivingDuration = ResourcesMgr.Instance.elevatorArrivingTime * 1000;
        
        // ⭐ 到达状态继续显示倒计时（不重新开始，继续从移动状态的剩余时间倒数）

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();  // ⭐ 到达结束时停止倒计时

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
        Debug.Log("[ElevatorMgr] 电梯已停靠");

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, true);

        string doorOpenSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_dooropen_abnormal" 
            : "Music/26GGJsound/elevator_dooropen";
        MusicMgr.Instance.PlaySound(doorOpenSound, false);

        // ⭐ 如果是胜利状态，清空乘客并延迟显示胜利面板
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

        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        if (canTriggerMirror)
        {
            UIMgr.Instance.ShowPanel<MirrorPanel>(E_UILayer.Top);
            Debug.Log("[ElevatorMgr] 触发铜镜事件");
        }
        pendingMirrorEvent = false;

        int dockingDuration = ResourcesMgr.Instance.elevatorDockingTime * 1000;
        
        // ⭐ 停靠状态显示倒计时
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

        string doorCloseSound = isAbnormalState 
            ? "Music/26GGJsound/elevator_doorclose_abnormal" 
            : "Music/26GGJsound/elevator_doorclose";
        MusicMgr.Instance.PlaySound(doorCloseSound, false);

        int departingDuration = ResourcesMgr.Instance.elevatorDepartingTime * 1000;
        
        // ⭐ 离开状态显示倒计时
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

    private void CheckResults()
    {
        var list = PassengerMgr.Instance.passengerList;
        int totalSpawnPoints = ResourcesMgr.Instance.globalPassengerSpawnPoints.Count;
        
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
                    ghostCount++;
                else
                    normalOrSpecialCount++;
            }
        }

        Debug.Log($"[ElevatorMgr] 结算 - 总点位:{totalSpawnPoints}, 当前乘客:{totalPassengers}, 鬼魂:{ghostCount}, 普通/特殊:{normalOrSpecialCount}");

        // ⭐ 胜利条件：所有点位都有乘客，且没有鬼魂
        if (totalPassengers >= totalSpawnPoints && ghostCount == 0)
        {
            Debug.Log("[ElevatorMgr] 🎉 电梯满载且无鬼魂，触发胜利流程！");
            
            // ⭐ 设置胜利待定状态，不立即弹出面板
            isWinPending = true;
            
            // ⭐ 解决异常状态（如果有）
            if (isAbnormalState)
            {
                isAbnormalState = false;
                EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
            }
            
            return;
        }

        // 失败条件：有鬼魂未被驱逐，扣除信任度
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

    /// <summary>
    /// ⭐ 调试：强制触发胜利流程
    /// </summary>
    public void TriggerWinForDebug()
    {
        if (!isRunning || isWinPending)
        {
            Debug.LogWarning("[ElevatorMgr] 无法触发胜利：电梯未运行或已在胜利流程中");
            return;
        }

        Debug.Log("[ElevatorMgr] ⭐ 调试：强制触发胜利流程！");
        
        // 设置胜利待定状态
        isWinPending = true;
        
        // 解决异常状态（如果有）
        if (isAbnormalState)
        {
            isAbnormalState = false;
            MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);
        }
    }

    /// <summary>
    /// ⭐ 调试：强制触发失败流程
    /// </summary>
    public void TriggerLoseForDebug()
    {
        if (!isRunning)
        {
            Debug.LogWarning("[ElevatorMgr] 无法触发失败：电梯未运行");
            return;
        }

        Debug.Log("[ElevatorMgr] ⭐ 调试：强制触发失败流程！");
        
        StopElevator();
        EventMgr.Instance.FallIntoAbyss();
    }

    public void StopElevator()
    {
        if (!isRunning) return;

        isRunning = false;
        isAbnormalState = false;
        isWinPending = false;  // ⭐ 重置胜利状态

        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);
        MonoMgr.Instance.RemoveUpdateListener(UpdateCountdown);
        MonoMgr.Instance.RemoveFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        
        // ⭐ 移除调试监听
        MonoMgr.Instance.RemoveUpdateListener(DebugInput);

        ClearActiveTimer();
        StopCountdown();
        StopRandomFloorDisplay();
        ClearDelayTimer();

        Debug.Log("[ElevatorMgr] 电梯已停止");
    }

    /// <summary>
    /// ⭐ 调试输入检测
    /// </summary>
    private void DebugInput()
    {
        if (!isRunning) return;

        // 按 V 键触发胜利流程
        if (Input.GetKeyDown(KeyCode.V))
        {
            TriggerWinForDebug();
        }
        
        // 按 L 键触发失败流程（可选）
        if (Input.GetKeyDown(KeyCode.L))
        {
            TriggerLoseForDebug();
        }
    }

    private ElevatorMgr()
    {
    }
}
