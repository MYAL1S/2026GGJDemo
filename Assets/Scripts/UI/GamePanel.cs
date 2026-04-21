using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏界面面板
/// </summary>
public class GamePanel : BasePanel
{
    /// <summary>
    /// 楼层信息文本
    /// </summary>
    private Text txtFloor;
    /// <summary>
    /// 时间信息文本
    /// </summary>
    private Text txtTimeInfo;
    /// <summary>
    /// 乘客图标列表 用于显示当前电梯中的乘客数量
    /// </summary>
    public List<RawImage> humanIconList;
    /// <summary>
    /// 灵能图标列表 用于显示当前玩家的灵能值
    /// </summary>
    public List<RawImage> energyIconList;
    /// <summary>
    /// 稳定性图标列表 用于显示当前玩家的稳定性值
    /// </summary>
    public List<RawImage> stabilityIconList;
    /// <summary>
    /// 标识电梯当前方向的箭头图像数组
    /// </summary>
    public RawImage[] rawImgUpDownArrow;

    /// <summary>
    /// 手机屏幕图像组件和Transform 用于物品系统中显示手机屏幕内容
    /// </summary>
    private Image phoneScreenImage;
    private Transform phoneScreenObj;

    /// <summary>
    /// 手机物体和铃铛物体 用于物品系统交互
    /// </summary>
    private PhoneItem phoneItem;
    private BellItem bellItem;

    // 乘客容器
    private RectTransform passengerContainer;

    // 驱逐UI相关
    private Passenger selectedPassenger;

    // 正常和异常状态下的倒计时颜色
    private Color normalTimeColor = Color.white;
    private Color abnormalTimeColor = Color.red;

    // 保存楼层默认颜色和覆盖标志
    private Color normalFloorColor = Color.white;
    private bool isFloorOverridden = false;

    public override void Init()
    {
        base.Init();
        txtFloor = GetControl<Text>("TxtFloor");
        txtTimeInfo = GetControl<Text>("TxtTimeInfo");
        
        // 保存正常颜色
        if (txtTimeInfo != null)
            normalTimeColor = txtTimeInfo.color;
        
        // 保存楼层默认颜色
        if (txtFloor != null)
            normalFloorColor = txtFloor.color;

        phoneScreenImage = GetControl<Image>("PhoneScreen");
        if (phoneScreenImage != null)
            phoneScreenObj = phoneScreenImage.transform;

        // 初始化乘客容器
        InitPassengerContainer();

        InitItemSystem();

        // 注册事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);
        // 监听异常状态变化
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_AbnormalStateChanged, OnAbnormalStateChanged);

        InitPlayerData();
        InitUIDisplay();
    }

    /// <summary>
    /// 初始化乘客容器
    /// </summary>
    private void InitPassengerContainer()
    {
        /// 如果场景中已经存在 PassengerContainer 则直接使用
        Transform existingContainer = transform.Find("PassengerContainer");
        if (existingContainer != null)
        {
            passengerContainer = existingContainer.GetComponent<RectTransform>();
            return;
        }

        // 否则创建一个新的 PassengerContainer
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

    /// <summary>
    /// 初始化物品系统
    /// </summary>
    private void InitItemSystem()
    {
        // 读取物品配置
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

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    private void InitPlayerData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
            return;

        if (playerInfo.nowPsychicPowerValue <= 0)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;
    }

    /// <summary>
    /// 初始化UI显示
    /// </summary>
    private void InitUIDisplay()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo != null)
            UpdateEnergyUI(playerInfo.nowPsychicPowerValue);

        UpdateStabilityUI(GameDataMgr.Instance.TrustValue);

        int passengerCount = PassengerMgr.Instance.passengerList?.Count ?? 0;
        UpdatePassengerUI(passengerCount);
        
        // 初始化时两个箭头都不亮（停止状态）
        SetArrowsVisible(false, false);
        
        UpdateCountdownUI(0);
    }

    #region UI更新方法
    /// <summary>
    /// 更新灵能值UI
    /// </summary>
    /// <param name="currentEnergy"></param>
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

    /// <summary>
    /// 更新稳定度UI
    /// </summary>
    /// <param name="currentStability"></param>
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

    /// <summary>
    /// 更新电梯内乘客数量UI
    /// </summary>
    /// <param name="currentPassengerCount"></param>
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
    /// - 离开时：两个箭头都亮
    /// - 移动/到达时：根据方向只亮一个
    /// </summary>
    public void UpdateDirectionUI(bool isGoingUp)
    {
        E_ElevatorState currentState = ElevatorMgr.Instance.CurrentState;
        
        switch (currentState)
        {
            case E_ElevatorState.Stopped:
                // 停靠时两个箭头都不亮
                SetArrowsVisible(false, false);
                break;
                
            case E_ElevatorState.Departing:
                // 离开时两个箭头都亮
                SetArrowsVisible(true, true);
                break;
                
            case E_ElevatorState.Moving:
            case E_ElevatorState.Arriving:
                // 运行/到达时根据方向显示
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
            txtTimeInfo.text = "00:00";
            return;
        }
        
        // 格式化为 分钟:秒
        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        txtTimeInfo.text = $"{minutes:D2}:{seconds:D2}";
    }

    #endregion

    #region 乘客交互
    /// <summary>
    /// 异常状态变化回调
    /// - 异常开始时覆盖楼层显示并变红（阻止其他写入）
    /// - 异常结束时清理覆盖标志并恢复颜色（楼层文本由 ElevatorMgr 恢复）
    /// </summary>
    private void OnAbnormalStateChanged(bool isAbnormal)
    {
        // 更新时间文本颜色
        if (txtTimeInfo != null)
            txtTimeInfo.color = isAbnormal ? abnormalTimeColor : normalTimeColor;

        if (txtFloor == null)
            return;

        if (isAbnormal)
        {
            // 开始异常：覆盖楼层显示并阻止外部写入
            isFloorOverridden = true;
            SetFloorText("-18");
            txtFloor.color = Color.red;
        }
        else
        {
            // 结束异常：清理覆盖标志并恢复颜色，真实楼层由 ElevatorMgr 恢复
            isFloorOverridden = false;
            txtFloor.color = normalFloorColor;
        }
    }

    /// <summary>
    /// 尝试设置楼层显示——尊重异常覆盖（若正在覆盖则返回 false）
    /// </summary>
    public bool TrySetFloor(int level)
    {
        if (isFloorOverridden)
            return false;
        SetFloorText(level.ToString());
        return true;
    }

    /// <summary>
    /// 设置楼层显示文本 仅供 ElevatorMgr 在非异常状态下调用（不检查覆盖标志）
    /// </summary>
    /// <param name="text"></param>
    private void SetFloorText(string text)
    {
        if (txtFloor != null)
            txtFloor.text = text;
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
                // 停止游戏相关流程并返回开始场景
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
        // 停止所有游戏相关流程（电梯、乘客、事件等）
        StopGameProcesses();
        
        // 清理音效列表
        MusicMgr.Instance.ClearSound();

        // 清理UI面板
        UIMgr.Instance.ClearAllPanels();

        // 加载开始场景并显示主菜单
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
    }

    public override void HideMe()
    {
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.RemoveEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);
        // 移除异常状态监听
        EventCenter.Instance.RemoveEventListener<bool>(E_EventType.E_AbnormalStateChanged, OnAbnormalStateChanged);
        base.HideMe();
    }
}
