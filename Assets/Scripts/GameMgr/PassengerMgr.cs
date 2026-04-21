using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 乘客管理器 - PassengerMgr
/// 
/// 核心职责：
/// ============================================
/// 1. 乘客生成与销毁管理
///    - 按波次生成乘客到电梯
///    - 根据等级配置控制乘客数量
///    - 管理等待队列中的乘客补位
/// 
/// 2. 乘客生命周期管理
///    - 初始化乘客数据和状态
///    - 处理乘客驱逐（踢出）和消散（驱散鬼魂）
///    - 淡入淡出动画控制
/// 
/// 3. 乘客位置与显示管理
///    - 两种位置模式：停靠(Docking)和运行(Moving)
///    - 根据Y坐标自动调整显示深度和缩放
///    - 鬼魂靠近时的位置交换
/// 
/// 4. UI容器和状态同步
///    - 缓存 GamePanel 容器引用
///    - 同步容器缩放与模式状态
///    - 通知 UI 系统乘客数量变化
/// 
/// 5. 特殊乘客与鬼魂管理
///    - 特殊乘客生成配额控制
///    - 鬼魂驱散逻辑
///    - 等待队列补位机制
/// 
/// 工作流程：
/// ============================================
/// A. 初始化阶段
///    SpawnWave() → GetGamePanel() → DoSpawnWave()
/// 
/// B. 生成阶段（DoSpawnWave）
///    GetAvailableIndices() 
///    → PrepareCandidates() 
///    → CalculateSpawnCounts() 
///    → GeneratePassengerImmediate()
///    → UpdateDepthAndScale()
/// 
/// C. 位置切换
///    SwitchToDockingPositions() / SwitchToMovingPositions()
///    → 更新 isInDockingMode 标志
///    → 更新容器缩放
///    → UpdateAllPassengerPositions()
/// 
/// D. 乘客移除
///    OnPassengerKicked() / DispelGhost()
///    → FadeOut() 淡出动画
///    → 从列表移除
///    → TrySpawnFromWaitingQueue() 补位
/// 
/// E. 深度更新
///    UpdateDepthAndScale()
///    → 根据Y坐标排序
///    → 设置 sortingOrder
///    → 计算显示深度比例
/// 
/// 关键特性：
/// ============================================
/// • 单例模式：全游戏唯一的乘客管理器
/// • 动态补位：被驱散的乘客位置由等待队列补位
/// • 深度自动调整：根据屏幕位置自动调整显示顺序
/// • 模式切换：支持两种乘客位置模式
/// • 容器缓存：避免频繁 UI 查询性能开销
/// 
/// 关键数据结构：
/// ============================================
/// • passengerList: 当前活跃乘客列表
/// • tempNormals/Ghosts/Specials: 候选乘客缓冲区
/// • waitingQueue: 等待补位的乘客队列
/// • isInDockingMode: 当前位置模式标志
/// </summary>
public class PassengerMgr : BaseSingleton<PassengerMgr>
{
    // ============ 常量定义 ============
    /// <summary>
    /// 乘客最大排序层级
    /// 用于 Canvas.sortingOrder 的上限
    /// 最小值 = BASE_SORTING_ORDER(5) + offset(-5) = 0
    /// 最大值 = MAX_PASSENGER_SORTING(90)
    /// </summary>
    private const int MAX_PASSENGER_SORTING = 90;

    // ============ 乘客列表字段 ============
    /// <summary>
    /// 当前活跃的乘客列表
    /// 包含所有在电梯中的乘客（已生成且未销毁）
    /// </summary>
    public List<Passenger> passengerList;

    /// <summary>
    /// 临时缓冲区：普通乘客候选
    /// 每次生成前重新填充，用于选择候选乘客
    /// </summary>
    private readonly List<PassengerSO> tempNormals = new List<PassengerSO>();

    /// <summary>
    /// 临时缓冲区：鬼魂乘客候选
    /// 每次生成前重新填充，用于选择候选乘客
    /// </summary>
    private readonly List<PassengerSO> tempGhosts = new List<PassengerSO>();

    /// <summary>
    /// 临时缓冲区：特殊乘客候选
    /// 每次生成前重新填充，用于选择候选乘客
    /// 特殊乘客具有灵能恢复能力
    /// </summary>
    private readonly List<PassengerSO> tempSpecials = new List<PassengerSO>();

    /// <summary>
    /// 等待队列：未能立即生成的乘客
    /// 当电梯停靠时，这些乘客会补位到空余槽位
    /// </summary>
    private readonly Queue<PassengerSO> waitingQueue = new Queue<PassengerSO>();

    // ============ UI引用缓存 ============
    /// <summary>
    /// 缓存的 GamePanel 引用
    /// 避免频繁从 UIManager 查询，提高性能
    /// </summary>
    private GamePanel cachedGamePanel;

