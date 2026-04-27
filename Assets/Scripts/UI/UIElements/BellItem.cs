using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 铃铛工具类 - 继承自 DraggableItem
/// 
/// 功能描述：
/// 1. 正常模式：拖曳铃铛到乘客身上进行鬼魂判定
///    - 检测乘客是否为鬼魂
///    - 若是鬼魂则驱散（无灵能消耗）
///    - 若是普通乘客则降低信任值
/// 
/// 2. 异常事件模式：摇晃铃铛来解决异常事件
///    - 检测玩家的摇晃动作（反向运动）
///    - 累计摇晃次数，达到要求次数时完成
///    - 无时间限制，持续摇晃直到完成
/// 
/// 工作流程：
/// - OnDragStart: 判断游戏状态，确定进入哪种模式
/// - OnDrag: 根据模式执行不同逻辑（乘客检测 或 摇晃检测）
/// - OnDragEnd: 松手时触发相应事件并清理状态
/// - OnTimerExpired: 正常模式下计时过期处理
/// 
/// 两种模式的主要区别：
/// - 正常模式: 有时间限制、监测乘客位置、松手时判定
/// - 异常模式: 无时间限制、检测摇晃动作、达到次数时触发
/// </summary>
public class BellItem : DraggableItem
{
    // ============ 配置相关字段 ============
    /// <summary>铃铛的配置信息（通过 Inspector 预设或运行时设置）</summary>
    [SerializeField]
    private ItemConfigSO itemConfig;

    // ============ 摄像机与乘客选择字段 ============
    /// <summary>主摄像机（用于屏幕坐标转换）</summary>
    private Camera mainCamera;

    /// <summary>当前选中的乘客（正常模式下追踪的目标）</summary>
    private Passenger currentSelectedPassenger;

    // ============ 摇晃检测相关字段 ============
    /// <summary>当前积累的摇晃次数（异常模式下）</summary>
    private int currentShakeCount = 0;

    /// <summary>上一帧铃铛的位置（用于计算移动距离和方向）</summary>
    private Vector2 lastPosition;

    /// <summary>上一次检测到的移动方向（用于判断反向运动）</summary>
    private Vector2 lastDirection;

    /// <summary>累积移动距离（未达到阈值前的临时累计）</summary>
    private float accumulatedDistance = 0f;

    /// <summary>上一次有效摇晃的时间（用于判断摇晃是否在时间窗口内）</summary>
    private float lastShakeTime = 0f;

    // ============ 模式状态字段 ============
    /// <summary>标记是否处于异常事件模式（true = 异常模式，false = 正常模式）</summary>
    private bool isInUnnormalMode = false;

    /// <summary>标记是否已完成摇晃任务（防止重复触发完成逻辑）</summary>
    private bool hasCompletedShake = false;

    // ============ 配置参数字段 ============
    /// <summary>使用一次铃铛消耗的灵能值（正常模式）</summary>
    private int psychicCost = 1;

    /// <summary>乘客检测半径（正常模式下，未覆盖乘客区域时的检测范围）</summary>
    private float detectionRadius = 50f;

    /// <summary>完成异常事件所需的摇晃次数（异常模式）</summary>
    private int requiredShakeCount = 5;

    /// <summary>触发一次有效摇晃所需的移动距离阈值（异常模式）</summary>
    private float shakeThreshold = 30f;

    /// <summary>有效摇晃的时间窗口（反向运动必须在此时间内发生才算有效摇晃）</summary>
    private float shakeTimeWindow = 0.3f;

    /// <summary>
    /// 保存正常模式的拖拽时间限制
    /// 
    /// 用途：
    /// - 异常模式时将 maxDragTime 设为 float.MaxValue（无限制）
    /// - 结束后需要恢复到正常模式的时间限制
    /// - 由基类 DraggableItem 中的 maxDragTime 管理具体的计时逻辑
    /// </summary>
    private float normalMaxDragTime = 0f;

