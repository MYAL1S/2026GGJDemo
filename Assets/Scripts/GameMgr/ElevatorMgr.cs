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
    Departing
}

/// <summary>
/// 电梯管理器
/// </summary>
public class ElevatorMgr : BaseSingleton<ElevatorMgr>
{
    private E_ElevatorState currentElevatorState = E_ElevatorState.Stopped;

    private Text txtFloor;

    private int waveNum = 0;
    private int levelNum = 0;

    // 缓存当前波/层信息
    private WaveDetailSO currentWave;
    private LevelDetailSO currentLevelDetail;
    private int currentLevelIndex;
    private bool pendingMirrorEvent; // 本次停靠是否应触发铜镜（由 wave 配置决定）

    /// <summary>
    /// 运行中可用面具；停靠中禁用
    /// </summary>
    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing;

    /// <summary>
    /// 停靠时允许与乘客交互；运行时禁用
    /// </summary>
    public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped;

    /// <summary>
    /// 开启电梯的方法 进入游戏时调用
    /// </summary>
    public void StartElevator()
    {
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
        Debug.Log("电梯正在运行...");

        TimerMgr.Instance.CreateTimer(false, Random.Range(5, 10) * 1000, () =>
        {
            // 随机异常
            bool triggerAbnormal = Random.value < 0.2f;
            if (triggerAbnormal)
                EventMgr.Instance.UnnormalEvent();

            EnterArrivingState();
        });
    }

    /// <summary>
    /// 电梯进入到到达状态(电梯状态2)
    /// </summary>
    public void EnterArrivingState()
    {
        currentElevatorState = E_ElevatorState.Arriving;

        TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            //播放电梯抵达音效
            MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_beep",false);

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
            GameLevelMgr.Instance.currentLevelDetail = currentLevelDetail; // 更新全局当前关卡
            levelNum++; // 准备下次

            // 本层是否配置了铜镜事件（基于 wave 的索引列表）
            pendingMirrorEvent = currentWave.createMirrorLevelIndex != null &&
                                 currentWave.createMirrorLevelIndex.Contains(currentLevelIndex);

            // 改变楼层 UI 显示
            ChangeLevelUI(currentLevelDetail.level);
            EnterStoppedState();
        },400,
        () =>
        {
            ChangeLevelUI(Random.Range(2,18));
        });
    }

    /// <summary>
    /// 电梯进入停靠状态(电梯状态3)
    /// 此处实现乘客生成和玩家操作时间倒计时
    /// </summary>
    public void EnterStoppedState()
    {
        currentElevatorState = E_ElevatorState.Stopped;

        //0. 停止电梯音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/elevator_dooropen", false);

        // 1. 生成乘客
        PassengerMgr.Instance.SpawnWave();

        // 2. 铜镜事件：一局只可触发 maxMirrorOccourence 次
        bool canTriggerMirror = pendingMirrorEvent && GameLevelMgr.Instance.TryConsumeMirrorOccurence();
        UIMgr.Instance.GetPanel<GamePanel>(panel =>
        {
            if (canTriggerMirror)
                panel.ShowMirrorUI();
            else
                panel.HideMirrorUI();
        });
        // 当次处理完毕，重置标记
        pendingMirrorEvent = false;

        // 3. 倒计时（玩家操作时间）
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
        // TODO: 根据当前楼层具体改变 UI 显示，例如 txtFloor.text = level.ToString();
        txtFloor.text = level.ToString();
    }

    /// <summary>
    /// 调用外部方法检查还剩多少鬼
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
            // 扣除信任值（ResourcesMgr 内部已做下限保护与失败判定）
            ResourcesMgr.Instance.SubPassengerTrustValue(ghostCount);
        else
        {
            //游戏通关
            Time.timeScale = 0;
            UIMgr.Instance.GetPanel<GameOverPanel>((panel) =>
            {
                //展示游戏胜利界面
                panel.ShowResult(true);
            });
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
