using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 电梯运行状态机的核心状态枚举。
/// 说明：当前脚本主要在 Moving/Arriving/Stopped/Departing 之间循环，
/// Abnormal 用于表达“异常语义”，真正的异常流程由 isAbnormalState + 倒计时驱动。
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
/// ElevatorMgr 维护手册（Maintenance Manual）
///
/// 【职责边界】
/// - 维护电梯状态机（Moving/Arriving/Stopped/Departing）
/// - 驱动异常态（开始、解除、超时失败）
/// - 驱动回合推进（PrepareNextLevel）与结果结算（CheckResults）
/// - 统一管理本脚本创建的计时器与 Update/FixedUpdate 监听
///
/// 【状态迁移图（主流程）】
///   StartElevator
///       |
///       v
///   EnterInitialArrivingState
///       |
///       v
///     Arriving ------------------------------+
///       |                                    |
///       v                                    |
///     Stopped --(isWinPending=true)--> Win  |
///       |                                    |
///       v                                    |
///    Departing --------------------------> Moving
///       ^                                    |
///       |____________________________________|
///
/// 【异常支线】
/// - Moving 结束时按概率触发 EventMgr.UnnormalEvent()
/// - OnUnnormalEventStart() 开启异常倒计时并接管 UI 秒数
/// - OnUnnormalEventResolved() 关闭异常倒计时并恢复普通倒计时显示
/// - 异常倒计时归零 -> StopElevator() + FallIntoAbyss()
///
/// 【调用时序（关键路径）】
/// 1) 启动：StartElevator -> 注册监听 -> EnterInitialArrivingState
/// 2) 每轮：EnterMovingState -> EnterArrivingState -> EnterStoppedState -> EnterDepartingState
/// 3) 离开结束：CheckResults -> PrepareNextLevel -> EnterMovingState
/// 4) 停止：StopElevator（可重入、无条件清理）
///
/// 【常见故障排查入口】
/// 1) 现象：电梯卡在某状态不前进
///    - 看 `isRunning` 是否被提前置 false（重点排查 StopElevator 调用来源）
///    - 看 `activeTimerId` 是否被正确创建/清理
///    - 查看日志关键字："[侦探模式]" / "[ElevatorMgr]"
///
/// 2) 现象：倒计时显示异常
///    - 看 `isAbnormalCountdown` 是否长期为 true
///    - 检查是否收到 `E_UnnormalEventResolved`
///
/// 3) 现象：跨局残留事件/镜子弹窗误触发
///    - 确认 StopElevator 是否执行
///    - 确认 `mirrorEventTimerIds` 与 `activeTimerId` 是否已清空
///
/// 4) 现象：到站/开关门音效导致流程中断
///    - 当前关键音效调用均有 try-catch 保护
///    - 优先检查资源路径与打包配置
///
/// 【维护原则】
/// - 先清理再启动（StartElevator 第一行必须保留 StopElevator）
/// - 阶段切换前优先 ClearActiveTimer，避免旧回调串入
/// - 任何新增计时器都要有对应清理路径
/// </summary>
public class ElevatorMgr : BaseSingleton<ElevatorMgr>
{
    // =========================
    // 运行时状态（核心）
    // =========================

    /// <summary>
    /// 当前电梯状态（状态机主变量）。
    /// </summary>
    private E_ElevatorState currentElevatorState = E_ElevatorState.Stopped;

    /// <summary>
    /// 对外只读暴露当前状态。
    /// </summary>
    public E_ElevatorState CurrentState => currentElevatorState;

    /// <summary>
    /// 当前 wave 在 waveSOList 中的索引。
    /// </summary>
    private int waveNum = 0;

    /// <summary>
    /// 当前 wave 下的关卡推进索引（指向 levelDetails）。
    /// </summary>
    private int levelNum = 0;

    /// <summary>
    /// 对外暴露当前 wave 索引（用于 UI/调试）。
    /// </summary>
    public int CurrentWaveIndex => waveNum;

    /// <summary>
    /// 当前运行中的波次配置。
    /// </summary>
    private WaveDetailSO currentWave;

    /// <summary>
    /// 当前楼层配置（由 currentWave.levelDetails[currentLevelIndex] 得到）。
    /// </summary>
    private LevelDetailSO currentLevelDetail;

    /// <summary>
    /// 当前楼层在 wave.levelDetails 中的索引。
    /// </summary>
    private int currentLevelIndex;

