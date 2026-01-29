using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class PassengerMgr : BaseSingleton<PassengerMgr>
{
    public List<Passenger> passengerList;
    private readonly List<PassengerSO> tempNormals = new List<PassengerSO>();
    private readonly List<PassengerSO> tempGhosts  = new List<PassengerSO>();

    private PassengerMgr()
    {
        passengerList = new List<Passenger>();
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
    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
