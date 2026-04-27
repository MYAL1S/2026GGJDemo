using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 手机工具类 - 继承自 DraggableItem
/// 
/// 功能描述：
/// 1. 管理手机屏幕（作为灵能检测工具）
/// 2. 在拖动时显示鬼魂投影图像
/// 3. 检测手机屏幕覆盖的鬼魂并播放发现音效
/// 4. 消耗玩家灵能值来使用手机
/// 
/// 工作流程：
/// - OnDragStart：检查灵能值，消耗灵能，初始化幽灵图像层
/// - OnDrag：更新幽灵图像位置，检测手机下是否有鬼魂
/// - OnDragEnd：清理幽灵图像，关闭手机屏幕
/// 
/// 留存管理注意事项：
/// - ghostImageCache 存储 Passenger 与 Image 的映射，需确保拖动结束时完全清理
/// - 动态创建的 GameObject（GhostLayer、Ghost 图像）需在 OnDestroy 时销毁
/// - lastDetectedGhost 在拖动结束时重置以避免状态残留
/// </summary>
public class PhoneItem : DraggableItem
{
    // ============ 配置相关字段 ============
    /// <summary>手机配置信息（通过 Inspector 预设或运行时设置）</summary>
    [SerializeField] private ItemConfigSO itemConfig;

    /// <summary>手机屏幕的根对象（UI Canvas 下的子物体，用于显示幽灵投影）</summary>
    [SerializeField] private GameObject phoneScreenObj;

    // ============ 状态相关字段 ============
    /// <summary>使用一次手机消耗的灵能值</summary>
    private int psychicCost = 1;

    /// <summary>标记是否成功开始拖动（用于区分取消拖动和正常拖动）</summary>
    private bool isDragStarted = false;

    // ============ UI 组件缓存 ============
    /// <summary>手机屏幕的 RectTransform（用于位置计算和重叠检测）</summary>
    private RectTransform phoneScreenRect;

    /// <summary>幽灵图像层（作为所有 Ghost 图像的父物体，位于手机屏幕内）</summary>
    private RectTransform ghostLayer;


    // ============ 鬼魂检测与缓存 ============
    /// <summary>
    /// 幽灵图像缓存字典
    /// Key: Passenger 对象（需要注意：若 Passenger 销毁，此引用需要清理）
    /// Value: 投影在手机屏幕上的 Image 组件
    /// 用于快速更新图像位置和销毁资源
    /// </summary>
    private Dictionary<Passenger, Image> ghostImageCache = new Dictionary<Passenger, Image>();

    /// <summary>上一次检测到的鬼魂（用于判断是否是新发现的鬼魂，避免重复播放音效）</summary>
    private Passenger lastDetectedGhost = null;

    /// <summary>鬼魂出现音效的资源路径</summary>
    private const string GhostAppearSoundPath = "Music/26GGJsound/ghost_appear";

    #region 配置管理模块
    /// <summary>
    /// 运行时设置手机屏幕的根对象
    /// 
    /// 调用时机：
    /// - 若通过 Inspector 预设设置，则不需调用
    /// - 若手机屏幕是动态生成或在其他地方创建，可通过此方法动态关联
    /// 
    /// 参数：
    /// - root: 包含 RectTransform 的手机屏幕 GameObject
    /// </summary>
    /// <param name="root">手机屏幕的根对象</param>
    public void SetRenderTextureRoot(GameObject root)
    {
        phoneScreenObj = root;
        if (root != null)
            phoneScreenRect = root.GetComponent<RectTransform>();
    }

    /// <summary>
    /// 运行时设置手机的配置信息
    /// 
    /// 功能：
    /// - 更新手机最大拖动时间（来自 ItemConfigSO.phoneMaxDragTime）
    /// - 更新使用一次手机的灵能消耗（来自 ItemConfigSO.phonePsychicCost）
    /// 
    /// 调用时机：
    /// - 若通过 Inspector 预设设置，则 Awake 会自动调用
    /// - 若需动态变更配置（如难度升级），可手动调用此方法
    /// </summary>
    /// <param name="config">手机配置信息对象</param>
    public void SetItemConfig(ItemConfigSO config)
    {
        itemConfig = config;
        if (config != null)
        {
            maxDragTime = config.phoneMaxDragTime;
            psychicCost = config.phonePsychicCost;
        }
    }

