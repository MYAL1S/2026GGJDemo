using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 设置数据类
/// 用于本地数据持久化
/// </summary>
public class SettingInfo
{
    /// <summary>
    /// 分辨率设置
    ///  1:1920x1080, 2:2560x1440, 3:1600x900, 4:1280x720
    /// </summary>
    public int resolutionType = 1;
    /// <summary>
    /// 是否全屏
    /// </summary>
    public bool isFullScreen = true;
    /// <summary>
    /// 是否开启了垂直同步
    /// </summary>
    public bool isVsyncOpen = true;
    /// <summary>
    /// 主音量大小
    /// </summary>
    public float masterVolume = 1;
    /// <summary>
    /// 背景音乐音量大小
    /// </summary>
    public float musicVolume = 0.5f;
    /// <summary>
    /// 音效大小
    /// </summary>
    public float fxVolume = 0.5f;
}
