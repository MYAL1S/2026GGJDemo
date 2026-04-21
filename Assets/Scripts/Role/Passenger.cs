using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

/// <summary>
/// 乘客类 - 管理电梯中的单个乘客
/// 
/// 功能描述：
/// 1. 乘客显示与渲染
///    - mainRender：乘客的主显示图像（正常或幽灵形态）
///    - ghostFeatureRenderer：用于显示鬼魂特征的额外渲染器
/// 
/// 2. 淡入淡出动画
///    - 通过 CanvasGroup 控制透明度
///    - 支持自定义时长和完成回调
///    - 用于乘客进入/离开电梯的动画效果
/// 
/// 3. 特殊乘客灵能恢复
///    - 特定乘客可为玩家恢复灵能
///    - 有恢复时间限制和频率限制
///    - 达到最大时长后失去能力
/// 
/// 4. UI交互与反馈
///    - 高亮显示（鼠标悬停时）
///    - 自动排序（多个乘客显示时的层级管理）
///    - 透明度控制
/// 
/// 5. 状态管理
///    - 轮次状态：isNewThisRound（本轮新进入）
///    - 伤害结算：hasDamageSettled（是否已结算伤害）
///    - 位置追踪：SlotIndex（所在槽位）
/// 
/// 工作流程：
/// - Init: 初始化乘客数据，配置 UI 组件
/// - FadeIn: 乘客进入时的淡入动画
/// - Update: 检查灵能恢复或特殊能力过期
/// - FadeOut: 乘客离开时的淡出动画
/// - OnDestroy: 清理状态
/// </summary>
public class Passenger : MonoBehaviour
{
    // ============ 显示组件字段 ============
    /// <summary>乘客的主显示图像（显示正常或幽灵形态）</summary>
    public Image mainRender;

    /// <summary>幽灵特征渲染器（用于显示特殊的鬼魂图标或效果）</summary>
    public Image ghostFeatureRenderer;

    // ============ 数据字段 ============
    /// <summary>
    /// 乘客信息（PassengerSO）
    /// 包含乘客的基础数据：名称、图像、是否为鬼魂等
    /// 通过 Init 方法设置
    /// </summary>
    [HideInInspector]
    public PassengerSO passengerInfo;

    // ============ 高亮相关字段 ============
    /// <summary>高亮时的颜色（鼠标悬停时显示此颜色）</summary>
    [SerializeField]
    private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    /// <summary>原始颜色（高亮前的原色）</summary>
    private Color originalColor = Color.white;

    /// <summary>当前是否处于高亮状态</summary>
    private bool isHighlighted = false;

    // ============ UI 组件字段 ============
    /// <summary>Canvas 组件（用于排序和显示管理）</summary>
    private Canvas canvas;

    /// <summary>
    /// CanvasGroup 组件（用于控制淡入淡出效果）
    /// 通过修改 alpha 值实现透明度变化
    /// 也可控制交互是否响应（blocksRaycasts）
    /// </summary>
    private CanvasGroup canvasGroup;

    // ============ 排序顺序常量 ============
    /// <summary>排序顺序基数（所有乘客的基础排序值）</summary>
    private const int BASE_SORTING_ORDER = 5;

    /// <summary>排序顺序最大值（防止排序值过大）</summary>
    private const int MAX_SORTING_ORDER = 90;

    // ============ 特殊乘客灵能恢复系统 ============
    /// <summary>
    /// 特殊乘客灵能恢复功能区域
    /// 
    /// 功能说明：
    /// 某些乘客具有特殊能力，可以为玩家恢复灵能（精神力）
    /// 恢复遵循以下规则：
    /// - 恢复频率：每 psychicRestoreInterval 秒恢复一次
    /// - 恢复数量：每次恢复 psychicRestoreAmount 点灵能
    /// - 最大时长：恢复持续 maxRestoreDuration 秒后失去能力
    /// - 状态追踪：hasLostSpecialAbility 标志能力是否已过期
    /// 
    /// 典型流程：
    /// 1. Init() 调用 InitSpecialPassengerAbility()
    /// 2. 如果是特殊乘客，调用 StartPsychicRestore()
    /// 3. Update() 监控恢复定时器和总时长
    /// 4. 每 psychicRestoreInterval 秒调用 GameDataMgr.AddPsychicPower()
    /// 5. 达到 maxRestoreDuration 时调用 LoseSpecialAbility()
    /// </summary>
    #region 特殊乘客灵能恢复

    /// <summary>是否正在进行灵能恢复</summary>
    private bool isRestoringPsychic = false;

    /// <summary>灵能恢复计时器（用于控制恢复频率）</summary>
    private float psychicRestoreTimer = 0f;

    /// <summary>
    /// 灵能恢复间隔（秒）
    /// 每隔此时间，为玩家恢复一次灵能
    /// 默认值：2秒
    /// </summary>
    [SerializeField]
    private float psychicRestoreInterval = 2f;

