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
    /// 游戏是否已结束
    /// </summary>
    public bool isGameOver = false;

    /// <summary>
    /// 获取乘客点位数量
    /// </summary>
    public int PassengerSlotCount => passengerDockingPositions.Count;

    /// <summary>
    /// 重置资源管理器的运行时数据
    /// </summary>
    public void ResetRuntimeData()
    {
        // 复位游戏结束标记
        isGameOver = false;

        // 复位信任值/稳定值
        passengerTrustValue = GameDataMgr.Instance.MaxTrustValue;
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
