using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于记录游戏中各种资源的管理器
/// </summary>
public class ResourcesMgr : BaseSingleMono<ResourcesMgr>
{
    [Header("LevelInfo")]
    [Tooltip("存储1-18层每一层的具体关卡信息")]
    public List<LevelDetailSO> levelDetailSOList = new List<LevelDetailSO>();

    [Header("PassengerInfo")]
    [Tooltip("存储通用的乘客预制体")]
    public GameObject passengerPrefab;
    [Tooltip("存储所有的乘客的具体信息")]
    public List<PassengerSO> passengerSOList = new List<PassengerSO>();

    public List<WaveDetailSO> waveSOList = new List<WaveDetailSO>();

    /// <summary>
    /// 乘客的信任值
    /// </summary>
    public int passengerTrustValue;

    /// <summary>
    /// 稳定度值
    /// </summary>
    private int _stabilityValue;
    public int stabilityValue
    {
        get => _stabilityValue;
        set
        {
            _stabilityValue = Mathf.Max(0, value);
            // 触发稳定度变化事件
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _stabilityValue);
        }
    }

    /// <summary>
    /// 最大特殊乘客数量
    /// </summary>
    public int maxSpecialPassengerCount;

    /// <summary>
    /// 面具镜可触发次数
    /// </summary>
    public int maxMirrorOccourence;

    /// <summary>
    /// 游戏是否已结束
    /// </summary>
    public bool isGameOver = false;

    /// <summary>
    /// 扣除乘客信任值
    /// </summary>
    public void SubPassengerTrustValue(int value)
    {
        passengerTrustValue -= value;
        passengerTrustValue = Mathf.Max(0, passengerTrustValue);
    }

    /// <summary>
    /// 扣除稳定度
    /// </summary>
    public void SubStabilityValue(int value)
    {
        stabilityValue -= value;
    }

    /// <summary>
    /// 增加稳定度
    /// </summary>
    public void AddStabilityValue(int value)
    {
        stabilityValue += value;
    }

    void Start()
    {
        // 初始化稳定度（触发初始UI更新）
        _stabilityValue = stabilityValue;
    }

    void Update()
    {
        if (isGameOver)
            return;

        isGameOver = passengerTrustValue <= 0;

        if (isGameOver)
            EventMgr.Instance.FallIntoAbyss();
    }
}
