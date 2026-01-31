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
    /// <summary>
    /// 异常状态（等待玩家解决）
    /// </summary>
    Abnormal
}

/// <summary>
/// 电梯管理器
/// </summary>
public class ElevatorMgr : BaseSingleton<ElevatorMgr>
{
    private E_ElevatorState currentElevatorState = E_ElevatorState.Stopped;

    /// <summary>
    /// 当前电梯状态（只读）
    /// </summary>
    public E_ElevatorState CurrentState => currentElevatorState;

    private Text txtFloor;

    private int waveNum = 0;
    private int levelNum = 0;

    // 缓存当前波/层信息
    private WaveDetailSO currentWave;
    private LevelDetailSO currentLevelDetail;
    private int currentLevelIndex;
    private bool pendingMirrorEvent;

    /// <summary>
    /// 异常事件发生前的状态（用于恢复）
    /// </summary>
    private E_ElevatorState stateBeforeAbnormal;

    /// <summary>
    /// 移动状态的定时器ID
    /// </summary>
    private int movingTimerId;

    /// <summary>
    /// 运行中可用物品；停靠中和异常中禁用
    /// </summary>
    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        currentElevatorState == E_ElevatorState.Abnormal; // 异常时允许使用铃铛

    /// <summary>
    /// 停靠时允许与乘客交互；运行时和异常时禁用
    /// </summary>
    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;

    /// <summary>
    /// 是否处于异常状态
    /// </summary>
    public bool IsInAbnormalState => currentElevatorState == E_ElevatorState.Abnormal;

    /// <summary>
    /// 开启电梯的方法 进入游戏时调用
    /// </summary>
    public void StartElevator()
    {
        // 注册异常事件监听
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        //开启电梯音效
        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience");
        // 监听 FixedUpdate 以便实时检测灵能值
        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        EnterMovingState();
    }

    /// <summary>
    /// 电梯进入移动状态(电梯状态1) 
    /// </summary>
    public void EnterMovingState()
    {
        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("[ElevatorMgr] 电梯正在运行...");

        movingTimerId = TimerMgr.Instance.CreateTimer(false, Random.Range(5, 10) * 1000, () =>
        {
            // 随机异常
            bool triggerAbnormal = Random.value < 0.2f;
            if (triggerAbnormal)
            {
                // 触发异常事件，暂停电梯状态转换
                EventMgr.Instance.UnnormalEvent();
                // 不立即进入下一状态，等待异常解决
                return;
            }

            EnterArrivingState();
        });
    }

    /// <summary>
    /// 异常事件开始时的处理
    /// </summary>
    private void OnUnnormalEventStart()
    {
        Debug.Log("[ElevatorMgr] 异常事件开始，电梯进入异常状态");

        // 记录异常前的状态
        stateBeforeAbnormal = currentElevatorState;

        // 进入异常状态
        currentElevatorState = E_ElevatorState.Abnormal;

        // 取消正在进行的状态转换定时器
        if (movingTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(movingTimerId);
            movingTimerId = 0;
        }
    }

    /// <summary>
    /// 异常事件解决后的处理
    /// </summary>
    private void OnUnnormalEventResolved()
    {
        Debug.Log("[ElevatorMgr] 异常事件已解决，电梯恢复运行");

        // 恢复到下一个正常状态
        // 无论异常前是什么状态，解决后都继续进入到达状态
        EnterArrivingState();
    }

    /// <summary>
    /// 电梯进入到到达状态(电梯状态2)
    /// </summary>
    public void EnterArrivingState()
    {
        currentElevatorState = E_ElevatorState.Arriving;
        Debug.Log("[ElevatorMgr] 电梯即将到达...");

        TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            //播放电梯抵达音效
            MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_beep", false);

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

            // 本层是否配置了铜镜事件
            pendingMirrorEvent = currentWave.createMirrorLevelIndex != null &&
                                 currentWave.createMirrorLevelIndex.Contains(currentLevelIndex);

            // 改变楼层 UI 显示
            ChangeLevelUI(currentLevelDetail.level);
            EnterStoppedState();
        }, 400,
        () =>
        {
            ChangeLevelUI(Random.Range(2, 18));
        });
    }

    /// <summary>
    /// 电梯进入停靠状态(电梯状态3)
    /// </summary>
    public void EnterStoppedState()
    {
        currentElevatorState = E_ElevatorState.Stopped;
        Debug.Log("[ElevatorMgr] 电梯已停靠");

        // 停止电梯音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_dooropen", false);

        // 生成乘客
        PassengerMgr.Instance.SpawnWave();

        // 铜镜事件
        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        UIMgr.Instance.GetPanel<GamePanel>(panel =>
        {
            if (canTriggerMirror)
                panel.ShowMirrorUI();
            else
                panel.HideMirrorUI();
        });
        pendingMirrorEvent = false;

        // 倒计时（玩家操作时间）
        TimerMgr.Instance.CreateTimer(false, GameLevelMgr.Instance.currentLevelDetail.dockingTime * 1000, () =>
        {
            EnterDepartingState();
        });
    }

    /// <summary>
    /// 电梯进入离开状态(电梯状态4)
    /// </summary>
    public void EnterDepartingState()
    {
        currentElevatorState = E_ElevatorState.Departing;
        Debug.Log("[ElevatorMgr] 电梯正在离开...");

        //播放电梯关门音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_doorclose", false);

        TimerMgr.Instance.CreateTimer(false, 1000, () =>
        {
            CheckResults();
            EnterMovingState();
        });
    }

    /// <summary>
    /// 改变楼层的UI显示
    /// </summary>
    public void ChangeLevelUI(int level)
    {
        if (txtFloor != null)
            txtFloor.text = level.ToString();
    }

    /// <summary>
    /// 检查结果
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

        if (ghostCount > 0)
            ResourcesMgr.Instance.SubPassengerTrustValue(ghostCount);
        else
        {
            Time.timeScale = 0;
            UIMgr.Instance.GetPanel<GameOverPanel>((panel) =>
            {
                panel.ShowResult(true);
            });
        }
    }

    /// <summary>
    /// 停止电梯（游戏结束时调用）
    /// </summary>
    public void StopElevator()
    {
        // 移除事件监听
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        // 移除定时器
        if (movingTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(movingTimerId);
            movingTimerId = 0;
        }
    }

    private ElevatorMgr()
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            txtFloor = panel.GetControl<Text>("TxtFloor");
        });
    }
}
