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
    /// 保存当前的设置数据到本地
    /// </summary>
    public void SaveSettingData()
    {
        JsonMgr.Instance.SaveData(settingInfo,"Settings");
    }


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


    private GameDataMgr()
    {
        ////在调用构造函数时加载关卡数据信息
        ////关卡信息存储在StreamingAssets/LevelInfo.json文件中
        ////可以通过Excel配置之后转为json文件进行存储
        //levelInfoList = JsonMgr.Instance.LoadData<List<LevelInfo>>("LevelInfo");


        //在调用构造函数时加载角色数据信息
        //角色信息存储在StreamingAssets/PlayerInfo.json文件中
        playerInfo = JsonMgr.Instance.LoadData<PlayerInfo>("PlayerInfo");
        ////在调用构造函数时加载乘客数据信息
        ////乘客信息存储在StreamingAssets/PassengerInfo.json文件中
        //passengerInfoList = JsonMgr.Instance.LoadData<List<PassengerInfo>>("PassengerInfo");
        //在调用构造函数时加载电梯数据信息
        //电梯信息存储在StreamingAssets/ElevatorInfo.json文件中
        elevatorInfo = JsonMgr.Instance.LoadData<ElevatorInfo>("ElevatorInfo");
        //在调用构造函数时加载面具数据信息
        //面具信息存储在StreamingAssets/MaskInfo.json文件中
        maskInfoList = JsonMgr.Instance.LoadData<List<MaskInfo>>("MaskInfo");

        settingInfo = JsonMgr.Instance.LoadData<SettingInfo>("Settings");
    }

    /// <summary>
    /// 提供给外部调用的保存角色信息的方法
    /// </summary>
    public void SavePlayerInfo()
    {
        JsonMgr.Instance.SaveData(playerInfo, "PlayerInfo");
    }
}
