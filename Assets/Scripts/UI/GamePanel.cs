using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏界面面板
/// </summary>
public class GamePanel : BasePanel
{
    private Text txtFloor;
    private Text txtTimeInfo;
    public List<RawImage> humanIconList;
    public List<RawImage> energyIconList;
    public List<RawImage> stabilityIconList;
    public RawImage[] rawImgUpDownArrow;
    private RawImage imgMirror;
    private Transform mirrorObj;
    
    /// <summary>
    /// 手机屏幕（遮罩方案使用 Image）
    /// </summary>
    private Image phoneScreenImage;
    private Transform phoneScreenObj;

    private PhoneItem phoneItem;
    private BellItem bellItem;

    // 乘客容器
    private RectTransform passengerContainer;

    public override void Init()
    {
        base.Init();
        txtFloor = GetControl<Text>("TxtFloor");
        txtTimeInfo = GetControl<Text>("TxtTimeInfo");
        imgMirror = GetControl<RawImage>("Mirror");
        mirrorObj = imgMirror.GetComponent<Transform>();
        
        // 获取手机屏幕 Image
        phoneScreenImage = GetControl<Image>("PhoneScreen");
        if (phoneScreenImage != null)
            phoneScreenObj = phoneScreenImage.transform;

        // 初始化乘客容器
        InitPassengerContainer();

        InitItemSystem();

        // 注册事件
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);

        HideMirrorUI();
        InitPlayerData();
        InitUIDisplay();
    }

    /// <summary>
    /// 初始化乘客容器
    /// </summary>
    private void InitPassengerContainer()
    {
        Transform existingContainer = transform.Find("PassengerContainer");
        if (existingContainer != null)
        {
            passengerContainer = existingContainer.GetComponent<RectTransform>();
            return;
        }

        GameObject containerObj = new GameObject("PassengerContainer");
        passengerContainer = containerObj.AddComponent<RectTransform>();
        passengerContainer.SetParent(transform, false);

        passengerContainer.anchorMin = Vector2.zero;
        passengerContainer.anchorMax = Vector2.one;
        passengerContainer.offsetMin = Vector2.zero;
        passengerContainer.offsetMax = Vector2.zero;
        passengerContainer.pivot = new Vector2(0.5f, 0.5f);

        passengerContainer.SetAsFirstSibling();
    }

    public Transform GetPassengerContainer()
    {
        return passengerContainer;
    }

    private void InitItemSystem()
    {
        ItemConfigSO itemConfig = Resources.Load<ItemConfigSO>("Config/ItemConfig");

        var phoneObj = transform.Find("PhoneItem");
        if (phoneObj != null)
        {
            phoneItem = phoneObj.GetComponent<PhoneItem>();
            if (phoneItem == null)
                phoneItem = phoneObj.gameObject.AddComponent<PhoneItem>();
            
            if (phoneScreenObj != null)
                phoneItem.SetRenderTextureRoot(phoneScreenObj.gameObject);
            
            if (itemConfig != null)
                phoneItem.SetItemConfig(itemConfig);
        }

        var bellObj = transform.Find("BellItem");
        if (bellObj != null)
        {
            bellItem = bellObj.GetComponent<BellItem>();
            if (bellItem == null)
                bellItem = bellObj.gameObject.AddComponent<BellItem>();
            if (itemConfig != null)
                bellItem.SetItemConfig(itemConfig);
        }

        if (phoneScreenObj != null)
        {
            phoneScreenObj.gameObject.SetActive(false);
            SetupPhoneScreenCanvasSorting();
        }
    }

    /// <summary>
    /// 设置手机屏幕的 Canvas 排序层级
    /// </summary>
    private void SetupPhoneScreenCanvasSorting()
    {
        if (phoneScreenObj == null)
            return;

        Canvas phoneCanvas = phoneScreenObj.GetComponent<Canvas>();
        if (phoneCanvas == null)
            phoneCanvas = phoneScreenObj.gameObject.AddComponent<Canvas>();
        phoneCanvas.overrideSorting = true;
        phoneCanvas.sortingOrder = 100;

        if (phoneScreenObj.GetComponent<GraphicRaycaster>() == null)
            phoneScreenObj.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void InitPlayerData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
            return;

        if (playerInfo.nowPsychicPowerValue <= 0)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
    }

    private void InitUIDisplay()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo != null)
            UpdateEnergyUI(playerInfo.nowPsychicPowerValue);

        UpdateStabilityUI(GameDataMgr.Instance.TrustValue);

        int passengerCount = PassengerMgr.Instance.passengerList?.Count ?? 0;
        UpdatePassengerUI(passengerCount);
        UpdateDirectionUI(true);
        UpdateCountdownUI(0);
    }

    #region UI更新方法

    public void UpdateEnergyUI(int currentEnergy)
    {
        if (energyIconList == null || energyIconList.Count == 0)
            return;

        int displayCount = Mathf.Clamp(currentEnergy, 0, energyIconList.Count);
        for (int i = 0; i < energyIconList.Count; i++)
        {
            if (energyIconList[i] != null)
                energyIconList[i].gameObject.SetActive(i < displayCount);
        }
    }

    public void UpdateStabilityUI(int currentStability)
    {
        if (stabilityIconList == null || stabilityIconList.Count == 0)
            return;

        int displayCount = Mathf.Clamp(currentStability, 0, stabilityIconList.Count);
        for (int i = 0; i < stabilityIconList.Count; i++)
        {
            if (stabilityIconList[i] != null)
                stabilityIconList[i].gameObject.SetActive(i < displayCount);
        }
    }

    public void UpdatePassengerUI(int currentPassengerCount)
    {
        if (humanIconList == null || humanIconList.Count == 0)
            return;

        int displayCount = Mathf.Clamp(currentPassengerCount, 0, humanIconList.Count);
        for (int i = 0; i < humanIconList.Count; i++)
        {
            if (humanIconList[i] != null)
                humanIconList[i].gameObject.SetActive(i < displayCount);
        }
    }

    public void UpdateDirectionUI(bool isGoingUp)
    {
        if (rawImgUpDownArrow == null || rawImgUpDownArrow.Length < 2)
            return;

        if (rawImgUpDownArrow[0] != null)
            rawImgUpDownArrow[0].enabled = isGoingUp;
        if (rawImgUpDownArrow[1] != null)
            rawImgUpDownArrow[1].enabled = !isGoingUp;
    }

    public void UpdateCountdownUI(int remainingSeconds)
    {
        if (txtTimeInfo == null)
            return;
        txtTimeInfo.text = remainingSeconds <= 0 ? "" : remainingSeconds.ToString();
    }

    #endregion

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnGaze":
                EventMgr.Instance.StartWatchMirror();
                break;
            case "BtnLeave":
                HideMirrorUI();
                EventMgr.Instance.StopWatchMirror();
                break;
            case "BtnSetup":
                UIMgr.Instance.HidePanel<GamePanel>();
                UIMgr.Instance.ShowPanel<OptionsPanel>();
                break;
            case "BtnReturn":
                UIMgr.Instance.HidePanel<GamePanel>();
                UIMgr.Instance.ShowPanel<MainMenuPanel>();
                break;
            case "BtnTip":
                UIMgr.Instance.ShowPanel<TipPanel>();
                break;
        }
    }

    public void ShowMirrorUI() => mirrorObj.gameObject.SetActive(true);
    public void HideMirrorUI() => mirrorObj.gameObject.SetActive(false);

    private void UpdateMirrorUI() { }

    public override void HideMe()
    {
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);
        base.HideMe();
    }
}
