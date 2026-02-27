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
    [Tooltip("乘客生成点位（全局通用）- 目前仍在使用，用于随机选择可用生成点")]
    public List<Vector3> globalPassengerSpawnPoints = new List<Vector3>();

    [Header("乘客位置配置 - 停靠状态")]
    [Tooltip("电梯停靠时乘客的位置")]
    public List<Vector2> passengerDockingPositions = new List<Vector2>();

    [Tooltip("电梯停靠时乘客的缩放（如果你改为由容器控制，可保持为 Vector3.one）")]
    public List<Vector3> passengerDockingScales = new List<Vector3>();

    [Header("乘客位置配置 - 运行状态")]
    [Tooltip("电梯运行时乘客的位置")]
    public List<Vector2> passengerMovingPositions = new List<Vector2>();

    [Tooltip("电梯运行时乘客的缩放（如果你改为由容器控制，可保持为 Vector3.one）")]
    public List<Vector3> passengerMovingScales = new List<Vector3>();

    [Header("乘客容器缩放（全局）")]
    [Tooltip("乘客父容器在停靠时的缩放（由外部控制父容器）")]
    public Vector3 passengerContainerDockingScale = Vector3.one;

    [Tooltip("乘客父容器在电梯运行时的缩放（由外部控制父容器）")]
    public Vector3 passengerContainerMovingScale = Vector3.one;

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
    public int elevatorDepartingTime = 5;

    // ⭐ 新增配置
    [Tooltip("第一次到达时间/ArrivingTime(秒)")]
    public int firstArrivingTime = 3; // 单位：秒（举例，实际可在 Inspector 配置）

    [Tooltip("第一次停靠时间/StoppedTime(秒）")]
    public int firstDockingTime = 10;

    [Tooltip("楼层随机显示延迟（秒）")]
    public int floorRandomDisplayDelay = 3;

    [Tooltip("楼层随机显示间隔（毫秒）")]
    public int floorRandomDisplayInterval = 100;

    [Tooltip("胜利面板弹出延迟（秒）")]
    public int winPanelDelay = 3;
    [Header("游戏配置")]
    /// <summary>
    /// 特殊乘客最大数量
    /// </summary>
    public int maxSpecialPassengerCount;

    /// <summary>
    /// 铃铛最大触发次数
    /// </summary>
    public int maxMirrorOccourence;

    [Header("异常事件配置")]
    [Tooltip("异常事件触发概率 (0-1)")]
    [Range(0f, 1f)]
    public float abnormalEventChance = 0.2f;
    
    [Tooltip("异常事件超时时间（秒）")]
    public int abnormalEventTimeout = 10;

    [Header("铜镜事件配置")]
    [Tooltip("注释铜镜时被鬼杀死的概率 (0-1)")]
    [Range(0f, 1f)]
    public float mirrorDeathChance = 0.1f;
    
    [Tooltip("铜镜死亡判定间隔（秒）")]
    public float mirrorDeathCheckInterval = 2f;

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

    /// <summary>
    /// ⭐【新增】重置资源管理器的运行时数据
    /// </summary>
    public void ResetRuntimeData()
    {
        // 1. 复位游戏结束标记（最关键！）
        isGameOver = false;

        // 2. 复位信任值/稳定值（根据你的游戏逻辑，恢复满值）
        // 假设 passengerTrustValue 是通过 GameDataMgr 获取的，或者在这里维护
        // 如果是在这里维护：
        passengerTrustValue = 6;

        // 如果有其他计数器，也要在这里清零
        Debug.Log("[ResourcesMgr] 运行时数据已重置，解除 GameOver 锁定");
    }

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

        // 如果控制台疯狂刷这一行，且数值是 0，那就是这里的问题
        if (passengerTrustValue <= 0)
        {
            Debug.LogError($"[侦探模式] ResourcesMgr 判定游戏结束！因为 passengerTrustValue 为: {passengerTrustValue}");
        }
        isGameOver = passengerTrustValue <= 0;

        if (isGameOver)
            EventMgr.Instance.FallIntoAbyss();
    }

    /// <summary>
    /// 验证两组配置数据长度是否一致
    /// </summary>
    public bool ValidatePassengerPositionConfigs()
    {
        int dockingPosCount = passengerDockingPositions.Count;
        int dockingScaleCount = passengerDockingScales.Count;
        int movingPosCount = passengerMovingPositions.Count;
        int movingScaleCount = passengerMovingScales.Count;

        if (dockingPosCount != dockingScaleCount || 
            dockingPosCount != movingPosCount ||
            dockingPosCount != movingScaleCount)
        {
            Debug.LogError($"[ResourcesMgr] 乘客位置配置数量不一致! " +
                $"停靠位置:{dockingPosCount}, 停靠缩放:{dockingScaleCount}, " +
                $"运行位置:{movingPosCount}, 运行缩放:{movingScaleCount}");
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// 获取乘客点位数量
    /// </summary>
    public int PassengerSlotCount => passengerDockingPositions.Count;
}
