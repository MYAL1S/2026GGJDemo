using UnityEngine;

/// <summary>
/// 设置应用工具：统一在启动/设置面板中复用
/// </summary>
public static class SettingsApplier
{
    /// <summary>索引转分辨率</summary>
    public static (int width, int height) ResolutionForIndex(int index)
    {
        switch (index)
        {
            case 0: return (1920, 1080);
            case 1: return (2560, 1440);
            case 2: return (1600, 900);
            case 3: return (1280, 720);
            default: return (Screen.currentResolution.width, Screen.currentResolution.height);
        }
    }

    /// <summary>按索引应用分辨率+全屏</summary>
    public static void ApplyResolution(int index, bool fullscreen)
    {
        var (w, h) = ResolutionForIndex(index);
        Screen.SetResolution(w, h, fullscreen);
    }

    /// <summary>应用垂直同步</summary>
    public static void ApplyVSync(bool on)
    {
        QualitySettings.vSyncCount = on ? 1 : 0;
    }

    /// <summary>应用音量相关</summary>
    public static void ApplyAudio(SettingInfo s)
    {
        if (s == null) return;
        MusicMgr.Instance.SetMasterValue(s.masterVolume);
        MusicMgr.Instance.SetBKMusicValue(s.musicVolume);
        MusicMgr.Instance.SetSoundValue(s.fxVolume);
    }

    /// <summary>一次性应用所有显示与音频设置</summary>
    public static void ApplyAll(SettingInfo s)
    {
        if (s == null) return;
        ApplyResolution(s.resolutionType, s.isFullScreen);
        ApplyVSync(s.isVsyncOpen);
        ApplyAudio(s);
    }
}