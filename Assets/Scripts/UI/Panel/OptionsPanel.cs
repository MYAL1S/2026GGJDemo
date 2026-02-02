using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsPanel : BasePanel
{
    private float alphaSpeed = 5f;
    private bool isPanelShowing = false;
    private CanvasGroup panelCanvasGroup;
    private bool isFadingOut = false;

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
        
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

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

    private void SetupCanvasSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = PANEL_SORTING_ORDER;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

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

    protected override Selectable GetDefaultSelectable()
    {
        return GetControl<Dropdown>("DropDownResolution");
    }

    public override void ShowMe()
    {
        isPanelShowing = true;
        isFadingOut = false;
        panelCanvasGroup.alpha = 0;
        
        // ? 检查是否从游戏中打开
        openedFromGame = ElevatorMgr.Instance.CurrentState != E_ElevatorState.Stopped || 
                         UIMgr.Instance.IsPanelShowing<GamePanel>();
        
        // ? 暂停游戏
        Time.timeScale = 0f;
    }

    public override void HideMe()
    {
        isPanelShowing = false;
        isFadingOut = true;
    }

    protected override void Update()
    {
        // 淡入
        if (isPanelShowing && panelCanvasGroup.alpha < 1)
        {
            panelCanvasGroup.alpha += alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha >= 1)
                panelCanvasGroup.alpha = 1;
        }
        // 淡出
        else if (isFadingOut && panelCanvasGroup.alpha > 0)
        {
            panelCanvasGroup.alpha -= alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha <= 0)
            {
                panelCanvasGroup.alpha = 0;
                isFadingOut = false;
                
                // ? 恢复游戏
                Time.timeScale = 1f;
                UIMgr.Instance.HidePanel<OptionsPanel>();
            }
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
                SettingsApplier.ApplyAll(GameDataMgr.Instance.SettingInfo);
                HideMe();
                
                // ? 根据来源返回不同面板
                if (openedFromGame)
                {
                    // 从游戏中打开，返回游戏
                    // 不需要显示其他面板，GamePanel 还在
                }
                else
                {
                    // 从主菜单打开，返回主菜单
                    UIMgr.Instance.ShowPanel<MainMenuPanel>();
                }
                break;
        }
    }

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

    /// <summary>
    /// 播放面板出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        MusicMgr.Instance.PlaySound("Music/26GGJsound/window_appear", false);
    }
}
