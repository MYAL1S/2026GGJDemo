using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 铃铛物品 - 拖曳到乘客身上判定鬼魂 / 晃动解决异常事件
/// </summary>
public class BellItem : DraggableItem
{
    [SerializeField]
    private ItemConfigSO itemConfig;

    private Camera mainCamera;
    private Passenger currentSelectedPassenger;
    private int currentShakeCount = 0;
    private Vector2 lastPosition;
    private Vector2 lastDirection;
    private float accumulatedDistance = 0f;
    private float lastShakeTime = 0f;
    private bool isInUnnormalMode = false;
    private bool hasCompletedShake = false;

    // 从配置读取的参数
    private int psychicCost = 1;
    private float detectionRadius = 50f;
    private int requiredShakeCount = 5;
    private float shakeThreshold = 30f;
    private float shakeTimeWindow = 0.3f;
    
    // ⭐ 保存正常模式的拖拽时间
    private float normalMaxDragTime = 0f;

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

    private void LoadConfig()
    {
        if (itemConfig != null)
        {
            psychicCost = itemConfig.bellPsychicCost;
            detectionRadius = itemConfig.bellDetectionRadius;
            maxDragTime = itemConfig.bellMaxDragTime;
            normalMaxDragTime = itemConfig.bellMaxDragTime;  // ⭐ 保存正常拖拽时间
            requiredShakeCount = itemConfig.requiredShakeCount;
            shakeThreshold = itemConfig.shakeThreshold;
            shakeTimeWindow = itemConfig.shakeTimeWindow;
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
        {
            // ⭐ 异常模式下无限拖拽时间
            maxDragTime = float.MaxValue;
            Debug.Log("[BellItem] 异常事件模式 - 开始晃动铃铛（无时间限制）");
        }
        else
        {
            // ⭐ 正常模式恢复配置的拖拽时间
            maxDragTime = normalMaxDragTime;
            Debug.Log("[BellItem] 正常模式 - 开始拖曳铃铛");
        }

        lastPosition = rectTransform.anchoredPosition;
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);

        // ⭐ 异常模式下不检查计时器过期
        if (!isInUnnormalMode && isTimerExpired)
            return;

        if (isInUnnormalMode)
        {
            if (!hasCompletedShake)
                DetectShake(eventData);
        }
        else
        {
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

            ClearPassengerSelection();
        }

        currentShakeCount = 0;
        accumulatedDistance = 0f;
        isInUnnormalMode = false;
        hasCompletedShake = false;
        
        // ⭐ 结束后恢复正常拖拽时间
        maxDragTime = normalMaxDragTime;
    }

    private void UpdatePassengerSelection(Vector2 screenPosition)
    {
        var passengerList = PassengerMgr.Instance.passengerList;
        if (passengerList == null)
            return;

        Passenger nearestPassenger = null;
        float minDistance = float.MaxValue;

        // ⭐ 获取铃铛的 RectTransform 用于碰撞检测
        Vector3[] bellCorners = new Vector3[4];
        rectTransform.GetWorldCorners(bellCorners);

        foreach (var passenger in passengerList)
        {
            if (passenger == null || !passenger.gameObject.activeSelf)
                continue;

            // ⭐ 使用乘客的 mainRender（如果有）进行更精确的检测
            RectTransform passengerRect = passenger.mainRender != null 
                ? passenger.mainRender.rectTransform 
                : passenger.GetComponent<RectTransform>();
            
            if (passengerRect == null)
                continue;

            // ⭐ 方法1：检测铃铛中心点是否在乘客区域内
            Vector2 bellCenter = (Vector2)rectTransform.position;
            if (RectTransformUtility.RectangleContainsScreenPoint(passengerRect, bellCenter, parentCanvas.worldCamera))
            {
                // 在区域内，计算到中心的距离作为优先级
                Vector2 passengerCenter = (Vector2)passengerRect.position;
                float distance = Vector2.Distance(bellCenter, passengerCenter);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPassenger = passenger;
                }
                continue;
            }

            // ⭐ 方法2：如果不在区域内，检测距离是否在检测半径内
            Vector2 passengerScreenPos = RectTransformUtility.WorldToScreenPoint(
                parentCanvas.worldCamera, 
                passengerRect.position
            );

            float screenDistance = Vector2.Distance(screenPosition, passengerScreenPos);

            if (screenDistance <= detectionRadius && screenDistance < minDistance)
            {
                minDistance = screenDistance;
                nearestPassenger = passenger;
            }
        }

        if (nearestPassenger != currentSelectedPassenger)
        {
            if (currentSelectedPassenger != null)
                currentSelectedPassenger.SetHighlight(false);

            currentSelectedPassenger = nearestPassenger;
            if (currentSelectedPassenger != null)
            {
                currentSelectedPassenger.SetHighlight(true);
                Debug.Log($"[BellItem] 选中乘客: {currentSelectedPassenger.name}, 距离: {minDistance}");
            }
        }
    }

    private void ClearPassengerSelection()
    {
        if (currentSelectedPassenger != null)
        {
            currentSelectedPassenger.SetHighlight(false);
            currentSelectedPassenger = null;
        }
    }

    protected override void OnTimerExpired()
    {
        // ⭐ 异常模式下不触发计时器过期逻辑
        if (isInUnnormalMode)
            return;
            
        ClearPassengerSelection();
        base.OnTimerExpired();
    }

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

    private void OnBellHitPassenger(Passenger passenger)
    {
        if (passenger.passengerInfo.isGhost)
        {
            //那么直接驱散鬼魂 不扣除灵能值
            Debug.Log("[BellItem] 检测到鬼魂，将其驱散");
            OnGhostDispelled(passenger);
        }
        else
        {
            //如果是普通乘客 不去减少灵能值 而是扣除稳定值
            GameDataMgr.Instance.SubTrustValue(1);
            Debug.Log("[BellItem] 检测到普通乘客，无事发生");
        }
    }

    /// <summary>
    /// 鬼魂被驱散的处理逻辑
    /// </summary>
    /// <param name="ghost">鬼魂乘客对象</param>
    private void OnGhostDispelled(Passenger ghost)
    {
        //播放驱散音效，移除鬼魂对象
        MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
        PassengerMgr.Instance.DispelGhost(ghost);
        Debug.Log("[BellItem] 鬼魂已被驱散");
    }
}