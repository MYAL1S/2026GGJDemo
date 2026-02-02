using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于记录游戏中各类资源的管理器
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

    [Header("全局点位配置")]
    [Tooltip("乘客生成点位（全局通用）")]
    public List<Vector3> globalPassengerSpawnPoints = new List<Vector3>();

    [Header("电梯时间配置")]
    [Tooltip("电梯运行最短时间（秒）")]
    public int elevatorMovingTimeMin = 5;
    
    [Tooltip("电梯运行最长时间（秒）")]
    public int elevatorMovingTimeMax = 10;
    
    [Tooltip("电梯停靠时间（秒）")]
    public int elevatorDockingTime = 15;
    
    [Tooltip("电梯到达动画时间（秒）")]
    public int elevatorArrivingTime = 3;
    
    [Tooltip("电梯离开动画时间（秒）")]
    public int elevatorDepartingTime = 1;

    [Header("游戏配额")]
    /// <summary>
    /// 特殊乘客最大数量
    /// </summary>
    public int maxSpecialPassengerCount;

    /// <summary>
    /// 铜镜最大触发次数
    /// </summary>
    public int maxMirrorOccourence;

    [Header("异常事件配置")]
    [Tooltip("异常事件触发概率 (0-1)")]
    [Range(0f, 1f)]
    public float abnormalEventChance = 0.2f;
    
    [Tooltip("异常事件超时时间（秒）")]
    public int abnormalEventTimeout = 10;

    /// <summary>
    /// 乘客的信任值
    /// </summary>
    public int passengerTrustValue;

    /// <summary>
    /// 稳定性值
    /// </summary>
    private int _stabilityValue;
    public int stabilityValue
    {
        get => _stabilityValue;
        set
        {
            _stabilityValue = Mathf.Max(0, value);
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _stabilityValue);
        }
    }

    /// <summary>
    /// 游戏是否已结束
    /// </summary>
    public bool isGameOver = false;

    public void SubPassengerTrustValue(int value)
    {
        passengerTrustValue -= value;
        passengerTrustValue = Mathf.Max(0, passengerTrustValue);
    }

    public void SubStabilityValue(int value)
    {
        stabilityValue -= value;
    }

    public void AddStabilityValue(int value)
    {
        stabilityValue += value;
    }

    void Start()
    {
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
