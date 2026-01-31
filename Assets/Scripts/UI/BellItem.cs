using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 铃铛物品 - 拖曳到乘客身上判定鬼魂 / 晃动解决异常事件
/// </summary>
public class BellItem : DraggableItem
{
    /// <summary>
    /// 物品配置
    /// </summary>
    [SerializeField]
    private ItemConfigSO itemConfig;

    private Camera mainCamera;

    /// <summary>
    /// 当前选中的乘客（高亮中）
    /// </summary>
    private Passenger currentSelectedPassenger;

    /// <summary>
    /// 当前晃动次数
    /// </summary>
    private int currentShakeCount = 0;

    /// <summary>
    /// 上一帧的位置
    /// </summary>
    private Vector2 lastPosition;

    /// <summary>
    /// 上一次的移动方向
    /// </summary>
    private Vector2 lastDirection;

    /// <summary>
    /// 累计移动距离
    /// </summary>
    private float accumulatedDistance = 0f;

    /// <summary>
    /// 上次方向变化的时间
    /// </summary>
    private float lastShakeTime = 0f;

    /// <summary>
    /// 是否处于异常事件解决模式
    /// </summary>
    private bool isInUnnormalMode = false;

    /// <summary>
    /// 是否已完成晃动（异常模式下）
    /// </summary>
    private bool hasCompletedShake = false;

    // 从配置读取的参数
    private int psychicCost;
    private float detectionRadius;
    private int requiredShakeCount;
    private float shakeThreshold;
    private float shakeTimeWindow;

