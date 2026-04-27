using UnityEngine;

/// <summary>
/// 游戏数据管理器
/// </summary>
public class GameDataMgr : BaseSingleton<GameDataMgr>
{

    /// <summary>
    /// 游戏设置数据
    /// </summary>
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

    /// <summary>
    /// 低灵能值警告阈值
    /// </summary>
    private const int LowPsychicThreshold = 4;
    /// <summary>
    /// 低灵能值警告音效路径（资源路径，需放在 Resources 文件夹下）
    /// </summary>
    private const string LowPsychicSoundPath = "Music/26GGJsound/percentlow";
    /// <summary>
    /// 循环音效源（如果正在播放低灵能警告音效，则不为 null）
    /// </summary>
    private AudioSource lowPsychicLoopSource;
    /// <summary>
    /// 低灵能音效是否正在加载中（防止重复加载）
    /// </summary>
    private bool lowPsychicSoundLoading;

    /// <summary>
    /// 信任值是否已耗尽（即玩家是否已失败）
    /// </summary>
    public bool IsTrustDepleted => _trustValue <= 0;

    private GameDataMgr()
    {
        // 从本地读取设置数据（设置数据需要持久化）
        settingInfo = JsonMgr.Instance.LoadData<SettingInfo>("Settings");
        
        // 读取玩家基础数据（如最大灵能值等配置）
        playerInfo = JsonMgr.Instance.LoadData<PlayerInfo>("PlayerInfo");

        //游戏开始时，信任度满值
        _trustValue = MaxTrustValue;
    }

    /// <summary>
    /// 重置游戏数据（每次开始新游戏时调用）
    /// </summary>
    public void ResetGameData()
    {
        // 重置信任度
        _trustValue = MaxTrustValue;
        //触发信任度变化事件 更新UI
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_StabilityChanged, _trustValue);

        // 重置灵能值
        if (playerInfo != null)
        {
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
            //触发灵能值变化事件 更新UI
            EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        }

        // 停止灵能告警音效
        StopLowPsychicSound();
    }

    /// <summary>
    /// 减少信任值
    /// </summary>
    /// <param name="value"></param>
    public void SubTrustValue(int value)
    {
        TrustValue -= value;
    } 


    #region 灵能值相关
    
    /// <summary>
    /// 增加灵能值
    /// </summary>
    /// <param name="value"></param>
    public void AddPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue += value;
        if (playerInfo.nowPsychicPowerValue > playerInfo.maxPsychicPowerValue)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
        //触发灵能值变化事件 更新UI
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
    }

    /// <summary>
    /// 消耗灵能值，成功返回 true（即当前灵能值足够消耗），否则返回 false（即当前灵能值不足）
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool ConsumePsychicPower(int value)
    {
        //如果灵能值不足 返回false 
        if (playerInfo.nowPsychicPowerValue < value)
            return false;
        //否则 消耗灵能值
        playerInfo.nowPsychicPowerValue -= value;
        //触发灵能值变化事件 更新UI
        EventCenter.Instance.EventTrigger<int>(E_EventType.E_PsychicPowerChanged, playerInfo.nowPsychicPowerValue);
        return true;
    }
    #endregion

    /// <summary>
    /// 保存当前设置到本地（在设置面板中调整设置后调用）
    /// </summary>
    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingInfo, "Settings");
    }


    /// <summary>
    /// 检查当前灵能值是否低于警告阈值，如果是则播放警告音效；如果不是则停止警告音效
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

    /// <summary>
    /// 停止灵能值警告背景音效
    /// </summary>
    private void StopLowPsychicSound()
    {
        if (lowPsychicLoopSource != null)
        {
            MusicMgr.Instance.StopSound(lowPsychicLoopSource);
            lowPsychicLoopSource = null;
        }
    }
}