    /// <summary>
    /// 灵能恢复最大持续时间（秒）
    /// 当总恢复时间达到此值后，乘客失去恢复能力
    /// 默认值：10秒（即可恢复5次灵能）
    /// </summary>
    [SerializeField]
    private float maxRestoreDuration = 10f;

    /// <summary>累计恢复时间（秒），用于判断是否超过最大时长</summary>
    private float totalRestoreTime = 0f;

    /// <summary>
    /// 每次恢复的灵能数量
    /// 每当 psychicRestoreInterval 时间到达时，恢复此数量的灵能
    /// 默认值：1点
    /// </summary>
    [SerializeField]
    private int psychicRestoreAmount = 1;

    /// <summary>
    /// 标志该乘客是否已失去特殊能力
    /// true = 已过期，不再恢复灵能
    /// false = 能力仍有效或未启用特殊能力
    /// </summary>
    private bool hasLostSpecialAbility = false;

    #endregion

    // ============ 轮次和伤害结算状态 ============
    /// <summary>
    /// 是否为本轮新进入的乘客（本轮结算跳过）
    /// 
    /// 用途：
    /// - 新进入的乘客在本轮内不参与伤害结算
    /// - 每轮开始时标记新乘客为 true
    /// - 轮次结束时调用 ClearNewThisRoundMark() 设为 false
    /// 
    /// 流程：
    /// 1. 乘客进入电梯时，调用 MarkAsNewThisRound() 设为 true
    /// 2. 本轮伤害计算时，跳过此标记为 true 的乘客
    /// 3. 轮次结束时，调用 ClearNewThisRoundMark() 设为 false
    /// </summary>
    public bool isNewThisRound = false;

    /// <summary>
    /// 是否已经结算过伤害（鬼魂只结算一次）
    /// 
    /// 用途：
    /// - 防止单个乘客被重复结算伤害
    /// - 对于鬼魂类型的乘客尤其重要（鬼魂只能结算一次）
    /// - 正常乘客也需要此标志防止重复结算
    /// 
    /// 流程：
    /// 1. 伤害计算前，检查此标志是否为 false
    /// 2. 执行伤害结算
    /// 3. 调用 MarkDamageSettled() 设为 true
    /// 4. 后续伤害计算将跳过此乘客
    /// </summary>
    public bool hasDamageSettled = false;

    /// <summary>
    /// 乘客所在的槽位索引
    /// 
    /// 用途：
    /// - 记录乘客在电梯中的位置（0-based索引）
    /// - 用于快速定位乘客、管理位置顺序
    /// 
    /// 取值范围：
    /// - -1 = 未分配（初始状态）
    /// - 0~N = 电梯中的槽位位置
    /// 
    /// 更新时机：
    /// - 乘客进入电梯时由 PassengerMgr 设置
    /// - 乘客离开时设回 -1
    /// </summary>
    public int SlotIndex { get; set; } = -1;

