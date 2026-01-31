using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

/// <summary>
/// 用于记录游戏中各种资源的管理器
/// </summary>
public class ResourcesMgr : BaseSingleMono<ResourcesMgr>
{
    /// <summary>
    /// 记录关卡详情的ScriptableObject列表
    /// </summary>
    [Header("LevelInfo")]
    [Tooltip("存储1-18层每一层的具体关卡信息")]
    public List<LevelDetailSO> levelDetailSOList = new List<LevelDetailSO>();
    /// <summary>
    /// 乘客预制体
    /// </summary>
    [Header("PassengerInfo")]
    [Tooltip("存储通用的乘客预设体")]
    public GameObject passengerPrefab;
    /// <summary>
    /// 乘客的ScriptableObject列表
    /// </summary>
    [Tooltip("存储所有的乘客的具体信息")]
    public List<PassengerSO> passengerSOList = new List<PassengerSO>();
    /// <summary>
    /// 波数详情的ScriptableObject列表
    /// </summary>
    public List<WaveDetailSO> waveSOList = new List<WaveDetailSO>();
    /// <summary>
    /// 乘客的信任值
    /// </summary>
    public int passengerTrustValue;
    /// <summary>
    /// 稳定度值
    /// </summary>
    public int stabilityValue;
    /// <summary>
    /// 最多特殊乘客数量
    /// </summary>
    public int maxSpecialPassengerCount;
    /// <summary>
    /// 最多镜像出现次数
    /// </summary>
    public int maxMirrorOccourence;

    /// <summary>
    /// 降低乘客信任值
    /// 当调用此方法时 乘客信任值降低10点 不低于0
    /// 踢错乘客时调用此方法
    /// </summary>
    public void SubPassengerTrustValue()
    {
        passengerTrustValue -= 10;
        passengerTrustValue = Mathf.Max(0, passengerTrustValue);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        print(passengerTrustValue);
        //当乘客信任值为0时 游戏失败
        if (passengerTrustValue <= 0)
            EventMgr.Instance.FallIntoAbyss();
    }
}
