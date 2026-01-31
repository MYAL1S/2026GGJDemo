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

    // --- 灵能过低提示音相关 ---
    private const int LowPsychicThreshold = 5;
    // 请将低灵能警告音效放在 Resources/Music/26GGJsound/percentlow 路径下，或修改此常量
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
    }

    /// <summary>
    /// 保存当前的设置数据到本地
    /// </summary>
    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingInfo,"Settings");
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
            // 尚未播放且未在加载，开始加载并播放
            if (lowPsychicLoopSource == null && !lowPsychicSoundLoading)
            {
                lowPsychicSoundLoading = true;
                MusicMgr.Instance.PlaySound(LowPsychicSoundPath, true, source =>
                {
                    lowPsychicSoundLoading = false;
                    // 如果加载完成时灵能已恢复，则立刻停止
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
    /// 停止低灵能警告音
    /// </summary>
    private void StopLowPsychicSound()
    {
        if (lowPsychicLoopSource != null)
        {
            MusicMgr.Instance.StopSound(lowPsychicLoopSource);
            lowPsychicLoopSource = null;
        }
    }

    /// <summary>
    /// 增加玩家灵能值
    /// </summary>
    /// <param name="value">灵能值</param>
    public void AddPlayerPsychicPower(int value)
    {
        playerInfo.nowPsychicPowerValue += value;
        if (playerInfo.nowPsychicPowerValue > playerInfo.maxPsychicPowerValue)
        {
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
        }
    }
}
