using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 设置面板类，负责显示和管理游戏设置界面，允许玩家调整分辨率、全屏模式、VSync 以及音量等设置
/// </summary>
public class OptionsPanel : BasePanel
{
    /// <summary>
    /// OptionsPanel 排序层级
    /// </summary>
    private const int PANEL_SORTING_ORDER = 300;
    private const int MASK_SORTING_ORDER = 250;

    // 添加字段记录是否从游戏中打开
    private bool openedFromGame = false;

    /// <summary>
    /// 初始化面板
    /// </summary>
    public override void Init()
    {
        base.Init();
        
        SetupCanvasSorting();
        SetupBlockingMask();

        // 加载当前设置到UI控件
        GetControl<Dropdown>("DropDownResolution").value = GameDataMgr.Instance.SettingInfo.resolutionType;
        GetControl<Dropdown>("DropDownVsync").value = GameDataMgr.Instance.SettingInfo.isVsyncOpen ? 0 : 1;
        GetControl<Dropdown>("DropDownFullscreen").value = GameDataMgr.Instance.SettingInfo.isFullScreen ? 0 : 1;
        GetControl<Slider>("SliderMasterVolume").value = GameDataMgr.Instance.SettingInfo.masterVolume;
        GetControl<Slider>("SliderMusicVolume").value = GameDataMgr.Instance.SettingInfo.musicVolume;
        GetControl<Slider>("SliderFXVolume").value = GameDataMgr.Instance.SettingInfo.fxVolume;
    }

    /// <summary>
    /// 设置 Canvas 排序层级，确保 OptionsPanel 在所有 UI 之上显示
    /// </summary>
    private void SetupCanvasSorting()
    {
        //如果已经有 Canvas 组件了，就直接设置排序
        //如果没有，就添加一个新的 Canvas 组件并设置排序
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = PANEL_SORTING_ORDER;

        // 确保有 GraphicRaycaster 组件，否则 UI 交互会失效
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 设置遮罩层，阻挡底层 UI 的交互，确保玩家只能与设置面板进行交互
    /// </summary>
    private void SetupBlockingMask()
    {
        Transform existingMask = transform.Find("BlockingMask");
        if (existingMask != null)
        {
            Canvas existingCanvas = existingMask.GetComponent<Canvas>();
            if (existingCanvas == null)
            {
                existingCanvas = existingMask.gameObject.AddComponent<Canvas>();
                existingCanvas.overrideSorting = true;
                existingCanvas.sortingOrder = MASK_SORTING_ORDER;
                existingMask.gameObject.AddComponent<GraphicRaycaster>();
            }
            return;
        }

        GameObject maskObj = new GameObject("BlockingMask");
        maskObj.transform.SetParent(transform, false);
        maskObj.transform.SetAsFirstSibling();

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = MASK_SORTING_ORDER;
        maskObj.AddComponent<GraphicRaycaster>();

        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = new Color(0, 0, 0, 0.5f);
        maskImage.raycastTarget = true;
    }

    /// <summary>
    /// 设置默认选中项为分辨率下拉框，方便玩家直接调整分辨率设置
    /// </summary>
    /// <returns></returns>
    protected override Selectable GetDefaultSelectable()
    {
        return GetControl<Dropdown>("DropDownResolution");
    }

    public override void ShowMe()
    {
        base.ShowMe();

        // [修改] 既然已经分了 BeginScene 和 GameScene，直接用场景名判断最安全
        // 如果当前场景不是 "BeginScene"，那肯定就是在游戏中
        string currentScene = SceneManager.GetActiveScene().name;
        openedFromGame = currentScene != "BeginScene";

        // 暂停游戏 (如果在游戏中)
        if (openedFromGame)
        {
            Time.timeScale = 0f;
        }
    }


    protected override void OnDropDownValueChanged(string name, int value)
    {
        base.OnDropDownValueChanged(name, value);
        switch (name)
        {
            case "DropDownResolution":
                ApplyResolutionByIndex(value);
                break;
            case "DropDownVsync":
                QualitySettings.vSyncCount = (value == 0) ? 1 : 0;
                break;
            case "DropDownFullscreen":
                SetFullScreen(value);
                break;
        }
    }

    protected override void OnSliderValueChanged(string name, float value)
    {
        base.OnSliderValueChanged(name, value);
        switch (name)
        {
            case "SliderMasterVolume":
                MusicMgr.Instance.SetMasterValue(value);
                break;
            case "SliderMusicVolume":
                MusicMgr.Instance.SetBKMusicValue(value);
                break;
            case "SliderFXVolume":
                MusicMgr.Instance.SetSoundValue(value);
                break;
        }
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnSave":
                // 保存设置数据
                GameDataMgr.Instance.SettingInfo.resolutionType = GetControl<Dropdown>("DropDownResolution").value;
                GameDataMgr.Instance.SettingInfo.isVsyncOpen = GetControl<Dropdown>("DropDownVsync").value == 0;
                GameDataMgr.Instance.SettingInfo.isFullScreen = GetControl<Dropdown>("DropDownFullscreen").value == 0;
                GameDataMgr.Instance.SettingInfo.masterVolume = GetControl<Slider>("SliderMasterVolume").value;
                GameDataMgr.Instance.SettingInfo.musicVolume = GetControl<Slider>("SliderMusicVolume").value;
                GameDataMgr.Instance.SettingInfo.fxVolume = GetControl<Slider>("SliderFXVolume").value;
                GameDataMgr.Instance.SaveSettingData();
                MusicMgr.Instance.PlaySound("Music/FX/TapeButton", false);
                break;
            case "BtnExit":
                //防止遗漏，在退出前再次应用设置，确保玩家的调整生效
                SettingsApplier.ApplyAll(GameDataMgr.Instance.SettingInfo);
                UIMgr.Instance.HidePanel<OptionsPanel>();

                // 恢复游戏时间流逝，确保玩家回到游戏时不会继续暂停
                Time.timeScale = 1f;

                //如果是从游戏中打开的设置面板，退出后回到主菜单；如果是从主菜单打开的设置面板，退出后继续留在主菜单
                if (!openedFromGame)
                    UIMgr.Instance.ShowPanel<MainMenuPanel>();
                break;
        }
    }

