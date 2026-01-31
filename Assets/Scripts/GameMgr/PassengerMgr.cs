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
    private readonly List<PassengerSO> tempNormals = new List<PassengerSO>();
    private readonly List<PassengerSO> tempGhosts  = new List<PassengerSO>();

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
        //注册乘客点击事件监听
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
        //注册每帧更新监听
        MonoMgr.Instance.AddUpdateListener(OnUpdate);
    }

    /// <summary>
    /// 生成一波乘客
    /// </summary>
    public void SpawnWave()
    {
        ClearAllPassengers();

        // 随机打乱生成点
        List<Vector3> availablePoints = new List<Vector3>(GameLevelMgr.Instance.currentLevelDetail.passengerSpawnPositionArray);
        Shuffle(availablePoints);

        // 准备候选并打乱，保证本波不重复
        PrepareCandidates(tempNormals, isGhost: false);
        PrepareCandidates(tempGhosts,  isGhost: true);

        int ghostCount  = GameLevelMgr.Instance.currentLevelDetail.ghostCount;
        int normalCount = GameLevelMgr.Instance.currentLevelDetail.normalPassengerCount;

        // 防御：可用数量不足时截断
        ghostCount  = Mathf.Min(ghostCount, tempGhosts.Count);
        normalCount = Mathf.Min(normalCount, tempNormals.Count);
        int needed = ghostCount + normalCount;
        if (needed > availablePoints.Count) needed = availablePoints.Count;

        int idx = 0;
        // 先生成鬼魂（可按需求调整顺序）
        for (int i = 0; i < ghostCount && idx < availablePoints.Count; i++, idx++)
            GeneratePassenger(tempGhosts[i], availablePoints[idx]);

        // 再生成普通乘客
        for (int i = 0; i < normalCount && idx < availablePoints.Count; i++, idx++)
            GeneratePassenger(tempNormals[i], availablePoints[idx]);
    }

    /// <summary>
    /// 准备乘客候选列表
    /// </summary>
    /// <param name="buffer">缓冲列表 用于暂存乘客候选</param>
    /// <param name="isGhost">乘客是否为鬼魂</param>
    private void PrepareCandidates(List<PassengerSO> buffer, bool isGhost)
    {
        buffer.Clear();
        foreach (var so in ResourcesMgr.Instance.passengerSOList)
        {
            if (so != null && so.isGhost == isGhost)
                buffer.Add(so);
        }
        Shuffle(buffer); // 打乱保证随机且不重复取
    }

    /// <summary>
    /// 生成乘客 此处直接将乘客实例化到场景中
    /// </summary>
    /// <param name="data">乘客数据</param>
    /// <param name="position">乘客位置</param>
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
    /// 随机打乱列表顺序
    /// </summary>
    /// <typeparam name="T">列表类型</typeparam>
    /// <param name="list">传入的列表</param>
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
        if (passenger == null)
            return;
        //此处应该显示UI等逻辑
        // 仅作示例输出
        string result = passenger.passengerInfo.isGhost ? "Ghost" : "Normal";
        Debug.Log(result);
        Debug.Log($"Passenger clicked: {(passenger.passengerInfo != null ? passenger.passengerInfo.passengerName : "Unknown")}");
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
