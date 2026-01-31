using System.Collections;
using System.Collections.Generic;
using System.Xml;
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

    public List<Passenger> passengerList;
    private readonly List<PassengerSO> tempNormals  = new List<PassengerSO>();
    private readonly List<PassengerSO> tempGhosts   = new List<PassengerSO>();
    private readonly List<PassengerSO> tempSpecials = new List<PassengerSO>();
    private readonly Queue<PassengerSO> waitingQueue = new Queue<PassengerSO>();

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
        //注册乘客点击事件监听
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
        //注册每帧更新监听
        MonoMgr.Instance.AddUpdateListener(OnUpdate);
    }

    /// <summary>
    /// 生成一波乘客（受点位容量限制，超出进入等待队列）    
    /// </summary>
    public void SpawnWave()
    {
        ClearAllPassengers();
        waitingQueue.Clear();

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
        int specialsSpawned = 0;
        for (int i = 0; i < specialCount; i++)
        {
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempSpecials[i]); continue; }
            GeneratePassenger(tempSpecials[i], spawnPoints[spawnIndex++]);
            capacityLeft--;
            specialsSpawned++;
        }

        // 再生成鬼魂
        int ghostsSpawned = 0;
        int gi = 0;
        while (ghostsSpawned < ghostCount)
        {
            if (gi >= tempGhosts.Count) break;
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempGhosts[gi++]); continue; }
            GeneratePassenger(tempGhosts[gi++], spawnPoints[spawnIndex++]);
            capacityLeft--;
            ghostsSpawned++;
        }

        // 最后生成普通乘客
        int normalsSpawned = 0;
        int ni = 0;
        while (normalsSpawned < normalCount)
        {
            if (ni >= tempNormals.Count) break;
            if (capacityLeft <= 0) { waitingQueue.Enqueue(tempNormals[ni++]); continue; }
            GeneratePassenger(tempNormals[ni++], spawnPoints[spawnIndex++]);
            capacityLeft--;
            normalsSpawned++;
        }

        // 刷新深度和缩放
        UpdateDepthAndScale();
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
    /// 生成乘客 此处直接将乘客实例化到场景中
    /// </summary>
    private void GeneratePassenger(PassengerSO data, Vector3 position)
    {
        if (data == null)
            return;

        GameObject obj = GameObject.Instantiate(ResourcesMgr.Instance.passengerPrefab, position, Quaternion.identity);
        Passenger passenger = obj.GetComponent<Passenger>();
        passenger.Init(data);
        passengerList.Add(passenger);
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
    }

    /// <summary>
    /// 当某乘客被踢出/移除时调用，尝试补位等待队列
    /// 如果驱逐的是普通乘客，则扣除信任值
    /// </summary>
    public void OnPassengerKicked(Passenger passenger)
    {
        if (passenger != null)
            GameObject.Destroy(passenger.gameObject);
        passengerList.Remove(passenger);
        if (!passenger.passengerInfo.isGhost)
            ResourcesMgr.Instance.SubPassengerTrustValue(1);
        if (passenger.passengerInfo.isGhost)
            MusicMgr.Instance.PlaySound("Music/26GGJsound/ghost_disappear", false);
        TrySpawnFromWaitingQueue();
        UpdateDepthAndScale();
    }

    /// <summary>
    /// 尝试从等待队列补位到空余点位
    /// </summary>
    private void TrySpawnFromWaitingQueue()
    {
        if (waitingQueue.Count == 0)
            return;

        // 计算当前已占用点位，寻找空位
        var spawnPoints = GameLevelMgr.Instance.currentLevelDetail.passengerSpawnPositionArray;
        foreach (var pos in spawnPoints)
        {
            bool occupied = false;
            for (int i = 0; i < passengerList.Count; i++)
            {
                if (passengerList[i] == null) continue;
                if (Vector3.Distance(passengerList[i].transform.position, pos) < 0.01f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied && waitingQueue.Count > 0)
            {
                var data = waitingQueue.Dequeue();
                GeneratePassenger(data, pos);
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
    /// <param name="passenger"></param>
    private void OnPassengerClicked(Passenger passenger)
    {

        //TODO: 处理乘客点击逻辑
        //应该显示一个小的二级面板 对乘客进行交互
        //此处仅作示例输出
        EventCenter.Instance.EventTrigger<Passenger>(E_EventType.E_PassengerUIAppear, passenger);
    }

    /// <summary>
    /// 每一帧执行的逻辑 检测是否点击了乘客
    /// </summary>
    private void OnUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MathUtil.RayCast2D<Passenger>(Camera.main.ScreenPointToRay(Input.mousePosition), (passenger) => {
                passenger?.OnMouseDown();
            },1000,1<<LayerMask.NameToLayer("Passenger"));
        }
    }

    /// <summary>
    /// 鬼魂靠近事件
    /// </summary>
    public void GhostApproaching()
    {
        if (passengerList == null || passengerList.Count == 0)
            return;

        // 选出“最远”的鬼魂（Y 最大视为更远）
        Passenger ghost = null;
        float ghostY = float.MinValue;
        foreach (var p in passengerList)
        {
            if (p != null && p.passengerInfo != null && p.passengerInfo.isGhost)
            {
                float y = p.transform.position.y;
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

        // 找一个更靠前（Y 更低）的普通乘客来交换
        Passenger swapTarget = null;
        float candidateY = float.MaxValue;
        foreach (var p in passengerList)
        {
            if (p == null || p.passengerInfo == null || p.passengerInfo.isGhost)
                continue;

            float y = p.transform.position.y;
            if (y < ghostY && y < candidateY)
            {
                candidateY = y;
                swapTarget = p;
            }
        }

        // 交换位置（若存在更前的乘客）
        if (swapTarget != null)
        {
            Vector3 ghostPos = ghost.transform.position;
            ghost.transform.position = swapTarget.transform.position;
            swapTarget.transform.position = ghostPos;
        }

        // 交换后统一刷新 SortingOrder 与 Scale
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
            float y = p.transform.position.y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }
        // 避免除零
        if (Mathf.Approximately(minY, maxY))
            maxY = minY + 0.01f;

        foreach (var p in passengerList)
        {
            if (p == null) continue;
            float y = p.transform.position.y;
            float t = Mathf.InverseLerp(maxY, minY, y); // Y 越低 t 越接近 1
            float scale = Mathf.Lerp(ScaleFar, ScaleNear, t);
            int sorting = Mathf.RoundToInt((maxY - y) * SortingMultiplier);

            p.transform.localScale = Vector3.one * scale;

            if (p.mainRender != null)
                p.mainRender.sortingOrder = sorting;
            if (p.ghostFeatureRenderer != null)
                p.ghostFeatureRenderer.sortingOrder = sorting + 1; // 特征层略前
        }
    }
}