    #region 配置管理模块
    /// <summary>
    /// 运行时设置铃铛的配置信息
    /// 
    /// 功能：
    /// - 更新 ItemConfigSO 配置对象
    /// - 调用 LoadConfig() 重新加载所有参数
    /// 
    /// 调用时机：
    /// - 若通过 Inspector 预设设置，则 Awake 会自动调用
    /// - 若需动态变更配置，可手动调用此方法
    /// </summary>
    /// <param name="config">铃铛配置信息对象</param>
    public void SetItemConfig(ItemConfigSO config)
    {
        itemConfig = config;
        LoadConfig();
    }

    /// <summary>
    /// 初始化阶段调用（Unity 生命周期）
    /// 
    /// 执行步骤：
    /// 1. 调用基类 Awake 以初始化拖动基础功能
    /// 2. 缓存主摄像机引用（用于屏幕坐标转换）
    /// 3. 加载配置参数
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        LoadConfig();
    }

    /// <summary>
    /// 从 ItemConfigSO 加载所有配置参数
    /// 
    /// 加载内容：
    /// - psychicCost: 灵能消耗值
    /// - detectionRadius: 乘客检测半径
    /// - maxDragTime: 拖动时间限制
    /// - normalMaxDragTime: 保存正常模式的时间限制（用于异常模式恢复）
    /// - requiredShakeCount: 完成异常事件所需摇晃次数
    /// - shakeThreshold: 摇晃动作距离阈值
    /// - shakeTimeWindow: 摇晃时间窗口
    /// 
    /// 依赖：
    /// - itemConfig: 必须非空才能加载
    /// </summary>
    private void LoadConfig()
    {
        if (itemConfig != null)
        {
            psychicCost = itemConfig.bellPsychicCost;
            detectionRadius = itemConfig.bellDetectionRadius;
            maxDragTime = itemConfig.bellMaxDragTime;

            // 保存正常模式的时间限制，以便在异常模式后恢复
            normalMaxDragTime = itemConfig.bellMaxDragTime;

            requiredShakeCount = itemConfig.requiredShakeCount;
            shakeThreshold = itemConfig.shakeThreshold;
            shakeTimeWindow = itemConfig.shakeTimeWindow;
        }
    }
    #endregion

    #region 拖动交互模块
    /// <summary>
    /// 拖动开始时的回调函数（被 DraggableItem 基类触发）
    /// 
    /// 核心逻辑：根据游戏状态选择工作模式
    /// 
    /// 执行步骤：
    /// 1. 重置所有状态字段（为新的拖动做准备）
    /// 2. 从 EventMgr 检查是否处于异常事件状态
    /// 3. 根据模式设置对应的时间限制：
    ///    - 异常模式: maxDragTime = float.MaxValue（无时间限制）
    ///    - 正常模式: maxDragTime = normalMaxDragTime（从配置读取）
    /// 4. 记录起始位置（用于计算移动距离）
    /// 
    /// 重置的字段：
    /// - currentShakeCount: 摇晃计数清零
    /// - accumulatedDistance: 累积距离清零
    /// - lastDirection: 上次方向清零
    /// - hasCompletedShake: 完成标志清零
    /// - currentSelectedPassenger: 选中乘客清零
    /// - lastShakeTime: 更新为当前时间
    /// </summary>
    protected override void OnDragStart()
    {
        // 重置所有摇晃检测相关状态
        currentShakeCount = 0;
        accumulatedDistance = 0f;
        lastDirection = Vector2.zero;
        lastShakeTime = Time.time;
        hasCompletedShake = false;
        currentSelectedPassenger = null;

        // 检查当前游戏状态：是否处于异常事件中
        isInUnnormalMode = EventMgr.Instance.IsInUnnormalState;

        if (isInUnnormalMode)
            // ===== 异常事件模式 =====
            // 需要持续摇晃直到完成，不能有时间限制
            maxDragTime = float.MaxValue;  // 无限拖动时间
        else
            // ===== 正常模式 =====
            // 恢复配置的时间限制
            maxDragTime = normalMaxDragTime;

        // 记录起始位置（用于后续计算移动距离）
        lastPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 拖动过程中的持续回调函数（每帧调用一次）
    /// 
    /// 核心逻辑：根据模式执行不同的检测逻辑
    /// 
    /// 执行步骤：
    /// 1. 调用基类的 OnDrag 以更新拖动位置（移动铃铛）
    /// 2. 检查计时是否过期（仅正常模式）
    /// 3. 根据模式分别处理：
    ///    - 异常模式: 调用 DetectShake 检测摇晃动作
    ///    - 正常模式: 调用 UpdatePassengerSelection 检测乘客并高亮
    /// 
    /// 模式区别：
    /// - 异常模式下计时过期不会中止（无时间限制）
    /// - 正常模式下超时会停止乘客检测
    /// 
    /// 依赖：
    /// - isInUnnormalMode: 当前工作模式
    /// - hasCompletedShake: 异常模式中用于判断是否已完成
    /// </summary>
    public override void OnDrag(PointerEventData eventData)
    {
        // 调用基类方法更新铃铛的拖动位置
        base.OnDrag(eventData);

        // 异常模式下不检查计时器过期，允许无限时间摇晃
        // 正常模式下若超时，则不继续处理
        if (!isInUnnormalMode && isTimerExpired)
            return;

        // 根据不同模式执行对应逻辑
        if (isInUnnormalMode)
        {
            // 异常模式：检测摇晃动作
            if (!hasCompletedShake)
                DetectShake(eventData);
        }
        else
            // 正常模式：检测乘客位置并更新高亮状态
            UpdatePassengerSelection(eventData.position);
    }

    /// <summary>
    /// 拖动结束时的回调函数（松手时触发）
    /// 
    /// 执行步骤：
    /// 1. 根据模式输出不同的日志
    /// 2. 正常模式下：若选中乘客且计时未过期，触发 OnBellHitPassenger
    /// 3. 清除乘客选中状态高亮
    /// 4. 重置所有状态字段
    /// 5. 恢复正常模式的时间限制
    /// 
    /// 正常模式的触发条件（必须同时满足）：
    /// - 有选中的乘客（currentSelectedPassenger != null）
    /// - 计时未过期（!isTimerExpired）
    /// 
    /// 重置的内容：
    /// - currentShakeCount: 清零
    /// - accumulatedDistance: 清零
    /// - isInUnnormalMode: 恢复为 false
    /// - hasCompletedShake: 恢复为 false
    /// - maxDragTime: 恢复为 normalMaxDragTime
    /// 
    /// 依赖：
    /// - currentSelectedPassenger: 正常模式下的判定对象
    /// - isTimerExpired: 从基类继承的计时标志
    /// </summary>
    protected override void OnDragEnd()
    {
        if (!isInUnnormalMode)
        {
            // 判定条件：已选中乘客 且 计时未过期
            if (currentSelectedPassenger != null && !isTimerExpired)
                OnBellHitPassenger(currentSelectedPassenger);

            // 清除乘客选中高亮
            ClearPassengerSelection();
        }

        // 重置状态字段
        currentShakeCount = 0;
        accumulatedDistance = 0f;
        isInUnnormalMode = false;
        hasCompletedShake = false;

        // 恢复正常模式的时间限制
        maxDragTime = normalMaxDragTime;
    }
    #endregion

    #region 铃铛交互逻辑
    #region 异常状态交互逻辑
    /// <summary>
    /// 检测摇晃动作（异常事件模式专用）
    /// 
    /// 核心算法：检测"反向运动"
    /// 
    /// 工作原理：
    /// 1. 跟踪铃铛的移动距离和方向
    /// 2. 当移动距离达到阈值（shakeThreshold）时：
    ///    - 计算当前移动方向
    ///    - 与上次方向进行点积运算（dot < -0.5f 表示反向）
    ///    - 判断是否在时间窗口内（shakeTimeWindow）
    /// 3. 若满足反向条件且在时间窗口内，则计数 +1
    /// 4. 达到要求次数时触发完成
    /// 
    /// 参数说明：
    /// 
    /// shakeThreshold（移动距离阈值）:
    /// - 只有在积累移动距离 >= shakeThreshold 时才检查方向
    /// - 避免微小抖动被误认为摇晃
    /// 
    /// shakeTimeWindow（时间窗口）:
    /// - 两次反向运动必须在此时间内发生才算有效摇晃
    /// - 防止慢速摇晃不被计数
    /// - 例如：0.3 秒内必须完成一次反向运动
    /// 
    /// dot < -0.5f（反向判定）:
    /// - Vector2.Dot 计算两个向量的点积
    /// - -1.0 表示完全反向，1.0 表示同向
    /// - -0.5f 作为阈值，即反向角度 > 120°
    /// 
    /// 执行步骤：
    /// 1. 计算本帧相对上帧的位置差（delta）
    /// 2. 累加移动距离（accumulatedDistance）
    /// 3. 若距离 >= 阈值：
    ///    - 计算本帧方向
    ///    - 若有上次方向：计算点积判断是否反向
    ///    - 若反向且在时间窗口内：计数 +1
    ///    - 更新上次方向和最后摇晃时间
    /// 4. 重置累积距离
    /// 5. 更新位置信息用于下帧计算
    /// 
    /// 参数：
    /// - eventData: UI 事件数据（当前未使用，保留用于扩展）
    /// </summary>
    private void DetectShake(PointerEventData eventData)
    {
        // 获取当前帧铃铛的本地位置
        Vector2 currentPosition = rectTransform.anchoredPosition;

        // 计算与上帧的位置差
        Vector2 delta = currentPosition - lastPosition;

        // 累计本帧移动距离
        accumulatedDistance += delta.magnitude;

        // 检查是否达到了移动距离阈值
        if (accumulatedDistance >= shakeThreshold)
        {
            // 计算当前运动方向（归一化）
            Vector2 currentDirection = delta.normalized;

            // 检查是否有上一帧的方向信息（第一次摇晃时 lastDirection = (0,0)）
            if (lastDirection != Vector2.zero)
            {
                // 计算两个方向的点积
                // -1 = 完全反向，0 = 垂直，1 = 完全同向
                float dot = Vector2.Dot(currentDirection, lastDirection);

                // 判断是否为反向运动（反向角度 > 120°）
                if (dot < -0.5f)
                {
                    // 检查两次反向运动是否在时间窗口内
                    float timeSinceLastShake = Time.time - lastShakeTime;

                    // 若在时间窗口内，则计数 +1
                    if (timeSinceLastShake <= shakeTimeWindow)
                    {
                        currentShakeCount++;

                        // 检查是否已达到完成条件
                        if (currentShakeCount >= requiredShakeCount)
                            OnShakeComplete();
                    }

                    // 更新最后摇晃时间
                    lastShakeTime = Time.time;
                }
            }

            // 更新上次方向
            lastDirection = currentDirection;

            // 重置累积距离，为下次阈值检测做准备
            accumulatedDistance = 0f;
        }

        // 更新位置用于下一帧计算
        lastPosition = currentPosition;
    }

    /// <summary>
    /// 摇晃完成时的处理（异常事件模式）
    /// 
    /// 执行步骤：
    /// 1. 检查是否已完成过（防止重复触发）
    /// 2. 标记完成状态
    /// 3. 输出日志
    /// 4. 播放铃铛的"完成"音效
    /// 5. 通知 EventMgr 异常事件已解决
    /// 6. 恢复铃铛到初始位置
    /// 
    /// 调用时机：
    /// - 当 currentShakeCount >= requiredShakeCount 时由 DetectShake 触发
    /// 
    /// 依赖：
    /// - hasCompletedShake: 防止重复触发标志
    /// - EventMgr.Instance.ResolveUnnormalByBell(): 异常事件解决回调
    /// - MusicMgr.Instance.PlaySound(): 音效播放
    /// - ResetPosition(): 从基类继承，恢复初始位置
    /// </summary>
    private void OnShakeComplete()
    {
        // 防止重复触发完成逻辑
        if (hasCompletedShake)
            return;

        // 标记已完成
        hasCompletedShake = true;

        // 播放完成音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/bell_ring", false);

        // 通知事件管理器异常事件已通过铃铛解决
        EventMgr.Instance.ResolveUnnormalByBell();

        // 恢复铃铛到初始位置
        ResetPosition();
    }
    #endregion

    #region 正常状态交互逻辑
    /// <summary>
    /// 计时器过期时的回调函数（被 DraggableItem 基类触发）
    /// 
    /// 功能：
    /// - 异常模式下：不触发任何逻辑（允许无限摇晃）
    /// - 正常模式下：清除乘客选中状态，调用基类方法
    /// 
    /// 执行步骤：
    /// 1. 检查是否处于异常事件模式
    /// 2. 若是异常模式则直接返回（不做任何处理）
    /// 3. 若是正常模式则：
    ///    - 清除选中乘客的高亮
    ///    - 调用基类 OnTimerExpired（可能播放超时动画等）
    /// 
    /// 异常模式说明：
    /// - 异常模式下没有时间限制
    /// - 不应该触发过期逻辑，应该等待玩家完成摇晃
    /// </summary>
    protected override void OnTimerExpired()
    {
        // 异常模式下允许无限摇晃，不触发过期逻辑
        if (isInUnnormalMode)
            return;

        // 正常模式下清除选中状态
        ClearPassengerSelection();

        // 调用基类过期处理（可能包含其他逻辑）
        base.OnTimerExpired();
    }

    /// <summary>
    /// 松手时的乘客判定（正常模式）
    /// 
    /// 执行步骤：
    /// 1. 检查乘客是否为鬼魂
    /// 2. 若是鬼魂：驱散鬼魂（无灵能消耗）
    /// 3. 若不是鬼魂：降低信任值（惩罚误触乘客）
    /// 
    /// 效果对比：
    /// 
    /// 鬼魂处理：
    /// - 调用 OnGhostDispelled 驱散鬼魂
    /// - 不消耗任何游戏资源
    /// - 目的：识别和驱散隐藏的鬼魂
    /// 
    /// 普通乘客处理：
    /// - 调用 GameDataMgr.SubTrustValue(1) 降低信任值
    /// - 惩罚玩家的错误操作
    /// - 目的：鼓励玩家准确判定
    /// 
    /// 参数：
    /// - passenger: 被击中的乘客对象
    /// 
    /// 依赖：
    /// - passenger.passengerInfo.isGhost: 判定乘客类型
    /// - GameDataMgr.Instance.SubTrustValue(): 信任值管理
    /// </summary>
    private void OnBellHitPassenger(Passenger passenger)
    {
        if (passenger.passengerInfo.isGhost)
            // 检测到鬼魂，直接驱散（不消耗灵能值）
            OnGhostDispelled(passenger);
        else
            // 误触普通乘客，降低信任值
            GameDataMgr.Instance.SubTrustValue(1);
    }

    /// <summary>
    /// 鬼魂被驱散的处理逻辑
    /// 
    /// 执行步骤：
    /// 1. 播放驱散音效（"ghost_disappear"）
    /// 2. 调用 PassengerMgr 移除鬼魂对象
    /// 3. 输出日志确认
    /// 
    /// 音效说明：
    /// - 使用 "ghost_disappear" 音效让玩家听到鬼魂被驱散的反馈
    /// - 第二个参数 false 表示不循环播放
    /// 
    /// 依赖：
    /// - MusicMgr.Instance.PlaySound(): 音效管理
    /// - PassengerMgr.Instance.DispelGhost(): 乘客管理的驱散逻辑
    /// 
    /// 参数：
    /// - ghost: 被驱散的鬼魂乘客对象
    /// </summary>
    private void OnGhostDispelled(Passenger ghost)
    {
        // 播放驱散音效
        MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);

        // 从游戏中移除鬼魂
        PassengerMgr.Instance.DispelGhost(ghost);
    }
    #endregion
    #endregion

    #region 乘客交互逻辑
    /// <summary>
    /// 更新乘客选中状态（正常模式专用）
    /// 
    /// 核心功能：
    /// 1. 遍历所有乘客
    /// 2. 使用两层检测算法找到最近的乘客
    /// 3. 更新乘客的高亮状态
    /// 
    /// 两层检测算法：
    /// 
    /// 【层1】直接覆盖检测：铃铛中心点是否在乘客区域内
    /// - 使用 RectTransformUtility.RectangleContainsScreenPoint
    /// - 若在区域内，计算到乘客中心的距离
    /// - 优先级最高，直接进入下一位乘客
    /// 
    /// 【层2】距离范围检测：铃铛是否在乘客周围 detectionRadius 范围内
    /// - 计算屏幕空间距离
    /// - 若在 detectionRadius 范围内，可被选中
    /// - 作为"近距离"检测的备选方案
    /// 
    /// 优先级规则：
    /// - 优先选择最近的乘客
    /// - 若有多个乘客，选择距离最小的
    /// - 若选中乘客变化，更新高亮状态
    /// 
    /// UI 反馈：
    /// - 选中乘客时调用 SetItemHighlight(true) 显示高亮
    /// - 取消选中时调用 SetHighlight(false) 移除高亮
    /// 
    /// 参数：
    /// - screenPosition: 当前指针在屏幕空间的位置
    /// </summary>
    private void UpdatePassengerSelection(Vector2 screenPosition)
    {
        // 获取所有乘客列表
        var passengerList = PassengerMgr.Instance.passengerList;
        if (passengerList == null)
            return;

        Passenger nearestPassenger = null;
        float minDistance = float.MaxValue;

        // 获取铃铛的世界坐标四个角（用于碰撞检测）
        Vector3[] bellCorners = new Vector3[4];
        rectTransform.GetWorldCorners(bellCorners);

        // 遍历所有乘客进行检测
        foreach (var passenger in passengerList)
        {
            // 基本有效性检查
            if (passenger == null || !passenger.gameObject.activeSelf)
                continue;

            // 获取乘客的 RectTransform（优先使用 mainRender，备选使用自身）
            // mainRender 通常是乘客显示图像的组件，更精确
            RectTransform passengerRect = passenger.mainRender != null
                ? passenger.mainRender.rectTransform
                : passenger.GetComponent<RectTransform>();

            if (passengerRect == null)
                continue;

            // ===== 第一层检测：直接覆盖检测 =====
            // 检查铃铛中心点是否在乘客区域内
            Vector2 bellCenter = (Vector2)rectTransform.position;
            if (RectTransformUtility.RectangleContainsScreenPoint(passengerRect, bellCenter, parentCanvas.worldCamera))
            {
                // 铃铛中心在乘客区域内，计算到乘客中心的距离
                Vector2 passengerCenter = (Vector2)passengerRect.position;
                float distance = Vector2.Distance(bellCenter, passengerCenter);

                // 更新最近乘客
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPassenger = passenger;
                }
                continue;  // 已检测此乘客，跳过第二层检测
            }

            // ===== 第二层检测：距离范围检测 =====
            // 若不在区域内，检查是否在检测半径范围内
            Vector2 passengerScreenPos = RectTransformUtility.WorldToScreenPoint(
                parentCanvas.worldCamera,
                passengerRect.position
            );

            // 计算屏幕空间距离
            float screenDistance = Vector2.Distance(screenPosition, passengerScreenPos);

            // 若在检测半径内，且距离更近，则更新最近乘客
            if (screenDistance <= detectionRadius && screenDistance < minDistance)
            {
                minDistance = screenDistance;
                nearestPassenger = passenger;
            }
        }

        // ===== 更新选中乘客的高亮状态 =====
        if (nearestPassenger != currentSelectedPassenger)
        {
            // 移除旧选中乘客的高亮
            if (currentSelectedPassenger != null)
                currentSelectedPassenger.SetHighlight(false);

            // 更新新选中乘客
            currentSelectedPassenger = nearestPassenger;

            // 添加新选中乘客的高亮
            if (currentSelectedPassenger != null)
                currentSelectedPassenger.SetHighlight(true);
        }
    }

    /// <summary>
    /// 清除乘客的选中状态（取消高亮）
    /// 
    /// 功能：
    /// - 将当前选中乘客的高亮移除
    /// - 清空 currentSelectedPassenger 引用
    /// 
    /// 调用时机：
    /// - 正常模式的拖动结束时
    /// - 新乘客被选中时（旧乘客）
    /// - 计时过期时
    /// 
    /// 依赖：
    /// - currentSelectedPassenger: 当前选中的乘客对象
    /// </summary>
    private void ClearPassengerSelection()
    {
        if (currentSelectedPassenger != null)
        {
            currentSelectedPassenger.SetHighlight(false);
            currentSelectedPassenger = null;
        }
    }
    #endregion
}