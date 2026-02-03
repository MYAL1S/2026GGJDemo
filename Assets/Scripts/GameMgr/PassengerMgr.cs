using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 乘客管理器
/// </summary>
public class PassengerMgr : BaseSingleton<PassengerMgr>
{
    private const float ScaleNear = 1.25f;
    private const float ScaleFar  = 0.85f;
    private const int   SortingMultiplier = 100;
    private const int   BaseSortingOrder  = 10;
    private const int MAX_PASSENGER_SORTING = 90;

    public List<Passenger> passengerList;
    private readonly List<PassengerSO> tempNormals  = new List<PassengerSO>();
    private readonly List<PassengerSO> tempGhosts   = new List<PassengerSO>();
    private readonly List<PassengerSO> tempSpecials = new List<PassengerSO>();
    private readonly Queue<PassengerSO> waitingQueue = new Queue<PassengerSO>();

    private GamePanel cachedGamePanel;
    private Transform passengerContainer; // 缓存父容器引用

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
    }

    private bool isInDockingMode = true;  // ⭐ 是否处于停靠模式

    /// <summary>
    /// 生成一波乘客（不清除已有乘客）
    /// </summary>
    public void SpawnWave()
    {
        // 不再清除现有乘客
        waitingQueue.Clear();

        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            cachedGamePanel = panel;
            if (cachedGamePanel != null)
                passengerContainer = cachedGamePanel.GetPassengerContainer(); // 缓存容器引用

            DoSpawnWave();
        });
    }

    /// <summary>
    /// 实际生成乘客逻辑
    /// </summary>
    private void DoSpawnWave()
    {
        // 只用当前模式的点位
        List<int> availableIndices = GetAvailableSpawnIndices(isInDockingMode);

        if (availableIndices.Count == 0)
        {
            Debug.Log("[PassengerMgr] 没有空闲点位");
            NotifyPassengerCountChanged();
            return;
        }

        Shuffle(availableIndices); // 洗牌索引

        // 准备候选乘客
        PrepareCandidates(tempNormals, isGhost: false);
        PrepareCandidates(tempGhosts, isGhost: true);
        PrepareSpecialCandidates(tempSpecials);

        Debug.Log($"[PassengerMgr] 候选数量 - 普通:{tempNormals.Count}, 鬼魂:{tempGhosts.Count}, 特殊:{tempSpecials.Count}");

        // 获取当前层配置数量
        int ghostCount = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.ghostCount, tempGhosts.Count);
        int normalCount = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.normalPassengerCount, tempNormals.Count);
        
        int waveSpecialCount = GameLevelMgr.Instance.currentLevelDetail.specialPassengerCount;
        int remainingQuota = GameLevelMgr.Instance.GetRemainingSpecialQuota();
        int specialCount = Mathf.Min(waveSpecialCount, tempSpecials.Count, remainingQuota);

        // 合并所有应该生成的乘客为一个列表
        List<PassengerSO> mergedList = new List<PassengerSO>();

        for (int i = 0; i < specialCount; i++)
        {
            mergedList.Add(tempSpecials[i]);
        }

        for (int i = 0; i < ghostCount; i++)
        {
            mergedList.Add(tempGhosts[i]);
        }

        for (int i = 0; i < normalCount; i++)
        {
            mergedList.Add(tempNormals[i]);
        }

        Shuffle(mergedList);

        Debug.Log($"[PassengerMgr] 合并后乘客总数: {mergedList.Count}");

        // 计算实际生成数量
        int minSpawn = 2;
        int maxSpawn = 4;
        int availableCount = availableIndices.Count;
        int mergedCount = mergedList.Count;

        int spawnCount;
        if (availableCount >= maxSpawn && mergedCount >= minSpawn)
        {
            int upperLimit = Mathf.Min(maxSpawn, mergedCount, availableCount);
            spawnCount = Random.Range(minSpawn, upperLimit + 1);
        }
        else
        {
            spawnCount = Mathf.Min(availableCount, mergedCount);
        }

        Debug.Log($"[PassengerMgr] 空闲点位:{availableCount}, 待生成:{mergedCount}, 实际生成:{spawnCount}");

        int actualSpecialSpawned = 0;
        int actualGhostSpawned = 0;
        int actualNormalSpawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            PassengerSO data = mergedList[i];
            GeneratePassengerImmediate(data, availableIndices[i]);

            if (data.isSpecialPassenger)
                actualSpecialSpawned++;
            else if (data.isGhost)
                actualGhostSpawned++;
            else
                actualNormalSpawned++;
        }

        GameLevelMgr.Instance.AddSpecialSpawned(actualSpecialSpawned);

        for (int i = spawnCount; i < mergedList.Count; i++)
        {
            waitingQueue.Enqueue(mergedList[i]);
        }

        UpdateDepthAndScale();
        NotifyPassengerCountChanged();

        Debug.Log($"[PassengerMgr] 本波生成：特殊{actualSpecialSpawned}, 鬼魂{actualGhostSpawned}, 普通{actualNormalSpawned}, 等待队列:{waitingQueue.Count}");
    }

    // 替换原有的 GetAvailableSpawnPoints 方法
    private List<int> GetAvailableSpawnIndices(bool isDocking)
    {
        List<int> availableIndices = new List<int>();
        int totalSlots = ResourcesMgr.Instance.PassengerSlotCount;

        for (int i = 0; i < totalSlots; i++)
        {
            // 动态获取当前模式下的坐标来检测占用
            Vector2 pos2D = isDocking
                ? ResourcesMgr.Instance.passengerDockingPositions[i]
                : ResourcesMgr.Instance.passengerMovingPositions[i];

            Vector3 checkPos = new Vector3(pos2D.x, pos2D.y, 0f);

            if (!IsPointOccupied(checkPos))
            {
                availableIndices.Add(i);
            }
        }
        return availableIndices;
    }

    private bool IsPointOccupied(Vector3 point)
    {
        const float threshold = 10f;

        foreach (var passenger in passengerList)
        {
            if (passenger == null) continue;

            RectTransform rt = passenger.GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector2 passengerPos = rt.anchoredPosition;
            Vector2 pointPos = new Vector2(point.x, point.y);

            if (Vector2.Distance(passengerPos, pointPos) < threshold)
            {
                return true;
            }
        }

        return false;
    }

    private void PrepareCandidates(List<PassengerSO> buffer, bool isGhost)
    {
        buffer.Clear();
        foreach (var so in ResourcesMgr.Instance.passengerSOList)
        {
            if (so != null && so.isGhost == isGhost && !so.isSpecialPassenger)
                buffer.Add(so);
        }
        Shuffle(buffer);
    }

    private void PrepareSpecialCandidates(List<PassengerSO> buffer)
    {
        buffer.Clear();
        foreach (var so in ResourcesMgr.Instance.passengerSOList)
        {
            if (so != null && so.isSpecialPassenger)
                buffer.Add(so);
        }
        Shuffle(buffer);
    }

    /// <summary>
    /// 生成乘客（使用 slotIndex 的版本）
    /// 注意：乘客本体 localScale 固定为 Vector3.one，容器整体缩放由 ResourcesMgr 中的两个 Vector3 控制
    /// </summary>
    private void GeneratePassengerImmediate(PassengerSO data, int slotIndex)
    {
        if (data == null || cachedGamePanel == null)
            return;

        var config = ResourcesMgr.Instance;
        if (slotIndex < 0 || slotIndex >= config.PassengerSlotCount)
        {
            Debug.LogError($"[PassengerMgr] 无效的槽位索引: {slotIndex}");
            return;
        }

        Transform parent = cachedGamePanel.GetPassengerContainer();
        GameObject obj = GameObject.Instantiate(config.passengerPrefab, parent);

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 使用停靠位置生成（位置由配置决定）
            Vector2 pos = config.passengerDockingPositions[slotIndex];

            rectTransform.anchoredPosition = pos;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one; // 本体保持 1，由父容器缩放控制
        }

        Passenger passenger = obj.GetComponent<Passenger>();
        passenger.Init(data);
        passenger.SlotIndex = slotIndex;
        passenger.MarkAsNewThisRound();
        
        passengerList.Add(passenger);
    }

    /// <summary>
    /// 生成乘客（使用指定位置的版本）
    /// </summary>
    private void GeneratePassengerImmediate(PassengerSO data, Vector3 position)
    {
        if (data == null || cachedGamePanel == null)
            return;

        Transform parent = cachedGamePanel.GetPassengerContainer();
        GameObject obj = GameObject.Instantiate(ResourcesMgr.Instance.passengerPrefab, parent);

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(position.x, position.y);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        Passenger passenger = obj.GetComponent<Passenger>();
        passenger.Init(data);
        passenger.MarkAsNewThisRound();
        
        passengerList.Add(passenger);
    }

    /// <summary>
    /// 切换到停靠位置（并更新容器缩放）
    /// </summary>
    public void SwitchToDockingPositions()
    {
        // 如果容器为空，尝试获取
        EnsureContainerReference();

        // ⭐【修复】如果获取失败（比如场景切换时 GamePanel 还没准备好），直接返回，不要报错！
        if (passengerContainer == null)
        {
            Debug.LogWarning("[PassengerMgr] 容器未找到，跳过 SwitchToDockingPositions");
            return;
        }

        if (isInDockingMode) return; // 状态没变就不做

        isInDockingMode = true;
        passengerContainer.localScale = ResourcesMgr.Instance.passengerContainerDockingScale;
        UpdateAllPassengerPositions();
    }

    /// <summary>
    /// 切换到运行位置（并更新容器缩放）
    /// </summary>
    public void SwitchToMovingPositions()
    {
        // 1. 同理，防止引用为空时操作失效
        if (!isInDockingMode && passengerContainer != null) return;

        isInDockingMode = false;

        // 2. 确保拿到引用
        EnsureContainerReference();

        if (passengerContainer != null)
        {
            passengerContainer.localScale = ResourcesMgr.Instance.passengerContainerMovingScale;
            Debug.Log($"[PassengerMgr] 设置PassengerContainer为运行缩放: {ResourcesMgr.Instance.passengerContainerMovingScale}");
        }

        UpdateAllPassengerPositions();
        Debug.Log("[PassengerMgr] 切换到运行位置");
    }

    /// <summary>
    /// 更新所有乘客的位置和本体缩放（本体缩放固定为 Vector3.one）
    /// </summary>
    private void UpdateAllPassengerPositions()
    {
        if (passengerList == null) return;
        
        var config = ResourcesMgr.Instance;

        // 如果容器存在，确保容器缩放与当前模式一致（防止外部未设置）
        if (passengerContainer != null)
        {
            passengerContainer.localScale = isInDockingMode
                ? config.passengerContainerDockingScale
                : config.passengerContainerMovingScale;
        }

        for (int i = 0; i < passengerList.Count; i++)
        {
            var passenger = passengerList[i];
            if (passenger == null || !passenger.gameObject.activeSelf) continue;
            
            int slotIndex = passenger.SlotIndex;
            if (slotIndex < 0 || slotIndex >= config.PassengerSlotCount) continue;
            
            Vector2 targetPos;
            
            if (isInDockingMode)
            {
                targetPos = config.passengerDockingPositions[slotIndex];
            }
            else
            {
                targetPos = config.passengerMovingPositions[slotIndex];
            }
            
            passenger.SetPositionAndScale(targetPos, Vector3.one); // 本体缩放为 1
        }
    }

    private void GeneratePassenger(PassengerSO data, Vector3 position)
    {
        if (data == null)
            return;

        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            cachedGamePanel = panel;
            if (cachedGamePanel != null)
                passengerContainer = cachedGamePanel.GetPassengerContainer();

            GeneratePassengerImmediate(data, position);
            UpdateDepthAndScale();
            NotifyPassengerCountChanged();
        });
    }

    /// <summary>
    /// 清除所有乘客（仅在游戏重新开始时调用）
    /// </summary>
    /// <summary>
    /// 清除所有乘客（仅在游戏重新开始时调用）
    /// </summary>
    public void ClearAllPassengers()
    {
        foreach (var p in passengerList)
        {
            if (p != null)
                GameObject.Destroy(p.gameObject);
        }
        passengerList.Clear();
        isInDockingMode = true;

        // ⭐【新增】强制置空 UI 引用，强迫下一局重新获取
        // 这样可以防止持有上一局已销毁的 GamePanel 引用导致报错卡死
        passengerContainer = null;
        cachedGamePanel = null;

        NotifyPassengerCountChanged();
    }

    /// <summary>
    /// 当某乘客被踢出/移除时调用
    /// </summary>
    public void OnPassengerKicked(Passenger passenger)
    {
        if (passenger == null) return;

        // 如果是特殊乘客，停止灵能恢复
        if (passenger.passengerInfo.isSpecialPassenger)
        {
            passenger.StopPsychicRestore();
        }

        // 如果驱逐的是普通乘客（非鬼非特殊），扣除信任度
        if (!passenger.passengerInfo.isGhost && !passenger.passengerInfo.isSpecialPassenger)
            GameDataMgr.Instance.SubTrustValue(1);

        // 如果是鬼魂，播放消散音效
        if (passenger.passengerInfo.isGhost)
            MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);

        passengerList.Remove(passenger);
        GameObject.Destroy(passenger.gameObject);

        //TrySpawnFromWaitingQueue();
        UpdateDepthAndScale();
        NotifyPassengerCountChanged();
    }

    ///// <summary>
    ///// 驱散鬼魂（由铃铛使用，不扣信任度，不重复播放音效）
    ///// </summary>
    //public void DispelGhost(Passenger ghost)
    //{
    //    if (ghost == null) return;

    //    passengerList.Remove(ghost);
    //    GameObject.Destroy(ghost.gameObject);

    //    TrySpawnFromWaitingQueue();
    //    UpdateDepthAndScale();
    //    NotifyPassengerCountChanged();
        
    //    Debug.Log("[PassengerMgr] 鬼魂已被驱散");
    //}

    /// <summary>
    /// 驱散鬼魂（由铃铛使用）
    /// 直接从列表中移除并销毁，不扣信任度
    /// </summary>
    public void DispelGhost(Passenger ghost)
    {
        if (ghost == null) return;

        // 从列表中移除
        passengerList.Remove(ghost);
        
        // 销毁游戏对象
        GameObject.Destroy(ghost.gameObject);

        // ⭐ 只有在电梯停靠状态才尝试补位
        if (ElevatorMgr.Instance.CurrentState == E_ElevatorState.Stopped)
        {
            TrySpawnFromWaitingQueue();
        }
        
        // 更新深度和缩放
        UpdateDepthAndScale();
        
        // 通知乘客数量变化
        NotifyPassengerCountChanged();
        
        Debug.Log($"[PassengerMgr] 鬼魂已被驱散（补位: {ElevatorMgr.Instance.CurrentState == E_ElevatorState.Stopped}）");
    }

    /// <summary>
    /// 尝试从等待队列补位到空余点位
    /// </summary>
    /// <summary>
    /// 尝试从等待队列补位到空余点位
    /// </summary>
    private void TrySpawnFromWaitingQueue()
    {
        if (waitingQueue.Count == 0)
            return;

        // ⭐ 修复：使用新的索引获取方法，而不是旧的 Vector3 方法
        List<int> availableIndices = GetAvailableSpawnIndices(isInDockingMode);

        foreach (var index in availableIndices)
        {
            if (waitingQueue.Count == 0) break;

            var data = waitingQueue.Dequeue();

            // ⭐ 修复：使用传入 index 的重载方法，确保新补位的乘客也有 SlotIndex
            GeneratePassengerImmediate(data, index);
        }

        UpdateDepthAndScale();
        NotifyPassengerCountChanged();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    private void OnPassengerClicked(Passenger passenger)
    {
        if (PassengerPanel.IsShowing)
            return;

        UIMgr.Instance.ShowPanel<PassengerPanel>(E_UILayer.Top, (panel) =>
        {
            panel.SetSelectedPassenger(passenger);
            passenger.SetHighlight(true);
        });
    }

    public void GhostApproaching()
    {
        if (passengerList == null || passengerList.Count == 0)
            return;

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

        UpdateDepthAndScale();
    }

    public void StopGhostApproaching()
    {
        UpdateDepthAndScale();
    }

    private void UpdateDepthAndScale()
    {
        if (passengerList == null || passengerList.Count == 0)
            return;

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

        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.01f;

        foreach (var p in passengerList)
        {
            if (p == null) continue;
            RectTransform rt = p.GetComponent<RectTransform>();
            float y = rt != null ? rt.anchoredPosition.y : 0;
            float t = Mathf.InverseLerp(maxY, minY, y);
            // 不再修改本体 localScale：容器缩放负责显示比例
            int sorting = Mathf.RoundToInt(Mathf.Lerp(5, MAX_PASSENGER_SORTING, t));
            p.SetSortingOrder(sorting - 5);
        }
    }

    private void NotifyPassengerCountChanged()
    {
        int count = passengerList?.Count ?? 0;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PassengerCountChanged, count);
    }

    /// <summary>
    /// 重置乘客管理器（开始新游戏时调用）
    /// </summary>
    public void Reset()
    {
        ClearAllPassengers();
        waitingQueue.Clear();
        Debug.Log("[PassengerMgr] 乘客管理器已重置");
    }

    /// <summary>
    /// ⭐ 清除所有乘客的"本轮新进入"标记（结算后调用）
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