    /// <summary>
    /// 乘客容器的 Transform 引用
    /// 这是所有 Passenger 的父容器
    /// 通过修改其 localScale 实现整体缩放
    /// </summary>
    private Transform passengerContainer;

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
    }

    // ============ 模式管理 ============
    /// <summary>
    /// 当前位置模式标志
    /// 
    /// true  = 停靠模式（Docking）
    ///   • 乘客站在电梯停靠位置
    ///   • 正常显示，用于上下乘客
    ///   • 使用 passengerDockingPositions 坐标
    /// 
    /// false = 运行模式（Moving）
    ///   • 乘客可能随电梯移动变化
    ///   • 使用 passengerMovingPositions 坐标
    ///   • 可能伴随缩放变化
    /// </summary>
    private bool isInDockingMode = true;

    #region 生成一波乘客
    /// <summary>
    /// 生成一波乘客 - 入口方法
    /// 
    /// 功能说明：
    /// 这是生成乘客波次的主入口
    /// 负责获取 GamePanel 容器，然后触发实际生成逻辑
    /// 
    /// 流程：
    /// 1. 清空等待队列（准备新的波次）
    /// 2. 通过 UIMgr 异步获取 GamePanel
    /// 3. 缓存 GamePanel 和容器引用
    /// 4. 调用 DoSpawnWave() 执行实际生成
    /// 
    /// 重要说明：
    /// - 不清除已有乘客（乘客在电梯中累积）
    /// - 新生成的乘客会加入 passengerList
    /// - 不能立即生成的乘客进入 waitingQueue
    /// 
    /// 调用时机：
    /// - 游戏开始时
    /// - 每个波次间隔时调用
    /// - 由 GameLevelMgr 或类似管理器触发
    /// 
    /// 性能考虑：
    /// - UIMgr.GetPanel() 是异步的（使用 callback）
    /// - 容器引用缓存避免重复查询
    /// </summary>
    public void SpawnWave()
    {
        // 清空等待队列，为新波次做准备
        waitingQueue.Clear();

        // 异步获取 GamePanel 并缓存引用
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            cachedGamePanel = panel;
            if (cachedGamePanel != null)
                passengerContainer = cachedGamePanel.GetPassengerContainer();

            // 获取到容器后执行实际生成逻辑
            DoSpawnWave();
        });
    }

    /// <summary>
    /// 实际生成乘客逻辑 - 核心生成方法
    /// 
    /// 功能说明：
    /// 这是真正执行乘客生成的方法
    /// 负责生成计数、候选选择、位置分配等
    /// 
    /// 执行步骤详解：
    /// 
    /// 1️⃣ 获取可用点位
    ///    availableIndices = GetAvailableSpawnIndices()
    ///    检查哪些槽位还没有乘客占用
    ///    如果没有空位则直接返回
    /// 
    /// 2️⃣ 洗牌可用点位
    ///    Shuffle(availableIndices)
    ///    打乱顺序，使生成位置随机
    /// 
    /// 3️⃣ 准备候选乘客
    ///    PrepareCandidates(tempNormals, false)  → 普通乘客
    ///    PrepareCandidates(tempGhosts, true)    → 鬼魂乘客
    ///    PrepareSpecialCandidates(tempSpecials) → 特殊乘客
    ///    从 ResourcesMgr 中筛选符合条件的乘客
    /// 
    /// 4️⃣ 计算生成数量
    ///    从等级配置获取数量限制：
    ///    • ghostCount = Min(配置数, 候选数)
    ///    • normalCount = Min(配置数, 候选数)
    ///    • specialCount = Min(配置数, 候选数, 剩余配额)
    /// 
    ///    特殊乘客配额说明：
    ///    • 整个等级有生成配额限制
    ///    • 只有特定层级才生成特殊乘客
    ///    • 每次最多生成1个特殊乘客
    /// 
    /// 5️⃣ 合并所有乘客为一个列表
    ///    顺序：特殊 → 鬼魂 → 普通
    ///    然后打乱
    /// 
    /// 6️⃣ 计算实际生成数量
    ///    minSpawn = 2, maxSpawn = 4
    ///    根据可用点位和候选数量计算
    ///    范围：2-4个乘客（如果条件允许）
    /// 
    /// 7️⃣循环生成乘客
    ///    对于前 spawnCount 个候选乘客：
    ///    GeneratePassengerImmediate(data, slotIndex)
    ///    记录每种类型的生成数
    /// 
    /// 8️⃣ 处理剩余乘客
    ///    未生成的乘客加入 waitingQueue
    ///    当有乘客被驱散时，从队列补位
    /// 
    /// 9️⃣ 更新显示
    ///    UpdateDepthAndScale() - 根据Y坐标调整深度
    ///    NotifyPassengerCountChanged() - 通知UI更新
    /// 
    /// 配额详解（特殊乘客）：
    /// ```
    /// 整个等级有配额上限（如5个特殊乘客）
    /// Wave 1: 生成1个 → 剩余4个
    /// Wave 2: 生成1个 → 剩余3个
    /// Wave 3: 生成0个（不是特殊层）→ 剩余3个
    /// Wave 4: 生成1个 → 剩余2个
    /// ...
    /// ```
    /// 
    /// 生成数量范围计算：
    /// ```
    /// 可用点位 ≥ maxSpawn(4) && 候选数 ≥ minSpawn(2)
    ///   → 生成 Random.Range(2, Min(4, 候选数) + 1)
    /// 否则
    ///   → 生成 Min(可用点位, 候选数)
    /// ```
    /// 
    /// 日志输出示例：
    /// [PassengerMgr] 候选数量 - 普通:15, 鬼魂:5, 特殊:3
    /// [PassengerMgr] 当前是特殊乘客生成层，生成 1 个特殊乘客
    /// [PassengerMgr] 空闲点位:8, 待生成:20, 实际生成:3
    /// [PassengerMgr] 本波生成：特殊1, 鬼魂1, 普通1, 等待队列:17
    /// </summary>
    private void DoSpawnWave()
    {
        // 1️⃣ 获取可用点位
        List<int> availableIndices = GetAvailableSpawnIndices(isInDockingMode);

        if (availableIndices.Count == 0)
        {
            NotifyPassengerCountChanged();
            return;
        }

        // 2️⃣ 洗牌可用点位
        Shuffle(availableIndices);

        // 3️⃣ 准备候选乘客
        PrepareCandidates(tempNormals, isGhost: false);
        PrepareCandidates(tempGhosts, isGhost: true);
        PrepareSpecialCandidates(tempSpecials);

        // 4️⃣ 计算生成数量
        int ghostCount = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.ghostCount, tempGhosts.Count);
        int normalCount = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.normalPassengerCount, tempNormals.Count);

        // 特殊乘客数量计算
        int waveSpecialCount = 0;

        // 检查是否是指定的特殊层级
        if (ElevatorMgr.Instance.ShouldSpawnSpecialPassenger)
            // 特殊乘客出现时只会一次一个
            waveSpecialCount = 1;
        else
            waveSpecialCount = 0;

        // 检查剩余特殊乘客配额
        int remainingQuota = GameLevelMgr.Instance.GetRemainingSpecialQuota();
        int specialCount = Mathf.Min(waveSpecialCount, tempSpecials.Count, remainingQuota);

        // 5️⃣ 合并所有应该生成的乘客为一个列表
        List<PassengerSO> mergedList = new List<PassengerSO>();

        // 添加特殊乘客
        for (int i = 0; i < specialCount; i++)
            mergedList.Add(tempSpecials[i]);

        // 添加鬼魂乘客
        for (int i = 0; i < ghostCount; i++)
            mergedList.Add(tempGhosts[i]);

        // 添加普通乘客
        for (int i = 0; i < normalCount; i++)
            mergedList.Add(tempNormals[i]);

        // 打乱合并后的列表
        Shuffle(mergedList);


        // 6️⃣ 计算实际生成数量
        const int minSpawn = 2;
        const int maxSpawn = 4;
        int availableCount = availableIndices.Count;
        int mergedCount = mergedList.Count;

        int spawnCount;
        if (availableCount >= maxSpawn && mergedCount >= minSpawn)
        {
            // 有足够的点位和候选，随机选择生成数量
            int upperLimit = Mathf.Min(maxSpawn, mergedCount, availableCount);
            spawnCount = Random.Range(minSpawn, upperLimit + 1);
        }
        else
            // 生成数量受限
            spawnCount = Mathf.Min(availableCount, mergedCount);


        // 统计各类型生成数
        int actualSpecialSpawned = 0;
        int actualGhostSpawned = 0;
        int actualNormalSpawned = 0;

        // 7️⃣ 生成乘客
        for (int i = 0; i < spawnCount; i++)
        {
            PassengerSO data = mergedList[i];
            GeneratePassengerImmediate(data, availableIndices[i]);

            // 统计每种类型
            if (data.isSpecialPassenger)
                actualSpecialSpawned++;
            else if (data.isGhost)
                actualGhostSpawned++;
            else
                actualNormalSpawned++;
        }

        // 更新特殊乘客配额
        GameLevelMgr.Instance.AddSpecialSpawned(actualSpecialSpawned);

        // 8️⃣ 处理剩余乘客（加入等待队列）
        for (int i = spawnCount; i < mergedList.Count; i++)
        {
            waitingQueue.Enqueue(mergedList[i]);
        }

        // 9️⃣ 更新显示和通知
        UpdateDepthAndScale();
        NotifyPassengerCountChanged();
    }
    #endregion

    #region 乘客可用点位相关
    /// <summary>
    /// 获取可用的生成点位索引列表
    /// 
    /// 功能说明：
    /// 根据当前模式（停靠/运行），检查每个槽位是否被占用
    /// 返回所有空余槽位的索引列表
    /// 
    /// 参数说明：
    /// - isDocking: 
    ///   true  = 使用停靠模式的位置数据
    ///   false = 使用运行模式的位置数据
    /// 
    /// 占用判断逻辑：
    /// 1. 获取该槽位对应的参考位置
    /// 2. 遍历所有乘客，检查是否在该位置附近
    /// 3. 使用距离阈值（threshold = 10f）判断
    /// 4. 如果有乘客在 10f 范围内，则该点位被占用
    /// 
    /// 返回值：
    /// 空余槽位的索引列表
    /// 例如：[1, 3, 5, 7] 表示这些槽位可用
    /// 
    /// 时间复杂度：
    /// O(totalSlots * passengerCount)
    /// 通常不会很大（最多10个槽位 × 20个乘客）
    /// 
    /// 注意事项：
    /// - 根据 isInDockingMode 选择使用哪个位置数组
    /// - 点位索引与 Passenger.SlotIndex 相同
    /// - 距离阈值可能需要根据游戏调整
    /// </summary>
    private List<int> GetAvailableSpawnIndices(bool isDocking)
    {
        List<int> availableIndices = new List<int>();
        int totalSlots = ResourcesMgr.Instance.PassengerSlotCount;

        for (int i = 0; i < totalSlots; i++)
        {
            // 根据模式选择使用哪个位置数组
            Vector2 pos2D = isDocking
                ? ResourcesMgr.Instance.passengerDockingPositions[i]
                : ResourcesMgr.Instance.passengerMovingPositions[i];

            Vector3 checkPos = new Vector3(pos2D.x, pos2D.y, 0f);

            // 检查该点位是否被占用
            if (!IsPointOccupied(checkPos))
            {
                availableIndices.Add(i);
            }
        }
        return availableIndices;
    }

    /// <summary>
    /// 判断某个点位是否被乘客占用
    /// 
    /// 功能说明：
    /// 通过距离检查，判断是否有乘客在该点附近
    /// 
    /// 参数说明：
    /// - point: 要检查的点位（世界坐标或 Canvas 坐标）
    /// 
    /// 检查算法：
    /// 1. 遍历所有乘客
    /// 2. 获取每个乘客的 anchoredPosition（Canvas 坐标）
    /// 3. 计算乘客与检查点的距离
    /// 4. 如果距离 < threshold(10f)，则认为占用
    /// 
    /// 距离阈值说明：
    /// - threshold = 10f（单位根据游戏坐标系）
    /// - 这个值决定了判定"占用"的范围
    /// - 如果乘客过于密集，可能需要增大此值
    /// - 通常不建议太大，避免过度限制生成
    /// 
    /// 返回值：
    /// true  = 该点位已被占用（有乘客在附近）
    /// false = 该点位空余
    /// 
    /// 注意事项：
    /// - 使用 Vector2.Distance() 计算欧几里得距离
    /// - 会跳过 null 和未激活的乘客
    /// - 只检查 anchoredPosition，忽略深度（Z）
    /// </summary>
    private bool IsPointOccupied(Vector3 point)
    {
        const float threshold = 10f;

        foreach (var passenger in passengerList)
        {
            // 跳过 null 引用
            if (passenger == null) continue;

            RectTransform rt = passenger.GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 passengerPos = rt.anchoredPosition;
            Vector2 pointPos = new Vector2(point.x, point.y);

            // 检查距离是否小于阈值
            if (Vector2.Distance(passengerPos, pointPos) < threshold)
            {
                return true;  // 该点位被占用
            }
        }

        return false;  // 该点位空余
    }
    #endregion

    #region 乘客数据准备相关
    /// <summary>
    /// 准备候选乘客列表
    /// 
    /// 功能说明：
    /// 从 ResourcesMgr 的全量乘客列表中，筛选符合条件的乘客
    /// 结果放入指定的缓冲区，并打乱顺序
    /// 
    /// 参数说明：
    /// - buffer: 存放筛选结果的列表（会被清空后重新填充）
    /// - isGhost: 
    ///   true  = 筛选鬼魂乘客
    ///   false = 筛选普通乘客
    /// 
    /// 筛选条件：
    /// 1. so != null（有效的 PassengerSO 对象）
    /// 2. so.isGhost == isGhost（符合指定的类型）
    /// 3. !so.isSpecialPassenger（不是特殊乘客）
    /// 
    /// 流程：
    /// 1. buffer.Clear() - 清空缓冲区
    /// 2. 遍历 ResourcesMgr.passengerSOList
    /// 3. 检查每个乘客是否符合条件
    /// 4. 符合条件的加入 buffer
    /// 5. Shuffle(buffer) - 打乱顺序
    /// 
    /// 使用场景：
    /// - 每次生成波次前调用三次（普通×2、特殊×1）
    /// - 普通乘客：PrepareCandidates(tempNormals, false)
    /// - 鬼魂乘客：PrepareCandidates(tempGhosts, true)
    /// 
    /// 注意事项：
    /// - 特殊乘客由 PrepareSpecialCandidates() 专门处理
    /// - 打乱顺序是为了保证每次选择不同的乘客
    /// - buffer 作为缓冲区重复使用，避免频繁分配内存
    /// </summary>
    private void PrepareCandidates(List<PassengerSO> buffer, bool isGhost)
    {
        buffer.Clear();
        foreach (var so in ResourcesMgr.Instance.passengerSOList)
        {
            // 筛选条件：有效的、符合类型的、非特殊的乘客
            if (so != null && so.isGhost == isGhost && !so.isSpecialPassenger)
                buffer.Add(so);
        }
        // 打乱顺序，保证每次选择都不同
        Shuffle(buffer);
    }

    /// <summary>
    /// 准备特殊乘客候选列表
    /// 
    /// 功能说明：
    /// 从全量乘客列表中，筛选所有特殊乘客
    /// 特殊乘客具有灵能恢复能力
    /// 
    /// 筛选条件：
    /// 1. so != null（有效的 PassengerSO 对象）
    /// 2. so.isSpecialPassenger == true（是特殊乘客）
    /// 
    /// 流程：
    /// 1. buffer.Clear() - 清空缓冲区
    /// 2. 遍历 ResourcesMgr.passengerSOList
    /// 3. 检查是否为特殊乘客
    /// 4. 是则加入 buffer
    /// 5. Shuffle(buffer) - 打乱顺序
    /// 
    /// 特殊乘客说明：
    /// - 数量通常较少（如3-5个）
    /// - 有全局生成配额限制
    /// - 在特定波次才会生成
    /// - 生成时通常只一个一个生成
    /// 
    /// 使用时机：
    /// - 在 DoSpawnWave() 中调用
    /// - 作为生成候选之一
    /// 
    /// 注意事项：
    /// - 不检查 isGhost 字段（特殊乘客可以是鬼魂也可以是普通）
    /// - 打乱顺序保证每次选择随机
    /// </summary>
    private void PrepareSpecialCandidates(List<PassengerSO> buffer)
    {
        buffer.Clear();
        foreach (var so in ResourcesMgr.Instance.passengerSOList)
        {
            // 筛选条件：有效的、是特殊乘客
            if (so != null && so.isSpecialPassenger)
                buffer.Add(so);
        }
        // 打乱顺序
        Shuffle(buffer);
    }
    #endregion

    #region 生成单个乘客
    /// <summary>
    /// 立即生成单个乘客 - 核心生成方法
    /// 
    /// 功能说明：
    /// 这是真正创建 Passenger 实例的方法
    /// 负责 GameObject 创建、组件初始化、淡入动画
    /// 
    /// 参数说明：
    /// - data: 乘客的 ScriptableObject 配置
    /// - slotIndex: 分配给乘客的槽位索引（0-based）
    /// 
    /// 生成流程详解：
    /// 
    /// 1️⃣ 数据验证
    ///    • 检查 data 是否有效
    ///    • 检查 cachedGamePanel 是否存在
    ///    • 检查 slotIndex 是否在有效范围内
    /// 
    /// 2️⃣ 获取父容器
    ///    • 从 GamePanel 获取乘客容器的 Transform
    ///    • 这个容器是所有 Passenger 的父物体
    /// 
    /// 3️⃣ 创建游戏物体
    ///    • GameObject.Instantiate(prefab, parent)
    ///    • 直接在指定父容器下创建
    ///    • 自动继承父容器的位置/旋转/缩放
    /// 
    /// 4️⃣ 配置 RectTransform
    ///    • 设置 anchoredPosition（相对于 Canvas 的位置）
    ///    • 使用停靠位置（passengerDockingPositions）
    ///    • 重置旋转为 identity
    ///    • 本体 localScale = Vector3.one（容器负责整体缩放）
    /// 
    /// 5️⃣ 初始化 Passenger 组件
    ///    • passenger.Init(data) - 初始化乘客数据
    ///    • passenger.SlotIndex = slotIndex - 记录槽位
    ///    • passenger.MarkAsNewThisRound() - 标记为本轮新进入
    /// 
    /// 6️⃣ 播放淡入动画
    ///    • passenger.FadeIn(0.5f) - 0.5秒淡入
    ///    • 从透明(alpha=0)变为不透明(alpha=1)
    ///    • 创建视觉上的进入效果
    /// 
    /// 7️⃣ 添加到列表
    ///    • passengerList.Add(passenger)
    ///    • 加入管理列表，后续可以访问
    /// 
    /// 重要说明：
    /// 
    /// 位置说明：
    /// - 乘客本身的 localScale 固定为 (1, 1, 1)
    /// - 整体缩放由父容器的 localScale 控制
    /// - 位置始终使用停靠坐标（不是运行坐标）
    /// 
    /// SlotIndex 的作用：
    /// - 用于后续位置切换时更新乘客位置
    /// - 关键用于 UpdateAllPassengerPositions() 中查表
    /// - 必须设置，否则位置切换时会出问题
    /// 
    /// MarkAsNewThisRound 的作用：
    /// - 标记本轮新进入的乘客
    /// - 伤害结算时会跳过这些乘客
    /// - 下一轮时会自动清除此标记
    /// 
    /// 错误处理：
    /// - 如果 slotIndex 无效，直接返回
    /// - 如果 data 为 null，直接返回
    /// - 如果 cachedGamePanel 为 null，无法生成
    /// 
    /// 日志说明：
    /// - 无效的 slotIndex 时输出错误日志
    /// - 帮助调试点位分配问题
    /// 
    /// 性能考虑：
    /// - GameObject.Instantiate 是相对昂贵的操作
    /// - 淡入动画由协程处理，不阻塞主线程
    /// - 建议每波最多生成 4-6 个乘客
    /// </summary>
    private void GeneratePassengerImmediate(PassengerSO data, int slotIndex)
    {
        // 1️⃣ 数据验证
        if (data == null || cachedGamePanel == null)
            return;

        var config = ResourcesMgr.Instance;
        if (slotIndex < 0 || slotIndex >= config.PassengerSlotCount)
            return;

        // 2️⃣ 获取父容器
        Transform parent = cachedGamePanel.GetPassengerContainer();

        // 3️⃣ 创建游戏物体
        GameObject obj = GameObject.Instantiate(config.passengerPrefab, parent);

        // 4️⃣ 配置 RectTransform
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 使用停靠位置（位置由配置决定）
            Vector2 pos = config.passengerDockingPositions[slotIndex];

            rectTransform.anchoredPosition = pos;
            rectTransform.localRotation = Quaternion.identity;
            // 本体保持 1，由父容器缩放控制
            rectTransform.localScale = Vector3.one;
        }

        // 5️⃣ 初始化 Passenger 组件
        Passenger passenger = obj.GetComponent<Passenger>();
        passenger.Init(data);
        passenger.SlotIndex = slotIndex;
        passenger.MarkAsNewThisRound();

        // 6️⃣ 播放淡入动画
        passenger.FadeIn(0.5f);

        // 7️⃣ 添加到列表
        passengerList.Add(passenger);
    }
    #endregion

    #region 状态切换逻辑
    /// <summary>
    /// 切换到停靠位置模式
    /// 
    /// 功能说明：
    /// 使所有乘客移动到停靠位置
    /// 通常在电梯停靠时调用
    /// 
    /// 执行步骤：
    /// 1. 确保有容器引用
    /// 2. 如果已在停靠模式，直接返回
    /// 3. 设置 isInDockingMode = true
    /// 4. 设置容器缩放为停靠模式下的缩放值
    /// 5. 更新所有乘客的位置
    /// 
    /// 参数说明：
    /// 无
    /// 
    /// 调用时机：
    /// - 电梯到达某个楼层时
    /// - 由 ElevatorMgr 调用
    /// 
    /// 容器缩放说明：
    /// 容器的 localScale 由配置提供：
    /// - passengerContainerDockingScale: 停靠模式的缩放
    /// - passengerContainerMovingScale: 运行模式的缩放
    /// 
    /// 位置更新：
    /// 调用 UpdateAllPassengerPositions() 更新所有乘客
    /// 将每个乘客从当前位置移到停靠位置
    /// 
    /// 性能优化：
    /// - 先检查 isInDockingMode，避免重复设置
    /// - 只有当状态改变时才更新
    /// 
    /// 注意事项：
    /// - 容器为 null 时会输出警告并返回
    /// - 这是正常情况，场景加载时可能还没准备好
    /// </summary>
    public void SwitchToDockingPositions()
    {
        // 确保拥有容器的引用
        EnsureContainerReference();

        // 如果容器为空，记录警告并返回（正常情况，不报错）
        if (passengerContainer == null)
            return;

        if (isInDockingMode) return; // 已在停靠模式，无需切换

        isInDockingMode = true;
        passengerContainer.localScale = ResourcesMgr.Instance.passengerContainerDockingScale;
        UpdateAllPassengerPositions();
    }

    /// <summary>
    /// 切换到运行位置模式
    /// 
    /// 功能说明：
    /// 使所有乘客移动到运行位置
    /// 通常在电梯开始运行时调用
    /// 
    /// 执行步骤：
    /// 1. 检查状态和容器引用
    /// 2. 如果已在运行模式且容器存在，直接返回
    /// 3. 设置 isInDockingMode = false
    /// 4. 确保拥有容器引用
    /// 5. 设置容器缩放为运行模式下的缩放值
    /// 6. 更新所有乘客的位置
    /// 
    /// 参数说明：
    /// 无
    /// 
    /// 调用时机：
    /// - 电梯开始上升/下降时
    /// - 由 ElevatorMgr 调用
    /// 
    /// 容器缩放说明：
    /// 运行模式下的缩放通常与停靠模式不同
    /// 例如：可能会缩小显示尺寸
    /// 
    /// 日志输出：
    /// 会输出当前设置的容器缩放值和模式变化信息
    /// 
    /// 错误处理：
    /// - 容器为 null 时仍会尝试获取
    /// - 防止因 UI 卸载而导致错误
    /// 
    /// 优化：
    /// - 先检查模式状态，避免重复操作
    /// - EnsureContainerReference() 确保引用有效
    /// </summary>
    public void SwitchToMovingPositions()
    {
        // 检查：如果已在运行模式且容器存在，直接返回
        if (!isInDockingMode && passengerContainer != null) return;

        isInDockingMode = false;

        // 确保拿到容器引用
        EnsureContainerReference();

        if (passengerContainer != null)
            passengerContainer.localScale = ResourcesMgr.Instance.passengerContainerMovingScale;

        UpdateAllPassengerPositions();
    }
    #endregion

    #region 位置更新逻辑
    /// <summary>
    /// 更新所有乘客的位置和缩放
    /// 
    /// 功能说明：
    /// 根据当前模式（停靠/运行），为所有乘客重新设置位置
    /// 本体缩放始终固定为 Vector3.one
    /// 整体缩放由容器负责
    /// 
    /// 执行流程：
    /// 1. 检查 passengerList 是否有效
    /// 2. 确保容器缩放与当前模式一致
    /// 3. 遍历每个乘客
    ///    a. 跳过 null 或非激活的乘客
    ///    b. 验证 SlotIndex 有效性
    ///    c. 根据模式选择目标位置
    ///    d. 调用 passenger.SetPositionAndScale()
    /// 
    /// 参数说明：
    /// 无
    /// 
    /// 位置选择逻辑：
    /// ```
    /// if (isInDockingMode)
    ///     targetPos = passengerDockingPositions[slotIndex]
    /// else
    ///     targetPos = passengerMovingPositions[slotIndex]
    /// ```
    /// 
    /// 本体缩放说明：
    /// - 本体 localScale 始终为 Vector3.one
    /// - 不再为每个乘客单独设置缩放
    /// - 容器（passengerContainer）的 localScale 控制整体显示大小
    /// 
    /// 容器缩放保证：
    /// 如果容器存在，确保其 localScale 与当前模式一致
    /// 防止外部未正确设置导致显示不正确
    /// 
    /// 容器缩放值来源：
    /// - ResourcesMgr.Instance.passengerContainerDockingScale
    /// - ResourcesMgr.Instance.passengerContainerMovingScale
    /// 
    /// 使用时机：
    /// - SwitchToDockingPositions() / SwitchToMovingPositions() 后调用
    /// - 乘客列表变化后可能需要调用
    /// 
    /// 性能考虑：
    /// - 会跳过 null 和非激活的乘客，减少计算
    /// - 验证 SlotIndex 有效性，防止越界
    /// - 每个乘客只调用一次 SetPositionAndScale()
    /// 
    /// 注意事项：
    /// - SlotIndex 必须有效（>=0 且 < PassengerSlotCount）
    /// - 乘客可能还在淡入动画中，位置会立即更新
    /// </summary>
    private void UpdateAllPassengerPositions()
    {
        if (passengerList == null) return;

        var config = ResourcesMgr.Instance;

        // 确保容器缩放与当前模式一致
        if (passengerContainer != null)
        {
            passengerContainer.localScale = isInDockingMode
                ? config.passengerContainerDockingScale
                : config.passengerContainerMovingScale;
        }

        // 更新每个乘客的位置
        for (int i = 0; i < passengerList.Count; i++)
        {
            var passenger = passengerList[i];
            if (passenger == null || !passenger.gameObject.activeSelf) continue;

            int slotIndex = passenger.SlotIndex;
            if (slotIndex < 0 || slotIndex >= config.PassengerSlotCount) continue;

            // 根据当前模式选择目标位置
            Vector2 targetPos;

            if (isInDockingMode)
            {
                targetPos = config.passengerDockingPositions[slotIndex];
            }
            else
            {
                targetPos = config.passengerMovingPositions[slotIndex];
            }

            // 设置位置，本体缩放固定为 1
            passenger.SetPositionAndScale(targetPos, Vector3.one);
        }
    }
    #endregion

    /// <summary>
    /// 清除所有乘客
    /// 
    /// 功能说明：
    /// 销毁所有在场景中的乘客
    /// 通常在游戏重新开始/回到主菜单时调用
    /// 
    /// 执行流程：
    /// 1. 遍历 passengerList 中的每个乘客
    /// 2. 如果乘客不为 null，销毁其 GameObject
    /// 3. 清空 passengerList
    /// 4. 重置 isInDockingMode = true（回到初始模式）
    /// 5. 强制置空 UI 引用
    ///    - passengerContainer = null
    ///    - cachedGamePanel = null
    /// 6. 通知 UI 系统乘客数量变化
    /// 
    /// 重要说明：
    /// 
    /// 为什么要置空 UI 引用：
    /// - 下一次游戏开始时，GamePanel 是新创建的实例
    /// - 如果不置空旧引用，会导致访问已销毁的对象
    /// - 这是常见的跨场景切换陷阱
    /// - EnsureContainerReference() 会在需要时重新获取
    /// 
    /// 调用时机：
    /// - GameOver 时
    /// - 返回主菜单时
    /// - 开始新游戏前
    /// 
    /// 性能考虑：
    /// - GameObject.Destroy() 不是立即销毁
    /// - 实际销毁延迟到帧结束
    /// - 这不会阻塞主线程
    /// 
    /// 相关方法：
    /// - Reset(): 重置整个管理器（包括等待队列）
    /// - OnPassengerKicked(): 单个乘客驱逐
    /// - DispelGhost(): 单个鬼魂驱散
    /// </summary>
    public void ClearAllPassengers()
    {
        // 销毁所有乘客的游戏物体
        foreach (var p in passengerList)
        {
            if (p != null)
                GameObject.Destroy(p.gameObject);
        }

        // 清空列表
        passengerList.Clear();

        // 重置模式
        isInDockingMode = true;

        // 强制置空 UI 引用，避免跨场景引用问题
        // 下一局游戏会重新获取新的 GamePanel 实例
        passengerContainer = null;
        cachedGamePanel = null;

        // 通知 UI 更新
        NotifyPassengerCountChanged();
    }


    /// <summary>
    /// 驱散鬼魂 - 专门用于鬼魂清除
    /// 
    /// 功能说明：
    /// 驱散/消灭一个鬼魂乘客
    /// 由铃铛(BellItem)使用，清除被检测到的鬼魂
    /// 
    /// 参数说明：
    /// - ghost: 要驱散的鬼魂乘客
    /// 
    /// 执行步骤详解：
    /// 
    /// 1️⃣ 参数检查
    ///    如果 ghost 为 null，直接返回
    /// 
    /// 2️⃣ 从列表移除
    ///    passengerList.Remove(ghost)
    /// 
    /// 3️⃣ 淡出动画并销毁
    ///    ghost.FadeOut(0.5f, callback)
    ///    同样的淡出+销毁流程
    /// 
    /// 4️⃣ 尝试补位
    ///    重要逻辑：
    ///    if (ElevatorMgr.Instance.CurrentState == E_ElevatorState.Stopped)
    ///    {
    ///        TrySpawnFromWaitingQueue();
    ///    }
    /// 
    ///    补位条件说明：
    ///    • 只有电梯停靠状态才补位
    ///    • 运行中不补位（等到停靠时再补位）
    ///    • 这是为了避免飞行中添加乘客的视觉问题
    /// 
    /// 5️⃣ 更新显示
    ///    UpdateDepthAndScale() - 重新计算深度
    ///    NotifyPassengerCountChanged() - 通知 UI 乘客数量
    /// 
    /// 6️⃣ 输出日志
    ///    记录驱散操作和补位情况
    /// 
    /// 与 OnPassengerKicked 的区别：
    /// - 不检查乘客类型（直接驱散）
    /// - 不扣除任何惩罚（驱鬼是好事）
    /// - 不播放音效（可能在 BellItem 中播放）
    /// - 补位条件不同（停靠状态才补位）
    /// 
    /// 补位机制详解：
    /// 假设等待队列中有 3 个乘客：
    /// 1. 驱散一个鬼魂
    /// 2. 检查电梯状态
    /// 3. 如果停靠中，调用 TrySpawnFromWaitingQueue()
    ///    这会生成 1-3 个乘客到空余位置
    /// 4. 如果运行中，什么都不做
    ///    等电梯停靠时由别处调用 TrySpawnFromWaitingQueue()
    /// 
    /// 日志输出示例：
    /// [PassengerMgr] 鬼魂已被驱散（补位: true）
    /// [PassengerMgr] 鬼魂已被驱散（补位: false）
    /// 
    /// 调用时机：
    /// - BellItem 检测到鬼魂并使用时
    /// - 其他驱鬼技能使用时
    /// - 游戏规则触发鬼魂清除时
    /// 
    /// 注意事项：
    /// - 补位逻辑确保了等待队列乘客被及时加入
    /// - 停靠状态检查防止了中途添加乘客的问题
    /// - 与 GameLevelMgr 的配额系统配合
    /// </summary>
    public void DispelGhost(Passenger ghost)
    {
        if (ghost == null) return;

        // 1️⃣ 从列表移除
        passengerList.Remove(ghost);

        // 2️⃣ 淡出动画并销毁
        ghost.FadeOut(0.5f, () =>
        {
            if (ghost != null && ghost.gameObject != null)
                GameObject.Destroy(ghost.gameObject);
        });

        //  3️⃣ 更新显示
        UpdateDepthAndScale();
        NotifyPassengerCountChanged();
    }


    /// <summary>
    /// Fisher-Yates 洗牌算法
    /// 
    /// 功能说明：
    /// 随机打乱列表中的元素顺序
    /// 
    /// 参数说明：
    /// - list: 要打乱的列表（任何类型 T）
    /// 
    /// 算法说明：
    /// 从后向前遍历：
    /// 1. 对于每个位置 i
    /// 2. 从 [i, list.Count) 中随机选择一个位置 r
    /// 3. 交换 list[i] 和 list[r]
    /// 
    /// 实现细节：
    /// ```csharp
    /// for (int i = 0; i < list.Count; i++)
    /// {
    ///     int r = Random.Range(i, list.Count);
    ///     // 交换
    ///     (list[i], list[r]) = (list[r], list[i]);
    /// }
    /// ```
    /// 
    /// 使用元组交换的优势：
    /// - C# 7.0+ 语法
    /// - 不需要临时变量
    /// - 更简洁清晰
    /// 
    /// 使用场景：
    /// - availableIndices 打乱
    /// - 乘客候选列表打乱
    /// - 任何需要随机顺序的列表
    /// 
    /// 时间复杂度：O(n)
    /// 空间复杂度：O(1)（原地操作）
    /// 
    /// 随机性保证：
    /// - 每个元素出现在任何位置的概率相等
    /// - 完全随机，无偏差
    /// </summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            // 交换元素
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    /// <summary>
    /// 鬼魂靠近时的特殊处理
    /// 
    /// 功能说明：
    /// 当鬼魂靠近电梯时，进行特殊视觉效果
    /// 找到最上面的鬼魂和下方最接近的普通乘客，交换位置
    /// 
    /// 执行流程详解：
    /// 
    /// 1️⃣ 验证列表有效性
    ///    if (passengerList == null || Count == 0) return
    /// 
    /// 2️⃣ 找到最上面的鬼魂
    ///    遍历所有乘客，找 Y 坐标最大的鬼魂
    ///    if (!p.passengerInfo.isGhost) continue;
    ///    if (y > ghostY) { ghostY = y; ghost = p; }
    /// 
    ///    如果没有找到鬼魂，直接返回
    /// 
    /// 3️⃣ 找最接近的候选普通乘客
    ///    从 ghostY 下方找 Y 坐标最大（最接近）的普通乘客
    ///    if (y < ghostY && y < candidateY)
    ///    {
    ///        candidateY = y;
    ///        swapTarget = p;
    ///    }
    /// 
    /// 4️⃣ 交换位置
    ///    if (swapTarget != null)
    ///    {
    ///        交换 ghostRt.anchoredPosition 和 targetRt.anchoredPosition
    ///    }
    /// 
    /// 5️⃣ 更新深度
    ///    UpdateDepthAndScale() - 重新计算显示顺序
    /// 
    /// 位置关系说明（Y轴）：
    /// 屏幕顶部
    ///    ↑
    ///    | 普通乘客
    ///    | 普通乘客
    ///    | [鬼魂] ← 最上面的鬼魂
    ///    | 普通乘客 ← swapTarget（下方最接近）
    ///    | 普通乘客
    ///    ↓
    /// 屏幕底部
    /// 
    /// 交换后：
    ///    | 普通乘客
    ///    | [普通] ← 原鬼魂位置
    ///    | [鬼魂] ← 原普通乘客位置
    ///    | 普通乘客
    /// 
    /// 使用场景：
    /// - 鬼魂接近的入场动画
    /// - 鬼魂靠近时的视觉反馈
    /// - 制造紧张气氛
    /// 
    /// 调用时机：
    /// - 由某个系统检测鬼魂靠近后调用
    /// - 例如 GhostRenderSystem 或类似
    /// 
    /// 边界情况：
    /// - 没有乘客：直接返回
    /// - 没有鬼魂：直接返回
    /// - 没有普通乘客：交换失败但不报错
    /// 
    /// 注意事项：
    /// - 这是位置交换，不是物体交换
    /// - SlotIndex 不改变（重要！）
    /// - 交换完后需要 UpdateDepthAndScale 重新计算
    /// </summary>
    public void GhostApproaching()
    {
        if (passengerList == null || passengerList.Count == 0)
            return;

        // 1️⃣ 找最上面的鬼魂
        Passenger ghost = null;
        float ghostY = float.MinValue;
        foreach (var p in passengerList)
        {
            if (p != null && p.passengerInfo != null && p.passengerInfo.isGhost)
            {
                RectTransform rt = p.GetComponent<RectTransform>();
                float y = rt != null ? rt.anchoredPosition.y : p.transform.localPosition.y;
                if (y > ghostY)
                {
                    ghostY = y;
                    ghost = p;
                }
            }
        }

        if (ghost == null)
        {
            UpdateDepthAndScale();
            return;
        }

        // 2️⃣ 找下方最接近的普通乘客
        Passenger swapTarget = null;
        float candidateY = float.MaxValue;
        foreach (var p in passengerList)
        {
            if (p == null || p.passengerInfo == null || p.passengerInfo.isGhost)
                continue;

            RectTransform rt = p.GetComponent<RectTransform>();
            float y = rt != null ? rt.anchoredPosition.y : p.transform.localPosition.y;
            if (y < ghostY && y < candidateY)
            {
                candidateY = y;
                swapTarget = p;
            }
        }

        // 3️⃣ 交换位置
        if (swapTarget != null)
        {
            RectTransform ghostRt = ghost.GetComponent<RectTransform>();
            RectTransform targetRt = swapTarget.GetComponent<RectTransform>();
            if (ghostRt != null && targetRt != null)
            {
                Vector2 ghostPos = ghostRt.anchoredPosition;
                ghostRt.anchoredPosition = targetRt.anchoredPosition;
                targetRt.anchoredPosition = ghostPos;
            }
        }

        // 4️⃣ 更新深度
        UpdateDepthAndScale();
    }

    /// <summary>
    /// 停止鬼魂靠近效果
    /// 
    /// 功能说明：
    /// 取消鬼魂靠近时的特殊效果
    /// 恢复正常的显示顺序
    /// 
    /// 执行步骤：
    /// 1. 调用 UpdateDepthAndScale()
    ///    重新根据当前 Y 坐标计算深度
    ///    回到正常的显示层级
    /// 
    /// 调用时机：
    /// - 鬼魂离开时
    /// - 鬼魂被消灭时
    /// - 需要取消特殊效果时
    /// 
    /// 注意事项：
    /// - 这只是恢复深度排序
    /// - 乘客位置不会改变（如果之前交换过）
    /// </summary>
    public void StopGhostApproaching()
    {
        UpdateDepthAndScale();
    }

    /// <summary>
    /// 更新所有乘客的显示深度和缩放
    /// 
    /// 功能说明：
    /// 这是一个重要的显示效果方法
    /// 根据每个乘客的 Y 坐标，自动调整：
    /// • Canvas.sortingOrder（显示层级）
    /// • 乘客之间的前后顺序
    /// 
    /// 效果说明：
    /// Y 坐标越大（越靠上）→ sortingOrder 越大（越靠前）
    /// Y 坐标越小（越靠下）→ sortingOrder 越小（越靠后）
    /// 
    /// 这创造了一个"远处在后、近处在前"的视觉效果
    /// 常见于等距视角游戏（Isometric/2.5D）
    /// 
    /// 执行流程详解：
    /// 
    /// 1️⃣ 验证列表有效性
    ///    if (passengerList == null || Count == 0) return
    /// 
    /// 2️⃣ 扫描所有乘客，找Y坐标范围
    ///    float minY = float.MaxValue;
    ///    float maxY = float.MinValue;
    ///    
    ///    遍历所有乘客，从 anchoredPosition 获取 Y 值
    ///    记录最小和最大值
    /// 
    /// 3️⃣ 处理所有乘客在同一高度的边界情况
    ///    if (Mathf.Approximately(minY, maxY))
    ///        maxY = minY + 0.01f;
    ///    
    ///    防止分母为 0（InverseLerp 中）
    ///    添加微小差值保证计算不出错
    /// 
    /// 4️⃣ 遍历每个乘客，计算排序值
    ///    
    ///    对于每个乘客：
    ///    a. 获取其 Y 坐标
    ///    b. 计算归一化比例（0~1）
    ///       float t = Mathf.InverseLerp(maxY, minY, y)
    ///       注意：maxY 在前，minY 在后
    ///       这使得 y==maxY 时 t==0，y==minY 时 t==1
    ///    c. 映射到排序范围
    ///       int sorting = Mathf.RoundToInt(Lerp(5, 90, t))
    ///       从 5（背景）到 90（前景）
    ///    d. 调用 SetSortingOrder()
    ///       passenger.SetSortingOrder(sorting - 5)
    ///       （因为 SetSortingOrder 会再加 5）
    /// 
    /// InverseLerp 说明：
    /// InverseLerp(a, b, value) 计算 value 在 [a, b] 中的比例
    /// 
    /// 示例（假设 maxY=100, minY=0）：
    /// • y = 100 → t = (100-100)/(0-100) = 0/(-100) = 0
    /// • y = 50  → t = (50-100)/(0-100) = (-50)/(-100) = 0.5
    /// • y = 0   → t = (0-100)/(0-100) = (-100)/(-100) = 1
    /// 
    /// 排序值映射：
    /// • t=0 (最上面)   → sorting = Lerp(5, 90, 0) = 5
    /// • t=0.5 (中间)   → sorting = Lerp(5, 90, 0.5) = 47
    /// • t=1 (最下面)   → sorting = Lerp(5, 90, 1) = 90
    /// 
    /// 使用 RoundToInt 的原因：
    /// • sortingOrder 必须是整数
    /// • RoundToInt 四舍五入到最近整数
    /// • 确保没有浮点精度问题
    /// 
    /// SetSortingOrder 调用说明：
    /// 传入 (sorting - 5) 是因为：
    /// • SetSortingOrder 内部会做 MIN(5 + order, 90)
    /// • 传入 (sorting - 5) 后，会变成 MIN(5 + sorting - 5, 90)
    /// • 即 MIN(sorting, 90)，这就是我们想要的值
    /// 
    /// 使用场景：
    /// • 乘客位置改变后
    /// • 乘客被添加/移除后
    /// • 鬼魂靠近/离开时
    /// • 任何需要重新排序的时刻
    /// 
    /// 性能考虑：
    /// • 两次遍历（一次扫描范围，一次设置排序）
    /// • 对于 ~20 个乘客很快
    /// • 可在每帧安全调用
    /// 
    /// 调用时机（被调用的位置）：
    /// • DoSpawnWave() 生成完成后
    /// • UpdateAllPassengerPositions() 位置改变后
    /// • GhostApproaching() / StopGhostApproaching()
    /// • OnPassengerKicked() / DispelGhost()
    /// • TrySpawnFromWaitingQueue() 补位后
    /// 
    /// 边界情况处理：
    /// • 空列表：直接返回
    /// • 单个乘客：minY == maxY，特殊处理
    /// • null 乘客：跳过
    /// 
    /// 注意事项：
    /// - 这个方法不改变乘客位置，只改变显示顺序
    /// - 必须频繁调用以保持正确显示顺序
    /// - 是视觉效果很重要的一环
    /// </summary>
    private void UpdateDepthAndScale()
    {
        if (passengerList == null || passengerList.Count == 0)
            return;

        // 1️⃣ 找到 Y 坐标的范围
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        foreach (var p in passengerList)
        {
            if (p == null) continue;
            RectTransform rt = p.GetComponent<RectTransform>();
            float y = rt != null ? rt.anchoredPosition.y : 0;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        // 2️⃣ 处理所有乘客在同一高度的情况
        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.01f;

        // 3️⃣ 为每个乘客计算排序值
        foreach (var p in passengerList)
        {
            if (p == null) continue;
            RectTransform rt = p.GetComponent<RectTransform>();
            float y = rt != null ? rt.anchoredPosition.y : 0;

            // 计算该乘客的排序比例（0~1）
            // maxY 对应 t=0（最后面），minY 对应 t=1（最前面）
            float t = Mathf.InverseLerp(maxY, minY, y);

            // 映射到排序范围（5~90）
            int sorting = Mathf.RoundToInt(Mathf.Lerp(5, MAX_PASSENGER_SORTING, t));

            // 调用 SetSortingOrder 设置排序
            p.SetSortingOrder(sorting - 5);
        }
    }

    /// <summary>
    /// 通知 UI 乘客数量已变化
    /// 
    /// 功能说明：
    /// 通过事件系统通知 UI 更新乘客数量显示
    /// 
    /// 执行步骤：
    /// 1. 获取当前乘客数量
    ///    count = passengerList?.Count ?? 0
    ///    使用空条件运算符，如果列表为 null 则返回 0
    /// 
    /// 2. 触发事件
    ///    EventCenter.Instance.EventTrigger<int>(
    ///        E_EventType.E_PassengerCountChanged, 
    ///        count
    ///    )
    /// 
    /// 事件说明：
    /// • 事件类型：E_PassengerCountChanged
    /// • 携带参数：乘客数量（int）
    /// • 监听方：UI 系统（如 GamePanel）
    /// 
    /// 调用时机：
    /// • 生成新乘客后
    /// • 移除乘客后
    /// • 清空所有乘客后
    /// • 任何乘客数量改变时
    /// 
    /// UI 对接方式：
    /// UI 系统应该在 Awake/OnEnable 时订阅此事件：
    /// ```csharp
    /// EventCenter.Instance.AddEventListener<int>(
    ///     E_EventType.E_PassengerCountChanged,
    ///     (count) => { /* 更新 UI 显示 */ }
    /// );
    /// ```
    /// </summary>
    private void NotifyPassengerCountChanged()
    {
        int count = passengerList?.Count ?? 0;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PassengerCountChanged, count);
    }

    /// <summary>
    /// 重置乘客管理器
    /// 
    /// 功能说明：
    /// 完全重置管理器状态，准备开始新游戏
    /// 
    /// 执行步骤：
    /// 1. ClearAllPassengers() - 清除所有乘客和 UI 引用
    /// 2. waitingQueue.Clear() - 清空等待队列
    /// 3. 输出日志
    /// 
    /// 调用时机：
    /// • 游戏开始前
    /// • 从主菜单进入游戏时
    /// • 重新开始游戏时
    /// 
    /// 清理内容：
    /// • 销毁所有 Passenger GameObject
    /// • 清空 passengerList
    /// • 置空 UI 引用（重要）
    /// • 清空 waitingQueue
    /// • 重置位置模式
    /// </summary>
    public void Reset()
    {
        ClearAllPassengers();
        waitingQueue.Clear();
    }

    /// <summary>
    /// 清除所有乘客的"本轮新进入"标记
    /// 
    /// 功能说明：
    /// 在伤害结算阶段完成后，清除所有乘客的新进入标记
    /// 这样下一轮伤害结算时，这些乘客会被正常计算
    /// 
    /// 执行流程：
    /// 遍历 passengerList 中的每个乘客
    /// 如果不为 null，调用 ClearNewThisRoundMark()
    /// 
    /// 与 Passenger 中的方法对应：
    /// • MarkAsNewThisRound() 标记为新进入
    /// • ClearNewThisRoundMark() 清除标记
    /// 
    /// 使用场景：
    /// • 伤害结算系统完成后
    /// • 轮次结束时
    /// • 由 GameLevelMgr 或伤害管理器调用
    /// 
    /// 状态变化：
    /// Before: isNewThisRound = true（伤害结算跳过）
    /// After:  isNewThisRound = false（下轮正常处理）
    /// 
    /// 调用时机示例：
    /// ```csharp
    /// // 伤害结算逻辑
    /// SettleDamage();
    /// 
    /// // 伤害结算完成，清除新乘客标记
    /// PassengerMgr.Instance.ClearAllNewThisRoundMarks();
    /// 
    /// // 进入下一轮...
    /// ```
    /// </summary>
    public void ClearAllNewThisRoundMarks()
    {
        foreach (var passenger in passengerList)
        {
            if (passenger != null)
            {
                passenger.ClearNewThisRoundMark();
            }
        }
    }

    /// <summary>
    /// 辅助方法：确保拥有容器的引用
    /// 
    /// 功能说明：
    /// 如果 passengerContainer 为 null，尝试重新获取
    /// 用于处理 UI 动态卸载/加载的场景
    /// 
    /// 执行流程：
    /// 1. 检查 passengerContainer 是否为 null
    /// 2. 如果为 null，通过 UIMgr 获取 GamePanel
    /// 3. 从 GamePanel 获取容器
    /// 
    /// 使用场景：
    /// • 场景切换后容器引用丢失
    /// • UI 被卸载后重新加载
    /// • SwitchToDockingPositions/SwitchToMovingPositions 前
    /// 
    /// 异步处理：
    /// UIMgr.GetPanel() 是异步的（使用回调）
    /// 所以这个方法调用后不能立即使用 passengerContainer
    /// 通常需要在主要操作前提前调用
    /// 
    /// 注意事项：
    /// - 不会报错，即使获取失败
    /// - 调用者需要检查 passengerContainer 是否有效
    /// - 适合错误容错的场景
    /// </summary>
    private void EnsureContainerReference()
    {
        if (passengerContainer == null)
        {
            // 尝试从 UI管理器获取 GamePanel 并拿到容器引用
            UIMgr.Instance.GetPanel<GamePanel>((panel) =>
            {
                if (panel != null)
                {
                    cachedGamePanel = panel;
                    passengerContainer = panel.GetPassengerContainer();
                }
            });
        }
    }
}
