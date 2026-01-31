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

    // 缓存当前波/层信息
    private WaveDetailSO currentWave;
    private LevelDetailSO currentLevelDetail;
    private int currentLevelIndex;
    private bool pendingMirrorEvent;

    private E_ElevatorState stateBeforeAbnormal;
    private int movingTimerId;

    /// <summary>
    /// 上一次的楼层（用于判断方向）
    /// </summary>
    private int previousLevel = 0;

    /// <summary>
    /// 当前楼层
    /// </summary>
    private int currentLevel = 0;

    /// <summary>
    /// 当前倒计时的定时器ID
    /// </summary>
    private int countdownTimerId;

    /// <summary>
    /// 当前倒计时剩余时间（毫秒）
    /// </summary>
    private int countdownRemaining;

    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        currentElevatorState == E_ElevatorState.Abnormal;

    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;
    public bool IsInAbnormalState => currentElevatorState == E_ElevatorState.Abnormal;

    /// <summary>
    /// 开启电梯
    /// </summary>
    public void StartElevator()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience");
        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);

        // 注册倒计时更新
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);

        EnterMovingState();
    }

    /// <summary>
    /// 电梯进入移动状态
    /// </summary>
    public void EnterMovingState()
    {
        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("[ElevatorMgr] 电梯正在运行...");

        // 开始移动状态倒计时
        int movingDuration = Random.Range(5, 10) * 1000;
        StartCountdown(movingDuration);

        movingTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            StopCountdown();

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

        // 暂停倒计时
        StopCountdown();

        if (movingTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(movingTimerId);
            movingTimerId = 0;
        }
    }

    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常事件已解决，电梯恢复运行");
        EnterArrivingState();
    }

    /// <summary>
    /// 电梯进入到达状态
    /// </summary>
    public void EnterArrivingState()
    {
        currentElevatorState = E_ElevatorState.Arriving;
        Debug.Log("[ElevatorMgr] 电梯即将到达...");

        // 选波
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

        TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            StopCountdown();
            MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_beep", false);
            ChangeLevelUI(currentLevelDetail.level);
            EnterStoppedState();
        }, 400,
        () =>
        {
            ChangeLevelUI(Random.Range(2, 18));
        });
    }

    /// <summary>
    /// 电梯进入停靠状态
    /// </summary>
    public void EnterStoppedState()
    {
        currentElevatorState = E_ElevatorState.Stopped;
        Debug.Log("[ElevatorMgr] 电梯已停靠");

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_dooropen", false);
        PassengerMgr.Instance.SpawnWave();

        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        UIMgr.Instance.GetPanel<GamePanel>(panel =>
        {
            if (canTriggerMirror)
                panel.ShowMirrorUI();
            else
                panel.HideMirrorUI();
        });
        pendingMirrorEvent = false;

        // 开始停靠状态倒计时
        int dockingDuration = GameLevelMgr.Instance.currentLevelDetail.dockingTime * 1000;
        StartCountdown(dockingDuration);

        TimerMgr.Instance.CreateTimer(false, dockingDuration, () =>
        {
            StopCountdown();
            EnterDepartingState();
        });
    }

    /// <summary>
    /// 电梯进入离开状态
    /// </summary>
    public void EnterDepartingState()
    {
        currentElevatorState = E_ElevatorState.Departing;
        Debug.Log("[ElevatorMgr] 电梯正在离开...");

        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_doorclose", false);

        TimerMgr.Instance.CreateTimer(false, 1000, () =>
        {
            CheckResults();
            EnterMovingState();
        });
    }

    #region 倒计时相关

    /// <summary>
    /// 开始倒计时
    /// </summary>
    private void StartCountdown(int durationMs)
    {
        countdownRemaining = durationMs;
        // 立即触发一次更新
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
    }

    /// <summary>
    /// 停止倒计时
    /// </summary>
    private void StopCountdown()
    {
        countdownRemaining = 0;
    }

    /// <summary>
    /// 每帧更新倒计时
    /// </summary>
    private void UpdateCountdown()
    {
        if (countdownRemaining <= 0)
            return;

        int previousSeconds = countdownRemaining / 1000;
        countdownRemaining -= (int)(Time.deltaTime * 1000);
        int currentSeconds = Mathf.Max(0, countdownRemaining / 1000);

        // 秒数变化时触发事件
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

        if (ghostCount > 0)
        {
            GameDataMgr.Instance.SubTrustValue(ghostCount);
            if (GameDataMgr.Instance.IsTrustDepleted)
                EventMgr.Instance.FallIntoAbyss();
        }
        else
        {
            Time.timeScale = 0;
            UIMgr.Instance.GetPanel<GameOverPanel>((panel) =>
            {
                panel.ShowResult(true);
            });
        }
    }

    public void StopElevator()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);
        MonoMgr.Instance.RemoveUpdateListener(UpdateCountdown);

        if (movingTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(movingTimerId);
            movingTimerId = 0;
        }

        StopCountdown();
    }

    private ElevatorMgr()
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            txtFloor = panel.GetControl<Text>("TxtFloor");
        });
    }
}