    /// <summary>
    /// 设置物品配置
    /// </summary>
    public void SetItemConfig(ItemConfigSO config)
    {
        itemConfig = config;
        LoadConfig();
    }

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        LoadConfig();
    }

    /// <summary>
    /// 从配置加载参数
    /// </summary>
    private void LoadConfig()
    {
        if (itemConfig != null)
        {
            psychicCost = itemConfig.bellPsychicCost;
            detectionRadius = itemConfig.bellDetectionRadius;
            maxDragTime = itemConfig.bellMaxDragTime;
            requiredShakeCount = itemConfig.requiredShakeCount;
            shakeThreshold = itemConfig.shakeThreshold;
            shakeTimeWindow = itemConfig.shakeTimeWindow;
        }
        else
        {
            // 默认值
            psychicCost = 1;
            detectionRadius = 50f;
            maxDragTime = 5f;
            requiredShakeCount = 5;
            shakeThreshold = 30f;
            shakeTimeWindow = 0.3f;
        }
    }

    protected override void OnDragStart()
    {
        currentShakeCount = 0;
        accumulatedDistance = 0f;
        lastDirection = Vector2.zero;
        lastShakeTime = Time.time;
        hasCompletedShake = false;
        currentSelectedPassenger = null;

        isInUnnormalMode = EventMgr.Instance.IsInUnnormalState;

        if (isInUnnormalMode)
            Debug.Log("[BellItem] 异常事件模式 - 开始晃动铃铛");
        else
            Debug.Log("[BellItem] 正常模式 - 开始拖曳铃铛");

        lastPosition = rectTransform.anchoredPosition;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);

        if (isTimerExpired)
            return;

        if (isInUnnormalMode)
        {
            // 异常事件模式：检测晃动
            if (!hasCompletedShake)
                DetectShake(eventData);
        }
        else
        {
            // 正常模式：检测并高亮乘客（不触发事件）
            UpdatePassengerSelection(eventData.position);
        }
    }

    protected override void OnDragEnd()
    {
        if (isInUnnormalMode)
        {
            Debug.Log($"[BellItem] 结束晃动，晃动次数: {currentShakeCount}/{requiredShakeCount}");
        }
        else
        {
            Debug.Log("[BellItem] 结束拖曳铃铛");

            // 松手时触发事件
            if (currentSelectedPassenger != null && !isTimerExpired)
            {
                OnBellHitPassenger(currentSelectedPassenger);
            }

            // 取消高亮
            ClearPassengerSelection();
        }

        // 重置状态
        currentShakeCount = 0;
        accumulatedDistance = 0f;
        isInUnnormalMode = false;
        hasCompletedShake = false;
    }

    /// <summary>
    /// 更新乘客选中状态（高亮）
    /// </summary>
    private void UpdatePassengerSelection(Vector2 screenPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        var passengerList = PassengerMgr.Instance.passengerList;
        if (passengerList == null)
            return;

        Passenger nearestPassenger = null;
        float minDistance = float.MaxValue;

        // 找到最近的乘客
        foreach (var passenger in passengerList)
        {
            if (passenger == null)
                continue;

            Vector3 passengerScreenPos = mainCamera.WorldToScreenPoint(passenger.transform.position);
            float screenDistance = Vector2.Distance(screenPosition, passengerScreenPos);

            if (screenDistance <= detectionRadius && screenDistance < minDistance)
            {
                minDistance = screenDistance;
                nearestPassenger = passenger;
            }
        }

        // 如果选中的乘客发生变化
        if (nearestPassenger != currentSelectedPassenger)
        {
            // 取消之前乘客的高亮
            if (currentSelectedPassenger != null)
            {
                currentSelectedPassenger.SetHighlight(false);
            }

            // 高亮新选中的乘客
            currentSelectedPassenger = nearestPassenger;
            if (currentSelectedPassenger != null)
            {
                currentSelectedPassenger.SetHighlight(true);
                Debug.Log($"[BellItem] 选中乘客: {currentSelectedPassenger.name}");
            }
        }
    }

    /// <summary>
    /// 清除乘客选中状态
    /// </summary>
    private void ClearPassengerSelection()
    {
        if (currentSelectedPassenger != null)
        {
            currentSelectedPassenger.SetHighlight(false);
            currentSelectedPassenger = null;
        }
    }

    /// <summary>
    /// 计时结束时的处理
    /// </summary>
    protected override void OnTimerExpired()
    {
        // 取消高亮
        ClearPassengerSelection();
        base.OnTimerExpired();
    }

    /// <summary>
    /// 检测晃动
    /// </summary>
    private void DetectShake(PointerEventData eventData)
    {
        Vector2 currentPosition = rectTransform.anchoredPosition;
        Vector2 delta = currentPosition - lastPosition;

        accumulatedDistance += delta.magnitude;

        if (accumulatedDistance >= shakeThreshold)
        {
            Vector2 currentDirection = delta.normalized;

            if (lastDirection != Vector2.zero)
            {
                float dot = Vector2.Dot(currentDirection, lastDirection);

                if (dot < -0.5f)
                {
                    float timeSinceLastShake = Time.time - lastShakeTime;

                    if (timeSinceLastShake <= shakeTimeWindow)
                    {
                        currentShakeCount++;
                        Debug.Log($"[BellItem] 晃动检测: {currentShakeCount}/{requiredShakeCount}");

                        if (currentShakeCount >= requiredShakeCount)
                            OnShakeComplete();
                    }

                    lastShakeTime = Time.time;
                }
            }

            lastDirection = currentDirection;
            accumulatedDistance = 0f;
        }

        lastPosition = currentPosition;
    }

    /// <summary>
    /// 晃动完成，解决异常事件
    /// </summary>
    private void OnShakeComplete()
    {
        if (hasCompletedShake)
            return;

        hasCompletedShake = true;
        Debug.Log("[BellItem] 晃动完成，解决异常事件");

        MusicMgr.Instance.PlaySound("Music/26GGJsound/bell_ring", false);
        EventMgr.Instance.ResolveUnnormalByBell();
        ResetPosition();
    }

    /// <summary>
    /// 铃铛命中乘客（松手时触发）
    /// </summary>
    private void OnBellHitPassenger(Passenger passenger)
    {
        // 检查并消耗灵能值
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null || playerInfo.nowPsychicPowerValue < psychicCost)
        {
            Debug.Log("[BellItem] 灵能值不足");
            return;
        }

        // 消耗灵能值
        playerInfo.nowPsychicPowerValue -= psychicCost;
        Debug.Log($"[BellItem] 消耗灵能 {psychicCost}，剩余 {playerInfo.nowPsychicPowerValue}");

        // 判断乘客类型
        if (passenger.passengerInfo.isGhost)
        {
            Debug.Log("[BellItem] 检测到鬼魂，触发消散");
            OnGhostDispelled(passenger);
        }
        else
        {
            Debug.Log("[BellItem] 检测到普通乘客，不做处理");
        }
    }

    /// <summary>
    /// 鬼魂消散处理
    /// </summary>
    private void OnGhostDispelled(Passenger ghost)
    {
        MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
        PassengerMgr.Instance.passengerList.Remove(ghost);

        if (ghost != null && ghost.gameObject != null)
            GameObject.Destroy(ghost.gameObject);

        Debug.Log("[BellItem] 鬼魂已消散");
    }
}