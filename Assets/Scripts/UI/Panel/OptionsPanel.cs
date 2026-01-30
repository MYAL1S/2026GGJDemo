using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPanel : BasePanel
{
    /// <summary>
    /// 初始化面板
    /// </summary>
    public override void Init()
    {
        base.Init();
        //从数据管理器中获取当前的设置数据并同步到UI控件上
        GetControl<Dropdown>("DropDownResolution").value = GameDataMgr.Instance.SettingInfo.resolutionType;
        GetControl<Dropdown>("DropDownVsync").value = GameDataMgr.Instance.SettingInfo.isVsyncOpen ? 0 : 1;
        GetControl<Dropdown>("DropDownFullscreen").value = GameDataMgr.Instance.SettingInfo.isFullScreen ? 0 : 1;
        GetControl<Slider>("SliderMasterVolume").value = GameDataMgr.Instance.SettingInfo.masterVolume;
        GetControl<Slider>("SliderMusicVolume").value = GameDataMgr.Instance.SettingInfo.musicVolume;
        GetControl<Slider>("SliderFXVolume").value = GameDataMgr.Instance.SettingInfo.fxVolume;
    }
    protected override Selectable GetDefaultSelectable()
    {
        return GetControl<Dropdown>("DropDownResolution");
    }

    protected override void OnDropDownValueChanged(string name, int value)
    {
        base.OnDropDownValueChanged(name, value);
        switch (name)
        {
            case "DropDownResolution":
                // 立即预览分辨率（不持久化，持久化在点击保存时）
                ApplyResolutionByIndex(value);
                break;
            case "DropDownVsync":
                // 立即应用垂直同步设置（0 表示 开启，1 表示 关闭）
                QualitySettings.vSyncCount = (value == 0) ? 1 : 0;
                break ;
            case "DropDownFullscreen":
                SetFullScreen(value);
                break;
            default:
                break;
        }
    }

    protected override void OnSliderValueChanged(string name, float value)
    {
        base.OnSliderValueChanged(name, value);
        switch (name)
        {
            case "SliderMasterVolume":
                //当主音量变化时 同步更新背景音乐和音效的音量
                MusicMgr.Instance.SetMasterValue(value);
                break;
            case "SliderMusicVolume":
                MusicMgr.Instance.SetBKMusicValue(value);
                break;
            case "SliderFXVolume":
                MusicMgr.Instance.SetSoundValue(value);
                break;
            default:
                break;
        }
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnSave":
                //保存设置数据
                GameDataMgr.Instance.SettingInfo.resolutionType = GetControl<Dropdown>("DropDownResolution").value;
                GameDataMgr.Instance.SettingInfo.isVsyncOpen = GetControl<Dropdown>("DropDownVsync").value == 0 ? true : false;
                GameDataMgr.Instance.SettingInfo.isFullScreen = GetControl<Dropdown>("DropDownFullscreen").value == 0 ? true : false;
                GameDataMgr.Instance.SettingInfo.masterVolume = GetControl<Slider>("SliderMasterVolume").value;
                GameDataMgr.Instance.SettingInfo.musicVolume = GetControl<Slider>("SliderMusicVolume").value;
                GameDataMgr.Instance.SettingInfo.fxVolume = GetControl<Slider>("SliderFXVolume").value;
                GameDataMgr.Instance.SaveSettingData();
                MusicMgr.Instance.PlaySound("Music/FX/TapeButton", false);
                break;
            case "BtnExit":
                SettingsApplier.ApplyAll(GameDataMgr.Instance.SettingInfo);
                //关闭面板
                UIMgr.Instance.HidePanel<OptionsPanel>();
                //返回主菜单面板
                UIMgr.Instance.ShowPanel<MainMenuPanel>();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 根据下拉索引立即应用分辨率（用于预览）
    /// 下拉索引与Saved数据保持一致：0->1920x1080, 1->2560x1440, 2->1600x900, 3->1280x720
    /// 注意：真正保存由 BtnSave 操作完成
    /// </summary>
    /// <param name="index">下拉索引</param>
    private void ApplyResolutionByIndex(int index)
    {
        bool fullscreen = GetControl<Dropdown>("DropDownFullscreen").value == 0;
        SettingsApplier.ApplyResolution(index, fullscreen);

        int vSyncDropdownValue = GetControl<Dropdown>("DropDownVsync").value;
        SettingsApplier.ApplyVSync(vSyncDropdownValue == 0);
    }

    private void SetFullScreen(int value)
    {
        bool fullscreen = value == 0;
        int currentResIndex = GetControl<Dropdown>("DropDownResolution").value;
        SettingsApplier.ApplyResolution(currentResIndex, fullscreen);
    }

}