    /// <summary>
    /// 根据分辨率下拉框的索引应用分辨率设置，同时考虑全屏模式的状态
    /// </summary>
    /// <param name="index"></param>
    private void ApplyResolutionByIndex(int index)
    {
        bool fullscreen = GetControl<Dropdown>("DropDownFullscreen").value == 0;
        SettingsApplier.ApplyResolution(index, fullscreen);

        //在 Unity 中，调用 Screen.SetResolution 有时会重置或影响 VSync 的实际生效
        //某些平台/驱动下，分辨率切换后，VSync 选项会被系统或引擎重置为默认值，导致之前设置的 VSync 状态失效
        //先设置分辨率，再设置 VSync，可以确保最终的 VSync 状态是期望的。
        //如果先设置 VSync，再设置分辨率，分辨率切换可能会覆盖 VSync 设置，导致 VSync 失效或变为默认
        int vSyncDropdownValue = GetControl<Dropdown>("DropDownVsync").value;
        SettingsApplier.ApplyVSync(vSyncDropdownValue == 0);
    }

    /// <summary>
    /// 设置全屏模式，根据全屏下拉框的值应用分辨率设置
    /// 同时确保 VSync 设置也被正确应用，避免分辨率切换后 VSync 失效的问题
    /// </summary>
    /// <param name="value"></param>
    private void SetFullScreen(int value)
    {
        bool fullscreen = value == 0;
        int currentResIndex = GetControl<Dropdown>("DropDownResolution").value;
        SettingsApplier.ApplyResolution(currentResIndex, fullscreen);

        //在 Unity 中，调用 Screen.SetResolution 有时会重置或影响 VSync 的实际生效
        //某些平台/驱动下，分辨率切换后，VSync 选项会被系统或引擎重置为默认值，导致之前设置的 VSync 状态失效
        //先设置分辨率，再设置 VSync，可以确保最终的 VSync 状态是期望的。
        //如果先设置 VSync，再设置分辨率，分辨率切换可能会覆盖 VSync 设置，导致 VSync 失效或变为默认
        int vSyncDropdownValue = GetControl<Dropdown>("DropDownVsync").value;
        SettingsApplier.ApplyVSync(vSyncDropdownValue == 0);
    }

    /// <summary>
    /// 播放面板出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        MusicMgr.Instance.PlaySound("Music/26GGJsound/window_appear", false);
    }
}
