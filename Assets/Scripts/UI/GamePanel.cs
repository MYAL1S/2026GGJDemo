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

    // 手机屏幕相关
    private Image phoneScreenImage;
    private Transform phoneScreenObj;

    private PhoneItem phoneItem;
    private BellItem bellItem;

    // 乘客容器
    private RectTransform passengerContainer;

    // 驱逐UI相关
    private Transform expelUI;
    private Passenger selectedPassenger;

    // ? 正常和异常状态下的倒计时颜色
    private Color normalTimeColor = Color.white;
    private Color abnormalTimeColor = Color.red;

    public override void Init()
    {
        base.Init();
        txtFloor = GetControl<Text>("TxtFloor");
        txtTimeInfo = GetControl<Text>("TxtTimeInfo");
        
        // ? 保存正常颜色
        if (txtTimeInfo != null)
            normalTimeColor = txtTimeInfo.color;
        
        phoneScreenImage = GetControl<Image>("PhoneScreen");
        if (phoneScreenImage != null)
            phoneScreenObj = phoneScreenImage.transform;

        expelUI = transform.Find("ExpelUI");

        // 初始化乘客容器
        InitPassengerContainer();

        InitItemSystem();

        // 注册事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
        // ? 监听异常状态变化
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_AbnormalStateChanged, OnAbnormalStateChanged);

        HideExpelUI();
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

            var phoneScreenTransform = phoneObj.Find("PhoneScreen");
            if (phoneScreenTransform != null)
            {
                phoneScreenObj = phoneScreenTransform;
                phoneScreenImage = phoneScreenTransform.GetComponent<Image>();
                phoneItem.SetRenderTextureRoot(phoneScreenTransform.gameObject);
            }

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
        
        // ? 初始化时两个箭头都不亮（停止状态）
        SetArrowsVisible(false, false);
        
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

    /// <summary>
    /// 更新方向UI
    /// - 停止状态：两个箭头都不亮
    /// - 离开状态：两个箭头都亮
    /// - 移动/到达状态：根据方向只亮一个
    /// </summary>
    public void UpdateDirectionUI(bool isGoingUp)
    {
        E_ElevatorState currentState = ElevatorMgr.Instance.CurrentState;
        
        switch (currentState)
        {
            case E_ElevatorState.Stopped:
                // ⭐ 停靠时两个箭头都不亮
                SetArrowsVisible(false, false);
                break;
                
            case E_ElevatorState.Departing:
                // ⭐ 离开时两个箭头都亮
                SetArrowsVisible(true, true);
                break;
                
            case E_ElevatorState.Moving:
            case E_ElevatorState.Arriving:
                // ⭐ 运行/到达时根据方向显示
                // isGoingUp=true 表示上升，显示上箭头
                // isGoingUp=false 表示下降，显示下箭头
                SetArrowsVisible(isGoingUp, !isGoingUp);
                break;
                
            case E_ElevatorState.Abnormal:
                // 异常状态保持当前方向
                SetArrowsVisible(isGoingUp, !isGoingUp);
                break;
        }
    }

    /// <summary>
    /// 设置箭头可见性
    /// </summary>
    private void SetArrowsVisible(bool upVisible, bool downVisible)
    {
        if (rawImgUpDownArrow == null || rawImgUpDownArrow.Length < 2)
            return;

        if (rawImgUpDownArrow[0] != null)
            rawImgUpDownArrow[0].enabled = upVisible;
        if (rawImgUpDownArrow[1] != null)
            rawImgUpDownArrow[1].enabled = downVisible;
    }

    /// <summary>
    /// 更新倒计时UI（格式：MM:SS）
    /// </summary>
    public void UpdateCountdownUI(int remainingSeconds)
    {
        if (txtTimeInfo == null)
            return;
        
        if (remainingSeconds <= 0)
        {
            txtTimeInfo.text = "";
            return;
        }
        
        // ? 格式化为 分钟:秒
        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        txtTimeInfo.text = $"{minutes:D2}:{seconds:D2}";
    }

    #endregion

    #region 乘客交互

    /// <summary>
    /// 乘客被点击时的回调
    /// </summary>
    private void OnPassengerClicked(Passenger passenger)
    {
        if (passenger == null) return;

        // 清除之前选中乘客的高亮
        if (selectedPassenger != null && selectedPassenger != passenger)
        {
            selectedPassenger.SetHighlight(false);
        }

        selectedPassenger = passenger;
        passenger.SetHighlight(true);
        ShowExpelUI();
    }

    /// <summary>
    /// 显示驱逐UI
    /// </summary>
    public void ShowExpelUI()
    {
        if (expelUI != null)
            expelUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏驱逐UI
    /// </summary>
    public void HideExpelUI()
    {
        if (expelUI != null)
            expelUI.gameObject.SetActive(false);

        // 清除选中乘客的高亮
        if (selectedPassenger != null)
        {
            selectedPassenger.SetHighlight(false);
            selectedPassenger = null;
        }
    }

    /// <summary>
    /// 异常状态变化回调
    /// </summary>
    private void OnAbnormalStateChanged(bool isAbnormal)
    {
        if (txtTimeInfo != null)
        {
            txtTimeInfo.color = isAbnormal ? abnormalTimeColor : normalTimeColor;
        }
    }

    #endregion

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnSetup":
                UIMgr.Instance.ShowPanel<OptionsPanel>(E_UILayer.Top);
                break;
            case "BtnReturn":
                // ⭐ 停止游戏相关流程并返回开始场景
                ReturnToMainMenu();
                break;
            case "BtnTip":
                UIMgr.Instance.ShowPanel<TipPanel>(E_UILayer.Top);
                break;
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void ReturnToMainMenu()
    {
        StopGameProcesses();
        
        // ⭐ 清理音效列表
        MusicMgr.Instance.ClearSound();
        
        UIMgr.Instance.ClearAllPanels();
        
        SceneMgr.Instance.LoadSceneAsync("BeginScene", () =>
        {
            UIMgr.Instance.ShowPanel<MainMenuPanel>();
            MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
        });
    }

    /// <summary>
    /// 停止所有游戏相关流程
    /// </summary>
    private void StopGameProcesses()
    {
        // 停止电梯
        ElevatorMgr.Instance.StopElevator();
        
        // 清理所有乘客
        PassengerMgr.Instance.ClearAllPassengers();
        
        // 重置异常事件状态
        EventMgr.Instance.ResetState();
        
        // 停止背景音乐
        MusicMgr.Instance.StopBKMusic();
        
        // 恢复时间（以防被暂停）
        Time.timeScale = 1f;
        
        Debug.Log("[GamePanel] 已停止所有游戏流程");
    }

    public override void HideMe()
    {
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);
        EventCenter.Instance.RemoveEventListener<Passenger>(E_EventType.E_PassengerClicked, OnPassengerClicked);
        // ? 移除异常状态监听
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_AbnormalStateChanged, OnAbnormalStateChanged);
        base.HideMe();
    }

    /// <summary>
    /// GamePanel 不播放出现音效
    /// </summary>
    protected override void PlayShowSound()
    {
        // GamePanel 不播放音效
    }
}