    /// <summary>
    /// 当前阶段主计时器（移动/到达/停靠/离开之一）的 timerId。
    /// </summary>
    private int activeTimerId = 0;

    /// <summary>
    /// 延迟启动随机楼层显示的计时器 id。
    /// </summary>
    private int delayRandomDisplayTimerId = 0;

    /// <summary>
    /// 上一层楼号（用于计算电梯方向）。
    /// </summary>
    private int previousLevel = 0;

    /// <summary>
    /// 当前楼号（UI显示与逻辑判断使用）。
    /// </summary>
    private int currentLevel = 0;

    /// <summary>
    /// 普通流程倒计时（毫秒）。
    /// </summary>
    private int countdownRemaining;

    /// <summary>
    /// 异常流程倒计时（毫秒）。
    /// </summary>
    private int abnormalCountdownRemaining;

    /// <summary>
    /// 电梯总开关：false 时所有阶段函数应尽快 return。
    /// </summary>
    private bool isRunning = false;

    /// <summary>
    /// 是否处于异常态（影响音乐、音效、倒计时优先级和失败判定）。
    /// </summary>
    private bool isAbnormalState = false;
    public bool IsAbnormalState => isAbnormalState;

    /// <summary>
    /// 胜利待触发标记。
    /// 在结果判定阶段设置，在下一次停靠时真正结算胜利并弹窗。
    /// </summary>
    private bool isWinPending = false;

    // 状态判断属性
    public bool CanUseMask =>
        currentElevatorState == E_ElevatorState.Moving ||
        currentElevatorState == E_ElevatorState.Arriving ||
        currentElevatorState == E_ElevatorState.Departing ||
        isAbnormalState;

    //之前的处理是：停靠状态且不是第一次停靠才允许交互乘客
    //如果是停靠状态，且不是第一次停靠（第一次停靠没有乘客），才允许交互乘客
    //public bool CanInteractPassengers => currentElevatorState == E_ElevatorState.Stopped && !isFirstDocking;
    // 现在改成：任何时候都不允许交互乘客，彻底禁止乘客交互逻辑，简化逻辑
    public bool CanInteractPassengers => false;

    /// <summary>
    /// 随机楼层显示累计时间（毫秒计时基于 deltaTime*1000）。
    /// </summary>
    private float floorDisplayTimer = 0f;

    /// <summary>
    /// 是否正在执行“离开阶段随机楼层数字闪烁”。
    /// </summary>
    private bool isRandomDisplaying = false;

    /// <summary>
    /// 是否由异常倒计时接管 UI 计时显示。
    /// true 时 E_CountdownUpdate 显示 abnormalCountdownRemaining。
    /// </summary>
    private bool isAbnormalCountdown = false;

    /// <summary>
    /// 电梯方向（true=上行，false=下行）。
    /// 由 currentLevel 与 previousLevel 比较得到。
    /// </summary>
    private bool isGoingUp = true;

    /// <summary>
    /// 是否是第一次停靠。
    /// 首次停靠通常没有交互窗口，且停靠时长使用 firstDockingTime。
    /// </summary>
    private bool isFirstDocking = true;

    /// <summary>
    /// 当前层将要调度的镜子事件列表（从 currentWave.mirrorEvents 过滤得到）。
    /// </summary>
    private List<MirrorEventConfig> currentMirrorEvents = new List<MirrorEventConfig>();

    /// <summary>
    /// 已创建的镜子事件计时器 id 列表，用于阶段切换和 Stop 时统一清理。
    /// </summary>
    private List<int> mirrorEventTimerIds = new List<int>();


    #region 初始化模块

