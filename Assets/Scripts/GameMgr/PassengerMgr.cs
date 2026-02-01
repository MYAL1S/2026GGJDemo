using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 乘客管理器
/// </summary>
public class PassengerMgr : BaseSingleton<PassengerMgr>
{
    // 可配置：近大远小的缩放范围、排序倍增
    private const float ScaleNear = 1.25f;
    private const float ScaleFar  = 0.85f;
    private const int   SortingMultiplier = 100;
    private const int   BaseSortingOrder  = 10; // 基础排序层级
    /// <summary>
    /// 乘客最大排序层级（必须低于道具的 100）
    /// </summary>
    private const int MAX_PASSENGER_SORTING = 90;

    public List<Passenger> passengerList;
    private readonly List<PassengerSO> tempNormals  = new List<PassengerSO>();
    private readonly List<PassengerSO> tempGhosts   = new List<PassengerSO>();
    private readonly List<PassengerSO> tempSpecials = new List<PassengerSO>();
    private readonly Queue<PassengerSO> waitingQueue = new Queue<PassengerSO>();

    // 缓存 GamePanel 引用
    private GamePanel cachedGamePanel;

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
        // 注册乘客点击事件监听
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
        // 注意：UI 系统不需要 OnUpdate 进行射线检测，使用 IPointerClickHandler 接口
    }

    /// <summary>
    /// 生成一波乘客（受点位容量限制，超出进入等待队列）    
    /// </summary>
    public void SpawnWave()
    {
        ClearAllPassengers();
        waitingQueue.Clear();

        // 确保 GamePanel 已加载
        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            cachedGamePanel = panel;
            DoSpawnWave();
        });
    }

    /// <summary>
    /// 实际生成乘客逻辑
    /// </summary>
    private void DoSpawnWave()
    {
        List<Vector3> spawnPoints = new List<Vector3>(GameLevelMgr.Instance.currentLevelDetail.passengerSpawnPositionArray);
        Shuffle(spawnPoints);
        int capacity = spawnPoints.Count;
        int spawnIndex = 0;
        int capacityLeft = capacity;

        // 准备候选
        PrepareCandidates(tempNormals,  isGhost: false);
        PrepareCandidates(tempGhosts,   isGhost: true);
        PrepareSpecialCandidates(tempSpecials);

        int ghostCount   = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.ghostCount,  tempGhosts.Count);
        int normalCount  = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.normalPassengerCount, tempNormals.Count);
        int specialCount = Mathf.Min(GameLevelMgr.Instance.currentLevelDetail.specialPassengerCount, tempSpecials.Count);

        // 先生成特殊乘客，占用容量
        for (int i = 0; i < specialCount; i++)
        {
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempSpecials[i]); continue; }
            GeneratePassengerImmediate(tempSpecials[i], spawnPoints[spawnIndex++]);
            capacityLeft--;
        }

        // 再生成鬼魂
        int gi = 0;
        int ghostsSpawned = 0;
        while (ghostsSpawned < ghostCount)
        {
            if (gi >= tempGhosts.Count) break;
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempGhosts[gi++]); continue; }
            GeneratePassengerImmediate(tempGhosts[gi++], spawnPoints[spawnIndex++]);
            capacityLeft--;
            ghostsSpawned++;
        }

        // 最后生成普通乘客
        int ni = 0;
        int normalsSpawned = 0;
        while (normalsSpawned < normalCount)
        {
            if (ni >= tempNormals.Count) break;
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempNormals[ni++]); continue; }
            GeneratePassengerImmediate(tempNormals[ni++], spawnPoints[spawnIndex++]);
            capacityLeft--;
            normalsSpawned++;
        }

        // 刷新深度和缩放
        UpdateDepthAndScale();

        // 触发乘客数量变化事件
        NotifyPassengerCountChanged();
    }

    /// <summary>
    /// 准备乘客候选列表
    /// </summary>
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

    /// <summary>
    /// 准备特殊乘客候选列表
    /// </summary>
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
    /// 生成乘客（立即生成，用于已获取 GamePanel 后）
    /// </summary>
    private void GeneratePassengerImmediate(PassengerSO data, Vector3 position)
    {
        if (data == null || cachedGamePanel == null)
            return;

        Transform parent = cachedGamePanel.GetPassengerContainer();
        GameObject obj = GameObject.Instantiate(ResourcesMgr.Instance.passengerPrefab, parent);

        // 设置 RectTransform 的锚点位置
        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(position.x, position.y);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        Passenger passenger = obj.GetComponent<Passenger>();
        passenger.Init(data);
        passengerList.Add(passenger);
    }

    /// <summary>
    /// 生成乘客（异步获取 GamePanel）
    /// </summary>
    private void GeneratePassenger(PassengerSO data, Vector3 position)
    {
        if (data == null)
            return;

        UIMgr.Instance.GetPanel<GamePanel>((panel) =>
        {
            cachedGamePanel = panel;
            GeneratePassengerImmediate(data, position);
            UpdateDepthAndScale();
            NotifyPassengerCountChanged();
        });
    }

    /// <summary>
    /// 清除所有乘客
    /// </summary>
    public void ClearAllPassengers()
    {
        foreach (var p in passengerList)
        {
            if (p != null)
                GameObject.Destroy(p.gameObject);
        }
        passengerList.Clear();
        NotifyPassengerCountChanged();
    }

    /// <summary>
    /// 当某乘客被踢出/移除时调用
    /// </summary>
    public void OnPassengerKicked(Passenger passenger)
    {
        if (passenger != null)
            GameObject.Destroy(passenger.gameObject);
        
        passengerList.Remove(passenger);
        
        // 如果驱逐的是普通乘客，扣除信任度
        if (!passenger.passengerInfo.isGhost)
            GameDataMgr.Instance.SubTrustValue(1);
        
        // 如果是鬼魂，播放消散音效
        if (passenger.passengerInfo.isGhost)
            MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
        
        TrySpawnFromWaitingQueue();
        UpdateDepthAndScale();
        NotifyPassengerCountChanged();
    }

    /// <summary>
    /// 尝试从等待队列补位到空余点位
    /// </summary>
    private void TrySpawnFromWaitingQueue()
    {
        if (waitingQueue.Count == 0)
            return;

        var spawnPoints = GameLevelMgr.Instance.currentLevelDetail.passengerSpawnPositionArray;
        foreach (var pos in spawnPoints)
        {
            bool occupied = false;
            for (int i = 0; i < passengerList.Count; i++)
            {
                if (passengerList[i] == null) continue;
                RectTransform rt = passengerList[i].GetComponent<RectTransform>();
                if (rt != null && Vector2.Distance(rt.anchoredPosition, new Vector2(pos.x, pos.y)) < 0.01f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied && waitingQueue.Count > 0)
            {
                var data = waitingQueue.Dequeue();
                GeneratePassengerImmediate(data, pos);
            }
        }
    }

    /// <summary>
    /// 随机打乱列表顺序
    /// </summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    /// <summary>
    /// 当乘客被点击时触发
    /// </summary>
    private void OnPassengerClicked(Passenger passenger)
    {
        // 如果交互面板已经显示，不再处理新的点击
        if (PassengerPanel.IsShowing)
            return;

        // 显示乘客交互面板
        UIMgr.Instance.ShowPanel<PassengerPanel>(E_UILayer.Top, (panel) =>
        {
            panel.SetSelectedPassenger(passenger);
            // 高亮被选中的乘客
            passenger.SetHighlight(true);
        });
    }

    /// <summary>
    /// 鬼魂靠近事件
    /// </summary>
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

    /// <summary>
    /// 停止鬼魂靠近事件
    /// </summary>
    public void StopGhostApproaching()
    {
        UpdateDepthAndScale();
    }

    /// <summary>
    /// 按 Y 轴深度刷新排序与缩放（Y 越低越靠前，越大越远）
    /// </summary>
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
            float scale = Mathf.Lerp(ScaleFar, ScaleNear, t);
            
            // 限制排序层级在安全范围内（5 ~ 90）
            int sorting = Mathf.RoundToInt(Mathf.Lerp(5, MAX_PASSENGER_SORTING, t));

            p.transform.localScale = Vector3.one * scale;
            p.SetSortingOrder(sorting - 5); // SetSortingOrder 会加上 BASE_SORTING_ORDER
        }
    }

    /// <summary>
    /// 触发乘客数量变化事件
    /// </summary>
    private void NotifyPassengerCountChanged()
    {
        int count = passengerList?.Count ?? 0;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PassengerCountChanged, count);
    }
}
