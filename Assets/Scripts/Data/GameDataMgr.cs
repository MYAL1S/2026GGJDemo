using UnityEngine;

/// <summary>
/// 游戏数据管理器
/// </summary>
public class GameDataMgr : BaseSingleton<GameDataMgr>
{
    // 游戏设置信息
    private SettingInfo settingInfo;
    public SettingInfo SettingInfo => settingInfo;

    /// <summary>
    /// 角色信息
    /// </summary>
    private PlayerInfo playerInfo;
    public PlayerInfo PlayerInfo => playerInfo;

    /// <summary>
    /// 信任度
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
    public int MaxTrustValue { get; private set; } = 6;

    // 灵能告警相关
    private const int LowPsychicThreshold = 4;
    private const string LowPsychicSoundPath = "Music/26GGJsound/percentlow";
    private AudioSource lowPsychicLoopSource;
    private bool lowPsychicSoundLoading;

    private GameDataMgr()
    {
        // 从本地读取设置数据（设置数据需要持久化）
        settingInfo = JsonMgr.Instance.LoadData<SettingInfo>("Settings");
        
        // 读取玩家基础数据（如最大灵能值等配置）
        playerInfo = JsonMgr.Instance.LoadData<PlayerInfo>("PlayerInfo");
        
        _trustValue = MaxTrustValue;
    }

    /// <summary>
    /// ? 重置游戏数据（每次开始新游戏时调用）
    /// </summary>
    public void ResetGameData()
    {
        // 重置信任度
        _trustValue = MaxTrustValue;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _trustValue);

        // 重置灵能值
        if (playerInfo != null)
        {
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        }

        // 停止灵能告警音效
        StopLowPsychicSound();

        Debug.Log($"[GameDataMgr] 游戏数据已重置 - 信任度:{_trustValue}, 灵能:{playerInfo?.nowPsychicPowerValue}");
    }

    public void InitTrustValue(int maxValue)
    {
        MaxTrustValue = maxValue;
        _trustValue = maxValue;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _trustValue);
    }

    public void SubTrustValue(int value)
    {
        TrustValue -= value;
    }

    public void AddTrustValue(int value)
    {
        TrustValue += value;
    }

    public bool IsTrustDepleted => _trustValue <= 0;

    #region 灵能值相关

    public void SetPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue = Mathf.Clamp(value, 0, playerInfo.maxPsychicPowerValue);
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
    }

    public void AddPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue += value;
        if (playerInfo.nowPsychicPowerValue > playerInfo.maxPsychicPowerValue)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
    }

    public bool ConsumePsychicPower(int value)
    {
        if (playerInfo.nowPsychicPowerValue < value)
            return false;
        playerInfo.nowPsychicPowerValue -= value;
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        return true;
    }

    public int GetCurrentPsychicPower() => playerInfo.nowPsychicPowerValue;

    #endregion

    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingInfo, "Settings");
    }

    public void SavePlayerInfo()
    {
        JsonMgr.Instance.SaveData(playerInfo, "PlayerInfo");
    }

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