    /// <summary>
    /// 启动电梯主流程。
    /// 该方法会执行一次“先停后启”的完整重置，并重新注册运行期监听，
    /// 最终把状态机推进到开局到站阶段（Initial Arriving）。
    /// </summary>
    public void StartElevator()
    {
        // ==================== 【阶段 1】全量清理旧状态 ====================
        // 无条件执行 StopElevator，即使当前 isRunning=true 也必须清理
        // 关键：这是修复"重新开始后立刻死亡"的第一步
        StopElevator();

        // ==================== 【阶段 2】重置关键管理器 ====================
        // 重置 ResourcesMgr 的"死亡判定"状态
        // 原因：如果不加这一行，ResourcesMgr 会在下一帧立刻再次杀死电梯
        // 这会导致"新局刚启动就直接失败"的 bug
        ResourcesMgr.Instance.ResetRuntimeData();

        // ==================== 【阶段 3】初始化运行时标记 ====================
        // 打开电梯总开关，允许所有阶段函数执行
        isRunning = true;
        // 清除异常态相关标记（若上局在异常中结束）
        isAbnormalState = false;
        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;
        // 清除胜利待结算标记（若上局在胜利中断裂）
        isWinPending = false;
        // 标记首次停靠（首停没有乘客，停靠时间不同）
        isFirstDocking = true;

        // ==================== 【阶段 4】重置其他管理系统 ====================
        GameLevelMgr.Instance.ResetRuntimeCounters();  // 重置关卡管理器计数
        PassengerMgr.Instance.ClearAllPassengers();    // 清空所有旧乘客

        // ==================== 【阶段 5】初始化局部索引 ====================
        // wave/level 计数器从零开始，直到 PrepareNextLevel 才会递增
        waveNum = 0;      // 当前波次索引
        levelNum = 0;     // 当前关卡索引
        previousLevel = 0; // 上一楼层号（用于计算方向）
        currentLevel = 0;  // 当前楼层号

        // ==================== 【阶段 6】注册全局事件监听 ====================
        // 异常事件生命周期监听（会在 StopElevator 时被移除）
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventStart, OnUnnormalEventStart);
        EventCenter.Instance.AddEventListener(E_EventType.E_UnnormalEventResolved, OnUnnormalEventResolved);

        // ==================== 【阶段 7】注册帧更新回调 ====================
        // FixedUpdate：检查"超能力警告"（如血量过低触发失败）
        MonoMgr.Instance.AddFixedUpdateListener(GameDataMgr.Instance.CheckPsychicPowerWarning);
        // Update：驱动倒计时逻辑（每帧递减，按秒级粒度更新 UI）
        MonoMgr.Instance.AddUpdateListener(UpdateCountdown);

        // ==================== 【阶段 8】UI 初始化 ====================
        // 显示初始楼层数字（通常是 18，表示顶楼）
        ChangeLevelUI(18);