    /// <summary>
    /// 设置乘客的位置和缩放
    /// 
    /// 功能说明：
    /// 根据电梯UI的布局，调整乘客的显示位置和缩放大小
    /// 通过 RectTransform 实现 Canvas 坐标系的位置调整
    /// 
    /// 参数说明：
    /// - position: 相对于 Canvas 的锚点位置（anchoredPosition）
    ///   X轴：水平位置（向右为正）
    ///   Y轴：竖直位置（向上为正）
    /// - scale: 缩放比例（支持非均匀缩放）
    ///   (1, 1, 1) = 原始大小
    ///   (0.5, 0.5, 1) = 缩小到一半
    ///   (1.5, 1.5, 1) = 放大到1.5倍
    /// 
    /// 使用场景：
    /// - 初始化乘客位置
    /// - 乘客进入/离开电梯时的位置调整
    /// - 动态布局调整
    /// 
    /// 实现细节：
    /// - 使用 RectTransform 而非 Transform 处理 Canvas 坐标
    /// - anchoredPosition 基于 Canvas 的 anchor 点
    /// - localScale 应用于该 Transform 及其子物体
    /// 
    /// 典型调用：
    /// passenger.SetPositionAndScale(new Vector2(100, 50), new Vector3(1, 1, 1));
    /// </summary>
    public void SetPositionAndScale(Vector2 position, Vector3 scale)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.localScale = scale;
        }
    }

    /// <summary>
    /// 初始化乘客数据和UI组件
    /// 
    /// 功能说明：
    /// 这是乘客生命周期的第一步，负责：
    /// 1. 设置乘客数据（passengerSO）
    /// 2. 配置显示图像和幽灵图像
    /// 3. 初始化 Canvas 相关组件
    /// 4. 设置初始透明度为 0（等待 FadeIn 动画）
    /// 5. 初始化特殊乘客的灵能恢复能力
    /// 
    /// 参数说明：
    /// - passengerSO: ScriptableObject 数据对象，包含：
    ///   * passengerName: 乘客名称
    ///   * normalSprite: 正常状态的图像
    ///   * ghostSprite: 鬼魂状态的图像
    ///   * isGhost: 是否为鬼魂
    ///   * isSpecialPassenger: 是否为特殊乘客（可恢复灵能）
    ///   * oderInLayer: 排序顺序
    /// 
    /// 初始化步骤详解：
    /// 
    /// 1. 设置乘客数据
    ///    passengerInfo = passengerSO;
    /// 
    /// 2. 配置主显示图像（mainRender）
    ///    - 设置精灵图像
    ///    - SetNativeSize() 使用原生纹理大小
    ///    - alphaHitTestMinimumThreshold = 0.1f 
    ///      意味着透明度 < 10% 的像素不响应点击（穿透）
    /// 
    /// 3. 配置幽灵特征渲染器（ghostFeatureRenderer）
    ///    - 根据 isGhost 选择显示对应精灵
    ///    - SetNativeSize() 调整为原始大小
    ///    - 初始设为非活跃状态（SetActive(false)）
    /// 
    /// 4. 记录原始颜色（用于高亮显示还原）
    ///    originalColor = mainRender.color;
    /// 
    /// 5. 初始化 CanvasGroup（透明度控制）
    ///    - 如果不存在则自动添加组件
    ///    - 用于实现淡入淡出效果
    /// 
    /// 6. 初始化 Canvas（渲染和排序）
    ///    - overrideSorting = true 启用自定义排序
    ///    - sortingOrder 使用基础值 + PassengerSO 的排序值
    ///    - 受 MAX_SORTING_ORDER 限制，防止过大
    /// 
    /// 7. 添加 GraphicRaycaster（UI射线检测）
    ///    - 用于处理鼠标指针交互事件
    /// 
    /// 8. 设置初始透明度为 0
    ///    canvasGroup.alpha = 0f;
    ///    - 完全透明状态
    ///    - 等待 FadeIn() 方法执行动画
    /// 
    /// 9. 初始化特殊乘客灵能恢复
    ///    InitSpecialPassengerAbility();
    /// 
    /// 流程图：
    /// Init() 
    ///  ├─ 设置 passengerInfo
    ///  ├─ 配置 mainRender（图像 + alphaHitTest）
    ///  ├─ 配置 ghostFeatureRenderer（条件性图像）
    ///  ├─ 初始化 CanvasGroup（创建或获取）
    ///  ├─ 初始化 Canvas（排序设置）
    ///  ├─ 添加 GraphicRaycaster（交互检测）
    ///  ├─ 设置透明度 = 0
    ///  └─ 初始化特殊能力
    /// 
    /// 注意事项：
    /// - 必须在使用 FadeIn() 前调用此方法
    /// - PassengerSO 中的 oderInLayer 值会影响显示顺序
    /// - 幽灵图像初始为非活跃状态，需通过外部代码激活
    /// - alphaHitTestMinimumThreshold = 0.1f 可能需要根据图像调整
    /// </summary>
    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;

        mainRender.sprite = passengerSO.normalSprite;
        mainRender.SetNativeSize();

        // 设置阈值为 0.1，意味着透明度低于 10% 的区域将不再响应点击，而是穿透过去
        mainRender.alphaHitTestMinimumThreshold = 0.1f;

        if (ghostFeatureRenderer != null)
        {

            ghostFeatureRenderer.sprite = passengerSO.isGhost
                ? passengerSO.ghostSprite
                : passengerSO.normalSprite;
            ghostFeatureRenderer.SetNativeSize();
            ghostFeatureRenderer.gameObject.SetActive(false);
        }

        originalColor = mainRender.color;

        // 2. 初始化 CanvasGroup 并设为透明
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Min(BASE_SORTING_ORDER + passengerSO.oderInLayer, MAX_SORTING_ORDER);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // 初始设为 0 (完全透明)，等待调用 FadeIn
        canvasGroup.alpha = 0f;

        // 初始化特殊乘客灵能恢复
        InitSpecialPassengerAbility();
    }

    #region 淡入淡出相关
    /// <summary>
    /// 淡入动画 - 乘客进入时的动画效果
    /// 
    /// 功能说明：
    /// 通过平滑的透明度过渡，使乘客从不可见逐渐变为可见
    /// 这是乘客进入电梯的标准动画效果
    /// 
    /// 参数说明：
    /// - duration: 动画持续时间（秒），默认 0.5 秒
    ///   * 时间越短，淡入越快
    ///   * 时间越长，淡入越平缓
    /// 
    /// 动画细节：
    /// - 起始透明度：0（完全透明）
    /// - 目标透明度：1（完全不透明）
    /// - 变化方式：线性插值（Lerp）
    /// 
    /// 工作原理：
    /// 1. 确保 CanvasGroup 存在（初始化如果需要）
    /// 2. 激活游戏物体（gameObject.SetActive(true)）
    /// 3. 启动 FadeRoutine 协程
    /// 4. 协程在指定时间内将 alpha 从 0 变为 1
    /// 
    /// 流程示例：
    /// // 乘客进入电梯
    /// passenger.FadeIn(0.5f);  // 0.5秒淡入动画
    /// // 乘客逐渐显示在屏幕上
    /// 
    /// 注意事项：
    /// - 必须在 Init() 后调用
    /// - FadeIn 期间可以被 FadeOut 中断
    /// - 动画不阻塞代码执行（使用协程）
    /// - 多次调用 FadeIn 会启动多个动画协程（如果需要控制，应先 StopCoroutine）
    /// </summary>
    public void FadeIn(float duration = 0.5f)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        // 确保物体是激活的
        gameObject.SetActive(true);
        StartCoroutine(FadeRoutine(0f, 1f, duration, null));
    }

    /// <summary>
    /// 淡出动画 - 乘客离开时的动画效果
    /// 
    /// 功能说明：
    /// 通过平滑的透明度过渡，使乘客从可见逐渐变为不可见
    /// 这是乘客离开电梯的标准动画效果
    /// 
    /// 参数说明：
    /// - duration: 动画持续时间（秒），默认 0.5 秒
    ///   * 时间越短，淡出越快
    ///   * 时间越长，淡出越平缓
    /// - onComplete: 动画完成后的回调函数（可选）
    ///   * 用于在淡出完成后执行清理操作
    ///   * 例如：销毁游戏物体、重置状态等
    /// 
    /// 动画细节：
    /// - 起始透明度：当前 alpha 值
    /// - 目标透明度：0（完全透明）
    /// - 变化方式：线性插值（Lerp）
    /// 
    /// 重要的交互管理：
    /// - 设置 canvasGroup.blocksRaycasts = false
    /// - 防止淡出期间被再次点击/交互
    /// - 这很重要，否则用户可能在淡出动画中仍然能触发交互
    /// 
    /// 工作原理：
    /// 1. 确保 CanvasGroup 存在
    /// 2. 禁用射线检测（blocksRaycasts = false）
    /// 3. 如果物体在层级中激活，启动 FadeRoutine 协程
    /// 4. 协程在指定时间内将 alpha 变为 0
    /// 5. 协程完成后调用 onComplete 回调
    /// 
    /// 流程示例：
    /// // 乘客离开电梯
    /// passenger.FadeOut(0.5f, () => {
    ///     // 淡出完成后的处理
    ///     Destroy(passenger.gameObject);  // 销毁游戏物体
    ///     PassengerMgr.Instance.RemovePassenger(passenger);  // 移除引用
    /// });
    /// 
    /// 注意事项：
    /// - 如果物体已不激活，直接调用回调（不执行动画）
    /// - blocksRaycasts = false 很关键，防止淡出中被交互
    /// - 回调在淡出完成时自动调用，无需手动调用
    /// - 可选的 onComplete 参数允许后续操作链式调用
    /// </summary>
    public void FadeOut(float duration = 0.5f, Action onComplete = null)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // 禁用交互，防止淡出过程中被再次点击
        canvasGroup.blocksRaycasts = false;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, duration, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 淡入淡出核心协程
    /// 
    /// 功能说明：
    /// 这是实现淡入淡出效果的核心协程
    /// 通过时间线性插值，实现平滑的透明度变化
    /// 
    /// 参数说明：
    /// - startAlpha: 起始透明度（0 ~ 1）
    /// - endAlpha: 目标透明度（0 ~ 1）
    /// - duration: 动画持续时间（秒）
    /// - onComplete: 动画完成后的回调函数
    /// 
    /// 算法说明（线性插值）：
    /// 假设 duration = 0.5秒，startAlpha = 0，endAlpha = 1
    /// 
    /// 时间线    进度(t)    alpha值      说明
    /// -----    -------    --------    -----
    /// 0.0s     0.0        0.0         刚开始
    /// 0.1s     0.2        0.2         进行20%
    /// 0.25s    0.5        0.5         进行50%（中点）
    /// 0.4s     0.8        0.8         进行80%
    /// 0.5s     1.0        1.0         完成
    /// 
    /// 插值公式：
    /// alpha = Lerp(startAlpha, endAlpha, t)
    ///       = startAlpha + (endAlpha - startAlpha) * t
    /// 
    /// 执行流程：
    /// 1. 初始化计时器 timer = 0
    /// 2. 设置起始透明度 alpha = startAlpha
    /// 3. 循环直到 timer >= duration：
    ///    a. 增加 timer += Time.deltaTime
    ///    b. 计算进度 t = Clamp01(timer / duration)
    ///    c. 更新 alpha = Lerp(startAlpha, endAlpha, t)
    ///    d. yield return null（等待下一帧）
    /// 4. 强制设置最终值 alpha = endAlpha
    /// 5. 调用回调函数 onComplete()
    /// 
    /// 实现细节：
    /// - Time.deltaTime 是帧间隔时间（秒）
    /// - Mathf.Clamp01 限制 t 在 0~1 范围内
    /// - yield return null 暂停协程，等待下一帧
    /// - 最后强制设置 endAlpha 确保精确的最终值
    /// 
    /// 使用示例：
    /// // 淡入：从0变为1，历时0.5秒，完成后打印日志
    /// StartCoroutine(FadeRoutine(0f, 1f, 0.5f, () => Debug.Log("淡入完成")));
    /// 
    /// // 淡出：从当前值变为0，历时1秒
    /// StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, 1f, null));
    /// 
    /// 性能考虑：
    /// - 每帧调用一次，但开销很小
    /// - 不涉及物理计算或复杂业务逻辑
    /// - 可以多个乘客同时运行而不影响性能
    /// 
    /// 注意事项：
    /// - 如果在动画中途销毁 gameObject，协程会自动停止
    /// - CanvasGroup 必须存在且有效
    /// - onComplete 可为 null（通过 onComplete?.Invoke() 安全调用）
    /// </summary>
    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        float timer = 0f;
        if (canvasGroup != null) canvasGroup.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = endAlpha;

        onComplete?.Invoke();
    }
    #endregion

    #region 特殊乘客相关
    /// <summary>
    /// 初始化特殊乘客能力
    /// 
    /// 功能说明：
    /// 如果当前乘客是特殊乘客且未失去能力，则自动开始灵能恢复
    /// 这是 Init() 流程的最后一步
    /// 
    /// 触发条件：
    /// 1. passengerInfo 存在
    /// 2. passengerInfo.isSpecialPassenger 为 true
    /// 3. hasLostSpecialAbility 为 false
    /// 
    /// 工作流程：
    /// 如果满足所有条件，调用 StartPsychicRestore()
    /// 
    /// 注意事项：
    /// - 由 Init() 自动调用
    /// - 无需手动调用此方法
    /// </summary>
    private void InitSpecialPassengerAbility()
    {
        if (passengerInfo != null && passengerInfo.isSpecialPassenger && !hasLostSpecialAbility)
        {
            StartPsychicRestore();
        }
    }

    /// <summary>
    /// 开始恢复灵能
    /// 
    /// 功能说明：
    /// 启动特殊乘客的灵能恢复机制
    /// 该乘客将以设定的间隔为玩家恢复灵能
    /// 
    /// 前置条件：
    /// 1. hasLostSpecialAbility 必须为 false（能力未过期）
    /// 2. passengerInfo 必须存在
    /// 3. passengerInfo.isSpecialPassenger 必须为 true
    /// 
    /// 初始化步骤：
    /// 1. isRestoringPsychic = true（设为恢复状态）
    /// 2. psychicRestoreTimer = 0f（重置恢复定时器）
    /// 3. totalRestoreTime = 0f（重置总恢复时间）
    /// 
    /// 恢复机制详解：
    /// - Update() 每帧检查 isRestoringPsychic 状态
    /// - 累加 psychicRestoreTimer
    /// - 当 psychicRestoreTimer >= psychicRestoreInterval 时：
    ///   a. 调用 GameDataMgr.AddPsychicPower(psychicRestoreAmount)
    ///   b. 重置 psychicRestoreTimer = 0
    /// - 同时累加 totalRestoreTime
    /// - 当 totalRestoreTime >= maxRestoreDuration 时调用 LoseSpecialAbility()
    /// 
    /// 参数值（默认）：
    /// - psychicRestoreInterval: 2 秒（每隔2秒恢复一次）
    /// - psychicRestoreAmount: 1 点（每次恢复1点灵能）
    /// - maxRestoreDuration: 10 秒（最多恢复10秒，即5次）
    /// 
    /// 时间轴示例（默认参数）：
    /// 时间(秒)    事件                        状态
    /// --------    ------                      -----
    /// 0.0         StartPsychicRestore()      恢复开始
    /// 2.0         恢复1点灵能                 总计: 1点
    /// 4.0         恢复1点灵能                 总计: 2点
    /// 6.0         恢复1点灵能                 总计: 3点
    /// 8.0         恢复1点灵能                 总计: 4点
    /// 10.0        恢复1点灵能，然后失去能力   总计: 5点
    /// 
    /// 调用时机：
    /// - 通常由 InitSpecialPassengerAbility() 自动调用
    /// - 也可以通过其他游戏逻辑手动调用（如事件触发）
    /// </summary>
    public void StartPsychicRestore()
    {
        if (hasLostSpecialAbility) return;
        if (passengerInfo == null || !passengerInfo.isSpecialPassenger) return;

        isRestoringPsychic = true;
        psychicRestoreTimer = 0f;
        totalRestoreTime = 0f;
    }

    /// <summary>
    /// 停止恢复灵能
    /// 
    /// 功能说明：
    /// 立即停止特殊乘客的灵能恢复
    /// 通常在以下情况调用：
    /// - 乘客离开电梯前
    /// - 临时暂停恢复
    /// - 遇到特殊游戏事件
    /// 
    /// 工作原理：
    /// 1. 检查 isRestoringPsychic 是否为 true
    /// 2. 如果为 false，直接返回（已停止，无需重复操作）
    /// 3. 设置 isRestoringPsychic = false
    /// 4. 输出调试日志，显示已恢复的时间
    /// 
    /// 效果说明：
    /// - 后续 Update() 不再执行恢复逻辑
    /// - 已恢复的灵能保留，不会回收
    /// - 如需重新启动，调用 StartPsychicRestore()
    /// 
    /// 日志输出示例：
    /// [Passenger] Alice 停止恢复灵能，已恢复 5.2 秒
    /// 
    /// 注意事项：
    /// - 停止后无法自动重启（需手动调用 StartPsychicRestore）
    /// - 不会重置总恢复时间（继续计算）
    /// - 不会触发 LoseSpecialAbility() 事件
    /// </summary>
    public void StopPsychicRestore()
    {
        if (!isRestoringPsychic) return;

        isRestoringPsychic = false;
    }

    /// <summary>
    /// Update 方法 - 每帧更新灵能恢复系统
    /// 
    /// 功能说明：
    /// 监控特殊乘客的灵能恢复过程，定时为玩家恢复灵能
    /// 同时监控恢复时间是否超过最大限制，自动过期能力
    /// 
    /// 执行条件：
    /// 每帧都会检查以下条件：
    /// 1. !isRestoringPsychic：恢复功能已停止则退出
    /// 2. hasLostSpecialAbility：特殊能力已过期则退出
    /// 
    /// 核心逻辑：
    /// 
    /// 1. 累计时间
    ///    totalRestoreTime += Time.deltaTime;  // 总恢复时间
    ///    psychicRestoreTimer += Time.deltaTime;  // 恢复间隔计时
    /// 
    /// 2. 检查恢复频率
    ///    if (psychicRestoreTimer >= psychicRestoreInterval)
    ///    {
    ///        psychicRestoreTimer = 0f;  // 重置计时器
    ///        GameDataMgr.Instance.AddPsychicPower(psychicRestoreAmount);  // 恢复灵能
    ///        // 输出调试日志
    ///    }
    /// 
    /// 3. 检查最大恢复时间
    ///    if (totalRestoreTime >= maxRestoreDuration)
    ///    {
    ///        LoseSpecialAbility();  // 调用能力过期处理
    ///    }
    /// 
    /// 时间轴示例（默认参数）：
    /// Frame   时间(s)  Event                          totalRestoreTime  isRestoringPsychic
    /// -----   ------   -----                          ----------------  ------------------
    /// 1       0.016    Init + StartPsychicRestore()  0.0               true
    /// 125     2.0      psychicRestoreTimer >= 2s      2.0               true
    ///                  恢复1点灵能，timer重置
    /// 250     4.0      psychicRestoreTimer >= 2s      4.0               true
    ///                  恢复1点灵能，timer重置
    /// ...
    /// 625     10.0     psychicRestoreTimer >= 2s      10.0              true
    ///                  恢复1点灵能，timer重置
    ///                  totalRestoreTime >= 10s
    ///                  调用 LoseSpecialAbility()
    /// 626     10.016   isRestoringPsychic = false     10.0              false
    ///                  Update 早期返回，不再执行
    /// 
    /// 参数说明：
    /// - Time.deltaTime: 上一帧到本帧的时间差（秒）
    ///   * 通常 1/60 ≈ 0.0167 秒（60帧/秒）
    ///   * 可能因性能问题而变化
    /// 
    /// 可配置参数（来自 SerializeField）：
    /// - psychicRestoreInterval: 恢复间隔（默认2秒）
    /// - psychicRestoreAmount: 每次恢复数量（默认1点）
    /// - maxRestoreDuration: 最大恢复时长（默认10秒）
    /// 
    /// 重要的交互：
    /// - 通过 GameDataMgr.Instance 全局单例恢复玩家灵能
    /// - 触发 E_SpecialPassengerExpired 事件通知其他系统
    /// - 状态变化通过调试日志记录
    /// 
    /// 性能考虑：
    /// - 早期返回条件优化（!isRestoringPsychic）
    /// - 避免不必要的计算
    /// - 可以多个特殊乘客同时运行
    /// 
    /// 注意事项：
    /// - 必须在 StartPsychicRestore() 后才会执行恢复逻辑
    /// - Stop/LoseSpecialAbility 会停止 Update 的恢复处理
    /// - 框架相关：依赖 Time.deltaTime 的准确性
    /// </summary>
    private void Update()
    {
        if (!isRestoringPsychic || hasLostSpecialAbility) return;

        totalRestoreTime += Time.deltaTime;
        psychicRestoreTimer += Time.deltaTime;

        // 每隔一段时间恢复一次灵能
        if (psychicRestoreTimer >= psychicRestoreInterval)
        {
            psychicRestoreTimer = 0f;
            GameDataMgr.Instance.AddPsychicPower(psychicRestoreAmount);
        }

        // 达到最大恢复时间，失去特殊能力
        if (totalRestoreTime >= maxRestoreDuration)
        {
            LoseSpecialAbility();
        }
    }

    /// <summary>
    /// 失去特殊能力
    /// 
    /// 功能说明：
    /// 特殊乘客的灵能恢复能力已到期
    /// 停止恢复灵能，标记能力已失去，触发相关事件
    /// 
    /// 触发条件：
    /// - 通常由 Update() 在 totalRestoreTime >= maxRestoreDuration 时调用
    /// - 也可手动调用以强制禁用能力
    /// 
    /// 执行步骤：
    /// 1. 检查 hasLostSpecialAbility
    ///    如已失去能力，直接返回（防止重复处理）
    /// 2. hasLostSpecialAbility = true
    ///    标记为已失去能力
    /// 3. isRestoringPsychic = false
    ///    停止恢复机制
    /// 4. 输出调试日志
    ///    记录总恢复时间
    /// 5. EventCenter.Instance.EventTrigger(E_EventType.E_SpecialPassengerExpired)
    ///    触发事件，通知其他系统（如 UI 更新、音效播放等）
    /// 
    /// 事件说明：
    /// - E_SpecialPassengerExpired：特殊乘客能力过期事件
    /// - 用于：
    ///   a. 更新 UI 显示
    ///   b. 播放过期音效
    ///   c. 更新游戏状态
    ///   d. 触发相关文本提示
    /// 
    /// 效果说明：
    /// - 虽然能力失去，但乘客仍保留在电梯中
    /// - 乘客显示可能改变（如去除闪光/特效）
    /// - 不会自动移除乘客或改变透明度
    /// - 需要外部逻辑（如 PassengerMgr）处理后续步骤
    /// 
    /// 日志输出示例：
    /// [Passenger] Alice 已失去特殊能力，共恢复了 10.0 秒
    /// 
    /// 注意事项：
    /// - 一旦调用，无法重新获得能力（需要创建新乘客实例）
    /// - 无需手动调用 StopPsychicRestore()（已在此处调用）
    /// - 确保事件监听者已订阅 E_SpecialPassengerExpired
    /// </summary>
    private void LoseSpecialAbility()
    {
        if (hasLostSpecialAbility) return;

        hasLostSpecialAbility = true;
        isRestoringPsychic = false;

        EventCenter.Instance.EventTrigger(E_EventType.E_SpecialPassengerExpired);
    }
    #endregion

    /// <summary>
    /// 设置 Canvas 排序顺序
    /// 
    /// 功能说明：
    /// 修改乘客的显示层级（Z-order/排序值）
    /// 用于控制多个乘客重叠时的显示顺序
    /// 
    /// 参数说明：
    /// - order: 排序值（整数）
    ///   值越大，显示越靠前（在其他乘客上方）
    /// 
    /// 实现细节：
    /// 最终的 sortingOrder = Mathf.Min(BASE_SORTING_ORDER + order, MAX_SORTING_ORDER)
    /// 
    /// 示例：
    /// - BASE_SORTING_ORDER = 5
    /// - MAX_SORTING_ORDER = 90
    /// 
    /// SetSortingOrder(10)  → sortingOrder = Min(5 + 10, 90) = 15
    /// SetSortingOrder(100) → sortingOrder = Min(5 + 100, 90) = 90（被限制）
    /// 
    /// 使用场景：
    /// - 鼠标悬停时提升排序，使其显示在最前
    /// - 点击时调整显示顺序
    /// - 电梯门打开/关闭时的动画效果
    /// 
    /// 注意事项：
    /// - 必须在 Init() 后才能正确使用
    /// - Canvas 存在才能生效
    /// - MAX_SORTING_ORDER 限制了最大值
    /// </summary>
    public void SetSortingOrder(int order)
    {
        if (canvas != null)
            canvas.sortingOrder = Mathf.Min(BASE_SORTING_ORDER + order, MAX_SORTING_ORDER);
    }

    /// <summary>
    /// 设置高亮显示状态
    /// 
    /// 功能说明：
    /// 改变乘客图像的颜色，实现高亮效果
    /// 通常在以下场景使用：
    /// 1. 鼠标悬停时高亮显示
    /// 2. 鼠标移出时取消高亮
    /// 3. 选中时视觉反馈
    /// 
    /// 参数说明：
    /// - highlight: 是否启用高亮
    ///   true = 使用 highlightColor（通常是亮黄色 1, 1, 0.5, 1）
    ///   false = 使用 originalColor（原始颜色）
    /// 
    /// 实现优化：
    /// if (isHighlighted == highlight) return;
    /// 这个检查可以防止重复设置相同的状态
    /// 减少不必要的 UI 更新
    /// 
    /// 颜色说明：
    /// - originalColor: 从 Init() 获取的初始颜色（通常白色）
    /// - highlightColor: SerializeField 定义的高亮颜色
    ///   默认值: (1f, 1f, 0.5f, 1f) - 浅黄色
    ///   (R, G, B, A) 范围都是 0~1
    /// 
    /// 使用示例：
    /// // 鼠标进入时高亮
    /// passenger.SetHighlight(true);  // 变为黄色
    /// 
    /// // 鼠标离开时取消高亮
    /// passenger.SetHighlight(false);  // 恢复原色
    /// 
    /// 流程图：
    /// SetHighlight(highlight)
    ///  ├─ 检查是否与当前状态相同
    ///  │  └─ 若相同则返回（优化）
    ///  ├─ 更新 isHighlighted 标志
    ///  └─ 修改 mainRender.color
    ///     ├─ highlight=true  → highlightColor (1, 1, 0.5, 1)
    ///     └─ highlight=false → originalColor
    /// 
    /// 性能注意：
    /// - 只在状态改变时更新 UI
    /// - 颜色改变是廉价操作
    /// - 可频繁使用而不影响性能
    /// 
    /// 依赖关系：
    /// - originalColor：在 Init() 时设置
    /// - mainRender.color：在 Init() 时配置的 Image 组件
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;
        isHighlighted = highlight;
        mainRender.color = highlight ? highlightColor : originalColor;
    }

    /// <summary>
    /// 标记为本轮新进入的乘客
    /// 
    /// 功能说明：
    /// 将乘客标记为本轮新进入
    /// 这样本轮的伤害结算会跳过此乘客
    /// 
    /// 调用时机：
    /// - 乘客通过淡入动画进入电梯时
    /// - 由 PassengerMgr 管理调用
    /// 
    /// 状态说明：
    /// - isNewThisRound = true
    /// - 伤害计算时会检查此标志
    /// - 下一轮结算时自动清除（调用 ClearNewThisRoundMark()）
    /// 
    /// 相关方法：
    /// - ClearNewThisRoundMark()：清除标记
    /// </summary>
    public void MarkAsNewThisRound()
    {
        isNewThisRound = true;
    }

    /// <summary>
    /// 清除本轮新进入标记（进入下一轮时调用）
    /// 
    /// 功能说明：
    /// 移除本轮新进入的标记
    /// 使乘客在下一轮参与伤害结算
    /// 
    /// 调用时机：
    /// - 游戏轮次结束时
    /// - 由 GameLevelMgr 或类似的轮次管理器调用
    /// 
    /// 流程示例：
    /// Round 1: 乘客进入 → MarkAsNewThisRound() → 伤害结算跳过
    /// Round 2: 调用 ClearNewThisRoundMark() → isNewThisRound = false
    ///          伤害结算时会处理此乘客
    /// 
    /// 相关方法：
    /// - MarkAsNewThisRound()：标记为新进入
    /// </summary>
    public void ClearNewThisRoundMark()
    {
        isNewThisRound = false;
    }

    /// <summary>
    /// 标记已结算伤害
    /// 
    /// 功能说明：
    /// 将乘客标记为已结算伤害
    /// 防止后续重复结算伤害
    /// 对于鬼魂类型尤其重要（鬼魂只能结算一次伤害）
    /// 
    /// 调用时机：
    /// - 伤害计算系统在处理乘客伤害后调用
    /// - 由 GameManager 或伤害结算模块调用
    /// 
    /// 效果说明：
    /// - hasDamageSettled = true
    /// - 后续伤害检查会跳过此乘客
    /// - 不会改变乘客外观或状态
    /// 
    /// 注意事项：
    /// - 一旦标记为已结算，无法重置（需要创建新乘客）
    /// - 与 isNewThisRound 配合使用
    ///   先检查 isNewThisRound（跳过新乘客）
    ///   再检查 hasDamageSettled（跳过已结算乘客）
    /// </summary>
    public void MarkDamageSettled()
    {
        hasDamageSettled = true;
    }

    /// <summary>
    /// OnDestroy - 清理资源和状态
    /// 
    /// 功能说明：
    /// Unity 生命周期方法
    /// 当乘客 GameObject 被销毁时自动调用
    /// 用于清理和重置状态，防止内存泄漏
    /// 
    /// 执行步骤：
    /// 1. 调用 StopPsychicRestore()
    ///    停止灵能恢复协程
    ///    如果乘客正在恢复灵能，立即停止
    /// 
    /// 2. 重置高亮状态
    ///    isHighlighted = false
    ///    防止幽灵状态
    /// 
    /// 调用时机：
    /// - 乘客离开电梯（淡出完成后）
    /// - 游戏场景卸载
    /// - 乘客被销毁
    /// 
    /// 重要的清理点：
    /// - 如果不停止灵能恢复，可能导致问题：
    ///   a. GameDataMgr 继续接收灵能恢复调用
    ///   b. EventCenter 继续触发事件
    ///   c. 内存泄漏
    /// 
    /// 性能考虑：
    /// - StopPsychicRestore() 有保护性检查
    /// - 即使乘客不是特殊乘客也安全
    /// - 重置状态是轻量级操作
    /// 
    /// 注意事项：
    /// - 由 Unity 自动调用，无需手动调用
    /// - OnDestroy 中应避免创建新对象
    /// - 确保所有协程都已停止
    /// </summary>
    private void OnDestroy()
    {
        StopPsychicRestore();
        isHighlighted = false;
    }
}