    /// <summary>
    /// 初始化阶段调用（Unity 生命周期）
    /// 
    /// 初始化步骤：
    /// 1. 调用基类 Awake 以初始化拖动基础功能
    /// 2. 从 Inspector 读取配置（若有预设），更新拖动时间和灵能消耗
    /// 3. 查找名为 "PhoneScreen" 的子对象作为手机屏幕根物体
    /// 4. 若找到屏幕对象，调用 SetupPhoneScreen 进行组件初始化和幽灵层创建
    /// 
    /// 依赖：
    /// - parentCanvas：来自基类 DraggableItem（手机所在的父级 Canvas）
    /// - itemConfig：通过 Inspector 预设或 SetItemConfig 设置
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // 初始化配置：从 ItemConfigSO 读取手机的特性参数
        if (itemConfig != null)
        {
            maxDragTime = itemConfig.phoneMaxDragTime;
            psychicCost = itemConfig.phonePsychicCost;
        }

        // 尝试在子对象中找到名为 "PhoneScreen" 的对象作为手机屏幕根对象
        // 这是手机 UI 的显示区域，用于投影幽灵图像
        if (phoneScreenObj == null)
            phoneScreenObj = transform.Find("PhoneScreen")?.gameObject;

        // 若找到了手机屏幕对象，进行进一步初始化
        if (phoneScreenObj != null)
        {
            phoneScreenRect = phoneScreenObj.GetComponent<RectTransform>();
            SetupPhoneScreen();
        }
    }
    #endregion

    #region UI初始化模块
    /// <summary>
    /// 初始化手机屏幕的 UI 组件并创建幽灵图像层
    /// 
    /// 执行操作：
    /// 1. 确保手机屏幕对象有 Image 组件（作为屏幕背景）
    /// 2. 添加 Mask 组件并禁用 Mask 图形显示（使内部内容被遮罩但不显示遮罩本身）
    /// 3. 创建 GhostLayer 子物体用于存放所有幽灵投影图像
    /// 
    /// Mask 的作用：限制幽灵图像只在手机屏幕区域内显示，超出部分被裁剪
    /// 
    /// 依赖：
    /// - phoneScreenObj：手机屏幕的根 GameObject
    /// </summary>
    private void SetupPhoneScreen()
    {
        if (phoneScreenObj == null) return;

        // 确保屏幕对象有 Image 组件用于显示背景
        Image screenImage = phoneScreenObj.GetComponent<Image>();
        if (screenImage == null)
            screenImage = phoneScreenObj.AddComponent<Image>();

        // 添加 Mask 组件用于遮罩内部内容
        // showMaskGraphic = false 表示只做遮罩效果，不显示遮罩图形本身
        Mask mask = phoneScreenObj.GetComponent<Mask>();
        if (mask == null)
            mask = phoneScreenObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // 创建幽灵图像层
        SetupGhostLayer();

        // 默认关闭屏幕，在拖动时才激活
        phoneScreenObj.SetActive(false);
    }

    /// <summary>
    /// 创建或查找幽灵图像层（GhostLayer）
    /// 
    /// 功能：
    /// - 若 GhostLayer 已存在，直接缓存其 RectTransform
    /// - 若不存在，新建 GhostLayer GameObject 并配置为中心锚点、无尺寸的容器
    /// 
    /// GhostLayer 的作用：
    /// - 作为所有幽灵投影图像的父物体
    /// - 所有幽灵图像都会以相对位置放在此层内，便于统一管理
    /// - 配置为中心锚点确保坐标系统一致
    /// 
    /// 留存注意：
    /// - GhostLayer 是动态创建的，需在 OnDestroy 时销毁
    /// </summary>
    private void SetupGhostLayer()
    {
        if (phoneScreenObj == null) return;

        // 尝试找已有的 GhostLayer
        Transform existingLayer = phoneScreenObj.transform.Find("GhostLayer");
        if (existingLayer != null)
        {
            ghostLayer = existingLayer.GetComponent<RectTransform>();
            return;
        }

        // 创建新的 GhostLayer GameObject
        GameObject layerObj = new GameObject("GhostLayer");
        ghostLayer = layerObj.AddComponent<RectTransform>();
        ghostLayer.SetParent(phoneScreenObj.transform, false);

        // 配置中心锚点和位置，使其作为坐标系统的中心
        ghostLayer.anchorMin = new Vector2(0.5f, 0.5f);
        ghostLayer.anchorMax = new Vector2(0.5f, 0.5f);
        ghostLayer.pivot = new Vector2(0.5f, 0.5f);
        ghostLayer.anchoredPosition = Vector2.zero;
        ghostLayer.sizeDelta = Vector2.zero;  // 容器大小为 0，由子物体决定其尺寸
        ghostLayer.localScale = Vector3.one;
    }
    #endregion

    #region 拖动交互模块
    /// <summary>
    /// 拖动开始时的回调函数（被 DraggableItem 基类触发）
    /// 
    /// 执行步骤：
    /// 1. 重置拖动状态标志（isDragStarted = false 防止未授权拖动）
    /// 2. 重置上次检测的鬼魂（避免状态残留）
    /// 3. 检查电梯是否允许使用面具（ElevatorMgr.CanUseMask）
    /// 4. 检查玩家灵能值是否充足并尝试消耗灵能
    /// 5. 若通过检查，标记拖动已开始
    /// 6. 激活手机屏幕 UI 并初始化幽灵图像
    /// 
    /// 失败情况：
    /// - 不允许使用面具工具 → 重置位置并返回
    /// - 灵能值不足 → 重置位置并返回
    /// 
    /// 依赖：
    /// - ElevatorMgr.Instance.CanUseMask：电梯管理器的权限检查
    /// - GameDataMgr.Instance.ConsumePsychicPower()：灵能消耗和检查
    /// - psychicCost：使用手机的灵能消耗值
    /// </summary>
    protected override void OnDragStart()
    {
        // 初始化状态：标记拖动未成功开始
        isDragStarted = false;
        lastDetectedGhost = null;  // 重置鬼魂检测状态

        // 权限检查：是否允许使用工具
        if (!ElevatorMgr.Instance.CanUseMask)
        {
            ResetPosition();  // 恢复原位置
            return;
        }

        // 资源检查：尝试消耗灵能值
        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            ResetPosition();  // 灵能不足，恢复原位置
            return;
        }

        // 成功开始拖动
        isDragStarted = true;

        // 激活手机屏幕 UI
        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(true);

        // 创建并显示幽灵图像
        CreateGhostImages();
        UpdateGhostImagePositions();
    }

    /// <summary>
    /// 拖动过程中的持续回调函数（每帧调用一次）
    /// 
    /// 功能：
    /// 1. 若拖动未成功开始（isDragStarted = false），直接返回
    /// 2. 调用基类的 OnDrag 更新手机位置
    /// 3. 实时更新幽灵图像的位置以跟随手机屏幕
    /// 4. 检测手机屏幕下是否有鬼魂并播放发现音效
    /// 
    /// 依赖：
    /// - isDragStarted：拖动是否已成功授权
    /// - phoneScreenRect：手机屏幕的位置信息
    /// - eventData：来自 UI 系统的指针事件数据（位置、按下状态等）
    /// </summary>
    public override void OnDrag(PointerEventData eventData)
    {
        // 只有成功开始拖动才继续处理
        if (!isDragStarted) return;

        // 调用基类方法更新拖动位置
        base.OnDrag(eventData);

        // 实时更新幽灵图像位置
        UpdateGhostImagePositions();

        // 检测手机屏幕是否覆盖到鬼魂
        CheckGhostUnderPhone();
    }

    /// <summary>
    /// 拖动结束时的回调函数（被 DraggableItem 基类触发）
    /// 
    /// 执行步骤：
    /// 1. 检查拖动是否曾成功开始
    /// 2. 清除所有幽灵投影图像
    /// 3. 重置鬼魂检测状态（lastDetectedGhost）
    /// 4. 关闭手机屏幕 UI
    /// 5. 标记拖动已结束
    /// 
    /// 留存管理关键：
    /// - ClearGhostImages 销毁动态创建的所有 GameObject
    /// - lastDetectedGhost 重置避免状态残留
    /// - ghostImageCache 被清空
    /// 
    /// 依赖：
    /// - isDragStarted：确保只清理成功开始的拖动
    /// </summary>
    protected override void OnDragEnd()
    {
        // 只清理成功开始的拖动
        if (!isDragStarted) return;

        // 清除幽灵投影图像（销毁 GameObject 并清空缓存）
        ClearGhostImages();

        // 重置鬼魂检测状态
        lastDetectedGhost = null;

        // 关闭手机屏幕 UI
        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(false);

        // 标记拖动已结束
        isDragStarted = false;
    }
    #endregion

    #region 鬼魂检测模块
    /// <summary>
    /// 检测手机屏幕下是否有鬼魂
    /// 
    /// 工作流程：
    /// 1. 遍历所有乘客列表
    /// 2. 过滤条件：
    ///    - 乘客对象有效
    ///    - 乘客信息有效
    ///    - 乘客是幽灵（isGhost = true）
    ///    - 乘客处于激活状态
    ///    - 乘客有主显示器（mainRender）
    /// 3. 检测手机屏幕是否与乘客重叠
    /// 4. 若发现新的鬼魂（不同于上次检测的），播放发现音效
    /// 5. 记录本次检测的鬼魂，用于下次对比
    /// 
    /// 留存考虑：
    /// - lastDetectedGhost 只保存引用，不会导致内存泄漏
    /// - 若 Passenger 对象被销毁，lastDetectedGhost 会变成野指针，需要在 OnDragEnd 时重置
    /// 
    /// 性能优化建议：
    /// - 快速移动时可能多次触发音效，考虑添加音效播放间隔限制
    /// </summary>
    private void CheckGhostUnderPhone()
    {
        // 检查手机屏幕是否有效
        if (phoneScreenRect == null) return;

        Passenger detectedGhost = null;

        // 获取所有乘客并检测是否有鬼魂在手机屏幕下
        var passengers = PassengerMgr.Instance.passengerList;
        if (passengers != null)
        {
            foreach (var passenger in passengers)
            {
                // 基本有效性检查
                if (passenger == null || passenger.passengerInfo == null) continue;
                if (!passenger.passengerInfo.isGhost) continue;
                if (!passenger.gameObject.activeSelf) continue;
                if (passenger.mainRender == null) continue;

                // 检测手机屏幕是否与乘客重叠
                if (IsOverlapping(phoneScreenRect, passenger.mainRender.rectTransform))
                {
                    detectedGhost = passenger;
                    break;  // 只需找到第一个就可以停止
                }
            }
        }

        // 检测到新的鬼魂（之前没有检测到或检测到的是不同的鬼魂）
        if (detectedGhost != null && detectedGhost != lastDetectedGhost)
            // 播放鬼魂出现音效
            MusicMgr.Instance.PlaySound(GhostAppearSoundPath, false);

        // 更新上次检测的鬼魂
        lastDetectedGhost = detectedGhost;
    }

    /// <summary>
    /// 检测两个 RectTransform 是否重叠
    /// 
    /// 算法：分离轴定理（Separating Axis Theorem）简化版本
    /// - 获取两个矩形的世界坐标四个角
    /// - 计算两个 AABB（轴对齐包围盒）的范围
    /// - 检查 X 轴和 Y 轴是否都有重叠
    /// 
    /// 参数：
    /// - rect1：第一个 RectTransform（通常是手机屏幕）
    /// - rect2：第二个 RectTransform（通常是乘客图像）
    /// 
    /// 返回值：
    /// - true：两个矩形有重叠
    /// - false：两个矩形不重叠
    /// </summary>
    private bool IsOverlapping(RectTransform rect1, RectTransform rect2)
    {
        // 获取两个矩形的世界坐标四个角
        Vector3[] corners1 = new Vector3[4];
        Vector3[] corners2 = new Vector3[4];
        rect1.GetWorldCorners(corners1);
        rect2.GetWorldCorners(corners2);

        // 计算第一个矩形的 AABB 范围
        float minX1 = Mathf.Min(corners1[0].x, corners1[2].x);
        float maxX1 = Mathf.Max(corners1[0].x, corners1[2].x);
        float minY1 = Mathf.Min(corners1[0].y, corners1[2].y);
        float maxY1 = Mathf.Max(corners1[0].y, corners1[2].y);

        // 计算第二个矩形的 AABB 范围
        float minX2 = Mathf.Min(corners2[0].x, corners2[2].x);
        float maxX2 = Mathf.Max(corners2[0].x, corners2[2].x);
        float minY2 = Mathf.Min(corners2[0].y, corners2[2].y);
        float maxY2 = Mathf.Max(corners2[0].y, corners2[2].y);

        // 检查是否有重叠：任何一个轴无重叠则结果为不重叠
        return !(maxX1 < minX2 || minX1 > maxX2 || maxY1 < minY2 || minY1 > maxY2);
    }
    #endregion

    #region 幽灵图像管理模块
    /// <summary>
    /// 创建所有乘客的幽灵投影图像
    /// 
    /// 执行步骤：
    /// 1. 清空旧的幽灵图像缓存（避免重复创建）
    /// 2. 遍历所有乘客
    /// 3. 为每个乘客创建一个 GameObject 作为幽灵投影图像
    /// 4. 配置图像的外观（使用 ghostSprite 或 normalSprite）
    /// 5. 配置图像的尺寸和位置参数
    /// 6. 缓存 Image 组件用于后续更新
    /// 
    /// 创建的 GameObject 命名规则：Ghost_{乘客名称}
    /// 
    /// 留存管理：
    /// - 所有创建的 GameObject 都存储在 ghostImageCache 中
    /// - 需要在 ClearGhostImages 中销毁这些对象
    /// - 若 Passenger 对象被销毁，对应的缓存项会变成野指针
    /// 
    /// 依赖：
    /// - PassengerMgr.Instance.passengerList：所有乘客列表
    /// - passenger.passengerInfo.isGhost：判断是否为鬼魂
    /// - passenger.passengerInfo.ghostSprite/normalSprite：幽灵图像
    /// - ghostLayer：幽灵投影图像的父物体
    /// </summary>
    private void CreateGhostImages()
    {
        if (ghostLayer == null) return;

        // 清空旧的幽灵图像，避免重复创建
        ClearGhostImages();

        // 获取所有乘客列表
        var passengers = PassengerMgr.Instance.passengerList;
        if (passengers == null) return;

        // 为每个乘客创建幽灵投影图像
        foreach (var passenger in passengers)
        {
            if (passenger == null || passenger.passengerInfo == null) continue;
            if (passenger.mainRender == null) continue;

            // 获取原始乘客图像的尺寸和位置信息
            RectTransform mainRenderRect = passenger.mainRender.rectTransform;

            // 创建新的幽灵投影 GameObject
            GameObject ghostObj = new GameObject($"Ghost_{passenger.passengerInfo.passengerName}");
            RectTransform ghostRect = ghostObj.AddComponent<RectTransform>();
            ghostRect.SetParent(ghostLayer, false);

            // 配置幽灵投影的图像
            Image ghostImage = ghostObj.AddComponent<Image>();
            ghostImage.sprite = passenger.passengerInfo.isGhost
                ? passenger.passengerInfo.ghostSprite  // 鬼魂显示幽灵图像
                : passenger.passengerInfo.normalSprite; // 非鬼魂显示常规图像
            ghostImage.raycastTarget = false;  // 不接收射线检测，避免影响交互

            // 复制原始图像的布局参数
            ghostRect.pivot = mainRenderRect.pivot;
            ghostRect.sizeDelta = mainRenderRect.sizeDelta;
            ghostRect.localScale = Vector3.one;

            // 缓存 Image 组件用于后续更新
            ghostImageCache[passenger] = ghostImage;
        }
    }

    /// <summary>
    /// 实时更新所有幽灵投影图像的位置、尺寸和缩放
    /// 
    /// 更新内容：
    /// 1. 本地位置：将乘客在世界坐标中的位置转换为相对于 ghostLayer 的本地坐标
    /// 2. 尺寸：同步乘客原始图像的尺寸（可能因为拉伸或缩放而变化）
    /// 3. 缩放：考虑 ghostLayer 的缩放值，计算幽灵图像应该的缩放比例
    ///         确保在 ghostLayer 被缩放时，幽灵图像仍能正确显示
    /// 
    /// 工作流程：
    /// - 每帧 OnDrag 时调用此函数
    /// - 使 ghostLayer 内的图像始终对应乘客的当前位置
    /// - 处理缩放差异：ghostLayerWorldScale 可能不是 1.0
    /// 
    /// 留存考虑：
    /// - 若 ghostImageCache 中的 Image 被销毁但未从字典中移除，此处会出错
    /// - 需要确保 ClearGhostImages 完全清理字典
    /// </summary>
    private void UpdateGhostImagePositions()
    {
        if (ghostLayer == null) return;

        foreach (var kvp in ghostImageCache)
        {
            Passenger passenger = kvp.Key;
            Image ghostImage = kvp.Value;

            // 检查乘客和幽灵图像是否仍然有效
            if (passenger == null || passenger.mainRender == null || ghostImage == null) continue;

            RectTransform ghostRect = ghostImage.rectTransform;
            RectTransform mainRenderRect = passenger.mainRender.rectTransform;

            // 获取乘客在世界坐标系中的位置
            Vector3 worldPos = mainRenderRect.position;

            // 转换为相对于 ghostLayer 的本地坐标
            Vector3 localPos = ghostLayer.InverseTransformPoint(worldPos);
            ghostRect.localPosition = localPos;

            // 同步乘客图像的尺寸
            ghostRect.sizeDelta = mainRenderRect.sizeDelta;

            // 计算并同步缩放
            // 需要考虑 ghostLayer 自身的缩放值，以确保最终显示正确
            Vector3 mainWorldScale = mainRenderRect.lossyScale;  // 乘客的世界缩放
            Vector3 ghostLayerWorldScale = ghostLayer.lossyScale;  // ghostLayer 的世界缩放

            // 防止除以 0
            if (ghostLayerWorldScale.x != 0 && ghostLayerWorldScale.y != 0)
            {
                // 计算相对缩放：乘客世界缩放 / ghostLayer 世界缩放
                ghostRect.localScale = new Vector3(
                    mainWorldScale.x / ghostLayerWorldScale.x,
                    mainWorldScale.y / ghostLayerWorldScale.y,
                    1f  // Z 轴保持 1
                );
            }
        }
    }

    /// <summary>
    /// 清除所有幽灵投影图像
    /// 
    /// 功能：
    /// - 遍历 ghostImageCache 字典
    /// - 销毁每个幽灵投影 Image 的 GameObject
    /// - 清空字典
    /// 
    /// 调用时机：
    /// - 拖动结束时（OnDragEnd）
    /// - 重新创建幽灵层时（CreateGhostImages 开始前）
    /// - 脚本销毁时（OnDestroy）
    /// 
    /// 留存管理关键函数：
    /// - 确保动态创建的 GameObject 被完全销毁
    /// - 清空字典以避免持有已销毁对象的引用
    /// </summary>
    private void ClearGhostImages()
    {
        foreach (var kvp in ghostImageCache)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);  // 销毁幽灵投影 GameObject
        }
        ghostImageCache.Clear();  // 清空字典
    }


    /// <summary>
    /// Unity 销毁回调（当 PhoneItem 脚本或所在 GameObject 销毁时调用）
    /// 
    /// 功能：
    /// - 确保销毁时清除所有幽灵投影图像
    /// - 防止内存泄漏和资源残留
    /// 
    /// 留存管理：
    /// - 最后一道防线，确保动态创建的 GameObject 被销毁
    /// - 即使拖动过程中脚本被销毁，也能清理资源
    /// </summary>
    private void OnDestroy()
    {
        ClearGhostImages();  // 销毁所有幽灵投影 GameObject
    }
    #endregion
}