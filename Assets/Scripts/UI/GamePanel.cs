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
    private RawImage randerTexture;
    private Transform renderTextureObj;
    private Passenger nowSelectedPassenger;

    private PhoneItem phoneItem;
    private BellItem bellItem;

    public override void Init()
    {
        base.Init();
        txtFloor = GetControl<Text>("TxtFloor");
        txtTimeInfo = GetControl<Text>("TxtTimeInfo");
        imgMirror = GetControl<RawImage>("Mirror");
        mirrorObj = imgMirror.GetComponent<Transform>();
        randerTexture = GetControl<RawImage>("RenderTexture");
        renderTextureObj = randerTexture.GetComponent<Transform>();

        InitItemSystem();

        // 注册事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PsychicPowerChanged, UpdateEnergyUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_StabilityChanged, UpdateStabilityUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_PassengerCountChanged, UpdatePassengerUI);
        EventCenter.Instance.AddEventListener<bool>(E_EventType.E_ElevatorDirectionChanged, UpdateDirectionUI);
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_CountdownUpdate, UpdateCountdownUI);

        HideMirrorUI();
        InitPlayerData();
        InitUIDisplay();

        Debug.Log("[GamePanel] 初始化完成");
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

            phoneItem.SetRenderTextureRoot(renderTextureObj.gameObject);
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

        if (renderTextureObj != null)
            renderTextureObj.gameObject.SetActive(false);
    }

    private void InitPlayerData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[GamePanel] PlayerInfo 为空");
            return;
        }

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

        // 初始化方向显示（默认隐藏）
        UpdateDirectionUI(true);

        // 初始化倒计时显示
        UpdateCountdownUI(0);
    }

    #region UI更新方法

    public void UpdateEnergyUI(int currentEnergy)
    {
        if (energyIconList == null || energyIconList.Count == 0)
            return;

        int maxDisplay = energyIconList.Count;
        int displayCount = Mathf.Clamp(currentEnergy, 0, maxDisplay);

        for (int i = 0; i < energyIconList.Count; i++)
        {
            if (energyIconList[i] != null)
                energyIconList[i].gameObject.SetActive(i < displayCount);
        }

        Debug.Log($"[GamePanel] 灵能UI更新: {displayCount}/{maxDisplay}");
    }

    public void UpdateStabilityUI(int currentStability)
    {
        if (stabilityIconList == null || stabilityIconList.Count == 0)
            return;

        int maxDisplay = stabilityIconList.Count;
        int displayCount = Mathf.Clamp(currentStability, 0, maxDisplay);

        for (int i = 0; i < stabilityIconList.Count; i++)
        {
            if (stabilityIconList[i] != null)
                stabilityIconList[i].gameObject.SetActive(i < displayCount);
        }

        Debug.Log($"[GamePanel] 稳定度UI更新: {displayCount}/{maxDisplay}");
    }

    public void UpdatePassengerUI(int currentPassengerCount)
    {
        if (humanIconList == null || humanIconList.Count == 0)
            return;

        int maxDisplay = humanIconList.Count;
        int displayCount = Mathf.Clamp(currentPassengerCount, 0, maxDisplay);

        for (int i = 0; i < humanIconList.Count; i++)
        {
            if (humanIconList[i] != null)
                humanIconList[i].gameObject.SetActive(i < displayCount);
        }

        Debug.Log($"[GamePanel] 乘客UI更新: {displayCount}/{maxDisplay}");
    }

    /// <summary>
    /// 更新电梯方向UI显示
    /// </summary>
    /// <param name="isGoingUp">true为上升，false为下降</param>
    public void UpdateDirectionUI(bool isGoingUp)
    {
        if (rawImgUpDownArrow == null || rawImgUpDownArrow.Length < 2)
            return;

        // 索引0为上箭头，索引1为下箭头
        if (rawImgUpDownArrow[0] != null)
            rawImgUpDownArrow[0].enabled = isGoingUp;

        if (rawImgUpDownArrow[1] != null)
            rawImgUpDownArrow[1].enabled = !isGoingUp;

        Debug.Log($"[GamePanel] 方向UI更新: {(isGoingUp ? "上升↑" : "下降↓")}");
    }

    /// <summary>
    /// 更新倒计时UI显示
    /// </summary>
    /// <param name="remainingSeconds">剩余秒数</param>
    public void UpdateCountdownUI(int remainingSeconds)
    {
        if (txtTimeInfo == null)
            return;

        if (remainingSeconds <= 0)
        {
            txtTimeInfo.text = "";
        }
        else
        {
            txtTimeInfo.text = remainingSeconds.ToString();
        }

        Debug.Log($"[GamePanel] 倒计时UI更新: {remainingSeconds}秒");
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
                break;
            case "BtnReturn":
                break;
            case "BtnTip":
                break;
        }
    }

    private void UpdateMaskUI(int maskID) { }

    public void ShowMirrorUI()
    {
        mirrorObj.gameObject.SetActive(true);
    }

    public void HideMirrorUI()
    {
        mirrorObj.gameObject.SetActive(false);
    }

    public void ShowRenderTextureUI(int time)
    {
        renderTextureObj.gameObject.SetActive(true);
        TimerMgr.Instance.CreateTimer(false, time, () =>
        {
            HideRenderTextureUI();
        });
    }

    private void HideRenderTextureUI()
    {
        renderTextureObj.gameObject.SetActive(false);
    }

    private void ExpelSelectedPassenger()
    {
        if (nowSelectedPassenger == null)
            return;

        PassengerMgr.Instance.OnPassengerKicked(nowSelectedPassenger);
        nowSelectedPassenger = null;
    }

    private void UpdateMirrorUI()
    {
        print("更新镜子UI");
    }

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