        // ==================== 【阶段 9】进入初始状态 ====================
        // 启动状态机到达"初始 Arriving"阶段
        // 从这里开始执行主要的 Arriving->Stopped->Departing->Moving 循环
        EnterInitialArrivingState();
    }

    /// <summary>
    /// 进入开局专用的到站阶段。
    /// 与常规循环不同，开局先执行一次到站与停靠，用于建立首轮节奏。
    /// </summary>
    private void EnterInitialArrivingState()
    {
        // 现版本开局流程：先到达再停靠，直接把玩家带入“首停靠->离开->移动”的循环。
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Arriving;

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);

        // 使用单独配置的首次Arriving时间
        int arrivingDuration = ResourcesMgr.Instance.firstArrivingTime * 1000;
        StartCountdown(arrivingDuration);

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            // 【计时器清理】倒计时结束时清空计时器 ID，标记本阶段完成
            activeTimerId = 0;
            StopCountdown();

            // 【保险检查】若在计时过程中电梯已停止，则不继续转移
            if (!isRunning) return;

            // 【音效播放】到达时播放"到达"音效，通知玩家已到站
            // 异常状态下播放异常版本，提示玩家危险情况
            try
            {
                string beepSound = isAbnormalState
                    ? "Music/26GGJsound/elevator_beep_abnormal"
                    : "Music/26GGJsound/elevator_beep";
                MusicMgr.Instance.PlaySound(beepSound, false);
            }
            catch
            {
                // 音效缺失不应中断流程，直接忽略异常
            }

            // 【状态转移】到达阶段结束，进入停靠状态（开门、生成乘客）
            EnterStoppedState();
        });
    }
    #endregion


    #region 楼层UI显示
    /// <summary>
    /// 开启离开阶段的随机楼层闪烁效果。
    /// 本质是注册一个 Update 回调，按配置间隔随机刷新楼层 UI。
    /// </summary>
    private void StartRandomFloorDisplay()
    {
        // 离开阶段用于营造“电梯运动感”的楼层随机跳变视觉效果。
        StopRandomFloorDisplay();
        isRandomDisplaying = true;
        floorDisplayTimer = 0f;
        MonoMgr.Instance.AddUpdateListener(UpdateRandomFloorDisplay);
    }

    /// <summary>
    /// 随机楼层显示的帧更新逻辑。
    /// 仅在 Departing 且运行中时生效，达到间隔后更新一次楼层数字。
    /// </summary>
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

    /// <summary>
    /// 关闭随机楼层闪烁效果并移除对应 Update 监听。
    /// </summary>
    private void StopRandomFloorDisplay()
    {
        // 确保 Update 回调被移除，避免悬挂更新。
        if (isRandomDisplaying)
        {
            isRandomDisplaying = false;
            MonoMgr.Instance.RemoveUpdateListener(UpdateRandomFloorDisplay);
        }
    }

    /// <summary>
    /// 刷新楼层 UI 显示。
    /// 通过异步取面板避免面板未创建时直接访问导致空引用。
    /// </summary>
    /// <param name="level">要显示的楼层号。</param>
    public void ChangeLevelUI(int level)
    {
        // 通过 UIMgr 异步拿面板，避免面板尚未创建时直接空引用。
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            if (panel == null) return;
            panel.TrySetFloor(level);
        });
    }
    #endregion


    #region 计时器相关

    /// <summary>
    /// 启动普通流程倒计时（毫秒）。
    /// 若当前由异常倒计时接管 UI，则仅更新内部值，不刷新 UI 文本。
    /// </summary>
    /// <param name="durationMs">倒计时总时长（毫秒）。</param>
    private void StartCountdown(int durationMs)
    {
        // 普通流程计时入口。
        // 注意：若当前由异常倒计时接管，这里不会覆盖 UI 的秒数显示。
        countdownRemaining = durationMs;
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
        }
    }

    /// <summary>
    /// 停止普通流程倒计时。
    /// 在非异常接管模式下会同步把 UI 倒计时归零。
    /// </summary>
    private void StopCountdown()
    {
        // 停止普通流程计时并（在非异常接管时）归零 UI 显示。
        countdownRemaining = 0;
        if (!isAbnormalCountdown)
        {
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, 0);
        }
    }

    /// <summary>
    /// 全局倒计时更新入口（每帧调用）。
    /// 同时维护普通倒计时与异常倒计时，且异常倒计时拥有更高优先级。
    /// </summary>
    private void UpdateCountdown()
    {
        // 计时优先级：异常倒计时 > 普通倒计时。
        // 异常倒计时结束会直接触发失败流程。
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
                EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, currentSeconds);

            if (abnormalCountdownRemaining <= 0)
            {
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

    /// <summary>
    /// 清理“延迟启动随机楼层显示”计时器。
    /// 该方法可重复调用，保证停止阶段不残留延迟回调。
    /// </summary>
    private void ClearDelayTimer()
    {
        if (delayRandomDisplayTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(delayRandomDisplayTimerId);
            delayRandomDisplayTimerId = 0;
        }
    }

    /// <summary>
    /// 清理当前阶段主计时器。
    /// 用于阶段切换前防止旧阶段回调在新阶段触发。
    /// </summary>
    private void ClearActiveTimer()
    {
        // 清掉“当前阶段主计时器”，防止阶段切换后旧计时器回调串入。
        if (activeTimerId != 0)
        {
            TimerMgr.Instance.RemoveTimer(activeTimerId);
            activeTimerId = 0;
        }
    }

    #endregion


    #region 数据结算，准备相关

    /// <summary>
    /// 预计算下一轮运行所需的关卡上下文。
    /// 包括 wave/level 选择、方向计算、镜子事件缓存重建与当前楼层同步。
    /// </summary>
    private void PrepareNextLevel()
    {
        // 关卡准备职责：
        // - 处理胜利待结算时的回楼层显示
        // - 选择 wave 与 levelDetail
        // - 同步 GameLevelMgr 当前层配置
        // - 重建镜子事件调度缓存
        // - 计算运行方向

        // 清理旧的铜镜事件计时器
        currentMirrorEvents.Clear();
        foreach (var id in mirrorEventTimerIds) TimerMgr.Instance.RemoveTimer(id);
        mirrorEventTimerIds.Clear();

        if (isWinPending)
        {
            previousLevel = currentLevel;
            currentLevel = 1;
            isGoingUp = currentLevel > previousLevel;
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);
            return;
        }

        if (ResourcesMgr.Instance.waveSOList == null || ResourcesMgr.Instance.waveSOList.Count == 0)
            return;

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

        previousLevel = currentLevel;
        currentLevel = currentLevelDetail.level;

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

    /// <summary>
    /// 回合结算逻辑。
    /// 统计乘客/幽灵状态，判断胜利待结算，处理幽灵伤害结算并触发失败判定。
    /// </summary>
    private void CheckResults()
    {
        // 回合结算职责：
        // 1) 统计乘客总数与幽灵数
        // 2) 满员且无幽灵 -> 标记胜利待结算
        // 3) 对“非本回合新增且未结算伤害”的幽灵扣信任值
        // 4) 信任归零则触发失败

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
                        ghostsToSettle.Add(p);
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
    #endregion


    #region 状态机循环
    /// <summary>
    /// 进入 Moving 阶段。
    /// 随机生成移动时长，设置“移动+到站”总倒计时，并在移动结束后切入 Arriving。
    /// </summary>
    public void EnterMovingState()
    {
        // Moving 阶段：
        // - 随机移动时长
        // - 倒计时显示=移动+到达总时长
        // - 移动结束时可概率触发异常，再进入 Arriving

        // 【运行检查】若电梯已停止，则中止状态转移
        if (!isRunning) return;

        // 【阶段切换准备】清理上一阶段（Departing）的计时器
        ClearActiveTimer();

        // 【状态更新】设置当前电梯状态为 Moving
        currentElevatorState = E_ElevatorState.Moving;

        // 【事件通知】通知所有监听者电梯方向已改变
        // isGoingUp 由 PrepareNextLevel 根据 currentLevel 和 previousLevel 计算
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);

        // 【配置获取】从资源管理器获取电梯运动的时间配置
        var config = ResourcesMgr.Instance;
        int minTime = config.elevatorMovingTimeMin * 1000;      // 最小移动时间（毫秒）
        int maxTime = config.elevatorMovingTimeMax * 1000;      // 最大移动时间（毫秒）
        int movingDuration = Random.Range(minTime, maxTime + 1); // 本次随机移动时长

        // 【倒计时计算】UI 显示的倒计时 = 移动时间 + 到达时间
        // 这样用户可以看到"还需多久到达"的总时间
        int arrivingDuration = config.elevatorArrivingTime * 1000;
        int totalDuration = movingDuration + arrivingDuration;
        StartCountdown(totalDuration);

        // 【主计时器创建】在移动时长后触发异常判断，然后进入 Arriving
        activeTimerId = TimerMgr.Instance.CreateTimer(false, movingDuration, () =>
        {
            // 【计时器清理】移动阶段完成时清空计时器 ID
            activeTimerId = 0;

            // 【保险检查】若在移动过程中电梯已停止，则不继续转移
            if (!isRunning) return;

            // 【异常判断】若非胜利待结算状态，则按概率触发异常事件
            if (!isWinPending)
            {
                // 按配置的异常概率随机决定是否触发异常
                bool triggerAbnormal = Random.value < config.abnormalEventChance;
                if (triggerAbnormal && !isAbnormalState)
                {
                    // 如果决定触发异常且当前未处于异常态，则通知事件管理器
                    EventMgr.Instance.UnnormalEvent();
                }
            }

            // 【状态转移】移动阶段结束，进入到达阶段（Arriving）
            // Arriving 阶段会继续倒计时，直到到达时间结束后进入停靠（Stopped）
            EnterArrivingState();
        });
    }

    /// <summary>
    /// 进入 Arriving 阶段。
    /// 执行到站前过渡计时，结束后播放提示音并进入 Stopped 阶段。
    /// </summary>
    public void EnterArrivingState()
    {
        // Arriving 阶段：到站前过渡。
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Arriving;

        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, isGoingUp);

        int arrivingDuration = ResourcesMgr.Instance.elevatorArrivingTime * 1000;

        activeTimerId = TimerMgr.Instance.CreateTimer(false, arrivingDuration, () =>
        {
            activeTimerId = 0;
            StopCountdown();

            if (!isRunning) return;

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

    /// <summary>
    /// 进入 Stopped 阶段。
    /// 负责开门表现、乘客生成、停靠计时，以及停靠结束后转入 Departing。
    /// </summary>
    public void EnterStoppedState()
    {
        // Stopped 阶段：开门、生成乘客（延迟）、停靠倒计时、然后离开。
        // 这是状态机最关键的“回合停靠节点”。
        if (!isRunning) return;

        ClearActiveTimer();

        // 1. 先切换状态枚举
        currentElevatorState = E_ElevatorState.Stopped;

        try
        {
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDirectionChanged, true);
            EventCenter.Instance.EventTrigger<bool>(E_EventType.E_ElevatorDoorStateChanged, true);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[ElevatorMgr] 停靠时UI/乘客出错(已忽略，继续流程): {e.Message}");
        }

        // 音效逻辑保护
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

        // UI和乘客逻辑保护
        try
        {
            // 如果 GamePanel 还没加载好，这里可能会空引用报错，必须 try 住
            PassengerMgr.Instance.SwitchToDockingPositions();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[ElevatorMgr] 停靠时UI/乘客出错(已忽略，继续流程): {e.Message}");
        }


        TimerMgr.Instance.CreateTimer(false, 500, () =>
        {
            // 生成乘客保护
            try
            {
                PassengerMgr.Instance.SpawnWave();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[ElevatorMgr] 生成乘客失败: {e.Message}");
            }
        });

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

    /// <summary>
    /// 进入 Departing 阶段。
    /// 负责关门、乘客站位切换、随机楼层显示、镜子事件调度与离站后回合推进。
    /// </summary>
    public void EnterDepartingState()
    {
        // Departing 阶段：关门、切到移动站位、随机楼层显示、镜子事件调度。
        // 阶段结束后执行回合结算并准备下一层。
        if (!isRunning) return;

        ClearActiveTimer();

        currentElevatorState = E_ElevatorState.Departing;

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

    #endregion


    #region 事件相关
    #region 异常事件
    /// <summary>
    /// 异常事件开始回调。
    /// 启用异常态、切换异常环境音，并让异常倒计时接管 UI 秒数展示。
    /// </summary>
    private void OnUnnormalEventStart()
    {
        // 异常开始后：
        // - 切换异常环境音
        // - 触发异常状态 UI
        // - 启动异常倒计时并接管倒计时显示
        isAbnormalState = true;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_amb_abnormal");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, true);

        isAbnormalCountdown = true;
        abnormalCountdownRemaining = ResourcesMgr.Instance.abnormalEventTimeout * 1000;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, abnormalCountdownRemaining / 1000);
    }

    /// <summary>
    /// 异常事件解除回调。
    /// 恢复常态音效与状态，并把 UI 秒数显示切回普通流程倒计时。
    /// </summary>
    private void OnUnnormalEventResolved()
    {
        // 异常解除后：
        // - 恢复常态环境音
        // - 取消异常倒计时接管
        // - 还原普通流程倒计时显示
        isAbnormalState = false;

        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
        EventCenter.Instance.EventTrigger<bool>(E_EventType.E_AbnormalStateChanged, false);

        isAbnormalCountdown = false;
        abnormalCountdownRemaining = 0;

        EventCenter.Instance.EventTrigger<int>(E_EventType.E_CountdownUpdate, countdownRemaining / 1000);
        ChangeLevelUI(currentLevel);
    }
    #endregion
    #region 特殊乘客
    /// <summary>
    /// 当前层是否应刷出特殊乘客。
    /// 判定规则：当前层索引命中当前 wave 的 specialPassengerSpawnIndex。
    /// </summary>
    public bool ShouldSpawnSpecialPassenger
    {
        get
        {
            // 特殊乘客刷出条件：当前层索引命中 wave 配置项。
            if (currentWave == null) return false;

            // 检查当前层索引是否等于配置的生成索引
            // 注意：这里会引用你在第一步中添加的变量 specialPassengerSpawnIndex
            return currentLevelIndex == currentWave.specialPassengerSpawnIndex;
        }
    }
    #endregion
    #region 铜镜事件
    /// <summary>
    /// 调度当前层配置的镜子事件。
    /// 按 triggerDelay 创建一次性计时器，且仅在 Departing 阶段触发弹窗。
    /// </summary>
    private void ScheduleMirrorEvents()
    {
        // 根据 currentMirrorEvents 为当前离开阶段创建延迟事件。
        // 每次进入前先清空旧 timer，防止跨层残留触发。
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

    #endregion
    #endregion


    #region 结束逻辑
    /// <summary>
    /// 停止电梯（彻底清理状态）
    /// </summary>
    public void StopElevator()
    {
        // 停止流程总目标：
        // - 无条件清理（即使当前看似未运行）
        // - 反注册监听
        // - 清空全部计时器引用
        // - 归零倒计时显示

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
    }
    #endregion

    /// <summary>
    /// 单例构造函数。
    /// 使用私有构造避免外部直接实例化。
    /// </summary>
    private ElevatorMgr() { }

}