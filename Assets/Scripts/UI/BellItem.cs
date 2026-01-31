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
    }

    private void UpdatePassengerSelection(Vector2 screenPosition)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        var passengerList = PassengerMgr.Instance.passengerList;
        if (passengerList == null)
            return;

        Passenger nearestPassenger = null;
        float minDistance = float.MaxValue;

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

        if (nearestPassenger != currentSelectedPassenger)
        {
            if (currentSelectedPassenger != null)
                currentSelectedPassenger.SetHighlight(false);

            currentSelectedPassenger = nearestPassenger;
            if (currentSelectedPassenger != null)
            {
                currentSelectedPassenger.SetHighlight(true);
                Debug.Log($"[BellItem] 选中乘客: {currentSelectedPassenger.name}");
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
        // 使用 GameDataMgr 的方法消耗灵能值（会自动触发UI更新事件）
        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            Debug.Log("[BellItem] 灵能值不足");
            return;
        }

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

    private void OnGhostDispelled(Passenger ghost)
    {
        MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
        PassengerMgr.Instance.passengerList.Remove(ghost);

        if (ghost != null && ghost.gameObject != null)
            GameObject.Destroy(ghost.gameObject);

        Debug.Log("[BellItem] 鬼魂已消散");
    }
}