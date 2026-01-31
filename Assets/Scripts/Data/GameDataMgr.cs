using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏数据管理器 主要用于管理游戏中的各种数据
/// </summary>
public class GameDataMgr : BaseSingleton<GameDataMgr>
{
    // 游戏设置信息
    private SettingInfo settingInfo;
    public SettingInfo SettingInfo => settingInfo;

    /// <summary>
    /// 记录角色信息 如灵能值 当前拥有的面具等
    /// </summary>
    private PlayerInfo playerInfo;
    public PlayerInfo PlayerInfo => playerInfo;

    /// <summary>
    /// 记录电梯信息 如稳定值 电梯容量等
    /// </summary>
    private ElevatorInfo elevatorInfo;
    public ElevatorInfo ElevatorInfo => elevatorInfo;

    /// <summary>
    /// 记录的面具信息列表
    /// </summary>
    private List<MaskInfo> maskInfoList;
    public List<MaskInfo> MaskInfoList => maskInfoList;

    /// <summary>
    /// 信任度（原稳定度和信任值合并）
    /// </summary>
    private int _trustValue;
    public int TrustValue
    {
        get => _trustValue;
        private set
        {
            _trustValue = Mathf.Clamp(value, 0, MaxTrustValue);
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _trustValue);
        }
    }

    /// <summary>
    /// 最大信任度
    /// </summary>
    public int MaxTrustValue { get; private set; } = 5;

    // --- 灵能过低提示音相关 ---
    private const int LowPsychicThreshold = 2;
    private const string LowPsychicSoundPath = "Music/26GGJsound/percentlow";
    private AudioSource lowPsychicLoopSource;
    private bool lowPsychicSoundLoading;

    private GameDataMgr()
    {
        // 在调用构造函数时加载角色/电梯/面具/设置数据信息
        playerInfo = JsonMgr.Instance.LoadData<PlayerInfo>("PlayerInfo");
        elevatorInfo = JsonMgr.Instance.LoadData<ElevatorInfo>("ElevatorInfo");
        maskInfoList = JsonMgr.Instance.LoadData<List<MaskInfo>>("MaskInfo");
        settingInfo = JsonMgr.Instance.LoadData<SettingInfo>("Settings");

        // 初始化信任度
        _trustValue = MaxTrustValue;
    }

    /// <summary>
    /// 初始化信任度（游戏开始时调用）
    /// </summary>
    public void InitTrustValue(int maxValue)
    {
        MaxTrustValue = maxValue;
        _trustValue = maxValue;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _trustValue);
    }

    /// <summary>
    /// 扣除信任度
    /// </summary>
    public void SubTrustValue(int value)
    {
        TrustValue -= value;
        Debug.Log($"[GameDataMgr] 信任度减少 {value}，当前: {TrustValue}");
    }

    /// <summary>
    /// 增加信任度
    /// </summary>
    public void AddTrustValue(int value)
    {
        TrustValue += value;
        Debug.Log($"[GameDataMgr] 信任度增加 {value}，当前: {TrustValue}");
    }

    /// <summary>
    /// 检查信任度是否为0（游戏失败）
    /// </summary>
    public bool IsTrustDepleted => _trustValue <= 0;

    #region 灵能值操作方法

    /// <summary>
    /// 设置灵能值并触发UI更新事件
    /// </summary>
    public void SetPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue = Mathf.Clamp(value, 0, playerInfo.maxPsychicPowerValue);
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
    }

    /// <summary>
    /// 增加灵能值并触发UI更新事件
    /// </summary>
    public void AddPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue += value;
        if (playerInfo.nowPsychicPowerValue > playerInfo.maxPsychicPowerValue)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;

        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        Debug.Log($"[GameDataMgr] 灵能增加 {value}，当前: {playerInfo.nowPsychicPowerValue}");
    }

    /// <summary>
    /// 消耗灵能值并触发UI更新事件
    /// </summary>
    /// <returns>是否成功消耗</returns>
    public bool ConsumePsychicPower(int value)
    {
        if (playerInfo.nowPsychicPowerValue < value)
        {
            Debug.Log($"[GameDataMgr] 灵能不足，需要 {value}，当前: {playerInfo.nowPsychicPowerValue}");
            return false;
        }

        playerInfo.nowPsychicPowerValue -= value;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        Debug.Log($"[GameDataMgr] 消耗灵能 {value}，当前: {playerInfo.nowPsychicPowerValue}");
        return true;
    }

    /// <summary>
    /// 获取当前灵能值
    /// </summary>
    public int GetCurrentPsychicPower()
    {
        return playerInfo.nowPsychicPowerValue;
    }

    #endregion

    /// <summary>
    /// 保存当前的设置数据到本地
    /// </summary>
    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingInfo, "Settings");
    }

    /// <summary>
    /// 提供给外部调用的保存角色信息的方法
    /// </summary>
    public void SavePlayerInfo()
    {
        JsonMgr.Instance.SaveData(playerInfo, "PlayerInfo");
    }

    /// <summary>
    /// FixedUpdate 中检测灵能值，低于阈值则循环播放警告音，恢复则停止
    /// </summary>
    public void CheckPsychicPowerWarning()
    {
        if (playerInfo == null)
            return;

        if (playerInfo.nowPsychicPowerValue < LowPsychicThreshold)
        {
            if (lowPsychicLoopSource == null && !lowPsychicSoundLoading)
            {
                lowPsychicSoundLoading = true;
                MusicMgr.Instance.PlaySound(LowPsychicSoundPath, true, source =>
                {
                    lowPsychicSoundLoading = false;
                    if (playerInfo.nowPsychicPowerValue >= LowPsychicThreshold)
                    {
                        MusicMgr.Instance.StopSound(source);
                        return;
                    }
                    lowPsychicLoopSource = source;
                });
            }
        }
        else
        {
            StopLowPsychicSound();
        }
    }

    private void StopLowPsychicSound()
    {
        if (lowPsychicLoopSource != null)
        {
            MusicMgr.Instance.StopSound(lowPsychicLoopSource);
            lowPsychicLoopSource = null;
        }
    }
}
