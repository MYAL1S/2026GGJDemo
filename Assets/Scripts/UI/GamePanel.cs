using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏界面面板
/// </summary>
public class GamePanel : BasePanel
{
    private Text txtMask;
    private RawImage imgMirror;
    private Transform mirrorObj;
    private RawImage randerTexture;
    private Transform renderTextureObj;
    private RawImage passengerPanel;
    private Transform passengerPanelObj;
    private Passenger nowSelectedPassenger;

    /// <summary>
    /// 手机物品
    /// </summary>
    private PhoneItem phoneItem;

    /// <summary>
    /// 铃铛物品
    /// </summary>
    private BellItem bellItem;

    public override void Init()
    {
        base.Init();
        txtMask = GetControl<Text>("TxtMask");
        imgMirror = GetControl<RawImage>("Mirror");
        mirrorObj = imgMirror.GetComponent<Transform>();
        randerTexture = GetControl<RawImage>("RenderTexture");
        renderTextureObj = randerTexture.GetComponent<Transform>();
        passengerPanel = GetControl<RawImage>("PassengerPanel");
        passengerPanelObj = passengerPanel.GetComponent<Transform>();

        // 初始化物品系统
        InitItemSystem();

        // 注册事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerUIAppear, ShowPassengerPanelUI);

        HideMirrorUI();

        // 初始化玩家数据
        InitPlayerData();

        Debug.Log("[GamePanel] 初始化完成");
    }

    /// <summary>
    /// 初始化物品系统
    /// </summary>
    private void InitItemSystem()
    {
        // 加载物品配置
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
        else
        {
            Debug.LogWarning("[GamePanel] 未找到 PhoneItem 对象");
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
        else
        {
            Debug.LogWarning("[GamePanel] 未找到 BellItem 对象");
        }

        // 确保隐藏层UI初始状态为失活
        if (renderTextureObj != null)
            renderTextureObj.gameObject.SetActive(false);
    }

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    private void InitPlayerData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[GamePanel] PlayerInfo 为空");
            return;
        }

        // 确保有初始灵能值
        if (playerInfo.nowPsychicPowerValue <= 0)
            playerInfo.nowPsychicPowerValue = playerInfo.maxPsychicPowerValue;

        Debug.Log($"[GamePanel] 当前灵能值: {playerInfo.nowPsychicPowerValue}");
    }

    /// <summary>
    /// 通过按钮名称分发按钮点击事件
    /// </summary>
    /// <param name="name"></param>
    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnGaze":
                //触发观看铜镜事件
                EventMgr.Instance.StartWatchMirror();
                break;
            case "BtnLeave":
                //关闭铜镜面板
                HideMirrorUI();
                //取消观看铜镜事件
                EventMgr.Instance.StopWatchMirror();
                break;
            case "BtnExpel":
                //驱逐乘客事件
                ExpelSelectedPassenger();
                break;
            case "BtnCancel":
                //关闭乘客交互面板UI
                HidePassengerPanelUI();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 更新面具UI显示
    /// </summary>
    /// <param name="maskID">面具id</param>
    private void UpdateMaskUI(int maskID)
    {
        switch (maskID)
        {
            case 0:
                txtMask.text = "无面具";
                break;
            case 1:
                txtMask.text = "普通面具";
                break;
            case 2:
                txtMask.text = "破幻面具";
                break;
            case 3:
                txtMask.text = "镇邪面具";
                break;
        }
        Debug.Log($"[GamePanel] 面具UI更新: {txtMask.text}");
    }

    /// <summary>
    /// 显示与面具相关的UI
    /// </summary>
    public void ShowMirrorUI()
    {
        mirrorObj.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏面具相关的UI
    /// </summary>
    public void HideMirrorUI()
    {
        mirrorObj.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示渲染隐藏层的UI
    /// </summary>
    public void ShowRenderTextureUI(int time)
    {
        renderTextureObj.gameObject.SetActive(true);
        TimerMgr.Instance.CreateTimer(false, time, () =>
        {
            HideRenderTextureUI();
        });
    }

    /// <summary>
    /// 隐藏渲染隐藏层的UI
    /// </summary>
    private void HideRenderTextureUI()
    {
        renderTextureObj.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示乘客交互面板UI
    /// </summary>
    private void ShowPassengerPanelUI(Passenger passenger)
    {
        passengerPanelObj.gameObject.SetActive(true);
        nowSelectedPassenger = passenger;
    }

    /// <summary>
    /// 隐藏乘客交互面板UI
    /// </summary>
    private void HidePassengerPanelUI()
    {
        passengerPanelObj.gameObject.SetActive(false);
    }

    /// <summary>
    /// 驱逐当前选中的乘客
    /// </summary>
    private void ExpelSelectedPassenger()
    {
        if (nowSelectedPassenger == null)
            return;

        PassengerMgr.Instance.OnPassengerKicked(nowSelectedPassenger);
        nowSelectedPassenger = null;
        HidePassengerPanelUI();
    }

    /// <summary>
    /// 更新与镜子相关的UI显示
    /// </summary>
    private void UpdateMirrorUI()
    {
        print("更新镜子UI");
    }

    /// <summary>
    /// 面板隐藏时移除按键检测
    /// </summary>
    public override void HideMe()
    {
        base.HideMe();
    }
}
