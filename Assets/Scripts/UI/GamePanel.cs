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

        // 注册事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerUIAppear, ShowPassengerPanelUI);

        HideMirrorUI();

        // 初始化玩家面具数据
        InitPlayerMaskData();

        // 注册按键检测到 MonoMgr
        MonoMgr.Instance.AddUpdateListener(DetectMaskInput);

        Debug.Log("[GamePanel] 初始化完成");
    }

    /// <summary>
    /// 初始化玩家面具数据
    /// </summary>
    private void InitPlayerMaskData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[GamePanel] PlayerInfo 为空");
            return;
        }

        // 确保面具列表存在
        if (playerInfo.gotMaskIDList == null)
            playerInfo.gotMaskIDList = new List<int>();

        // 添加默认面具 1, 2, 3
        if (!playerInfo.gotMaskIDList.Contains(1))
            playerInfo.gotMaskIDList.Add(1);
        if (!playerInfo.gotMaskIDList.Contains(2))
            playerInfo.gotMaskIDList.Add(2);
        if (!playerInfo.gotMaskIDList.Contains(3))
            playerInfo.gotMaskIDList.Add(3);

        // 默认装备普通面具
        if (playerInfo.nowMaskID == 0)
            playerInfo.nowMaskID = 1;

        // 确保所有面具可用
        var maskList = GameDataMgr.Instance.MaskInfoList;
        if (maskList != null)
        {
            foreach (var mask in maskList)
            {
                if (mask != null)
                    mask.canUseInElevator = true;
            }
        }

        Debug.Log($"[GamePanel] 玩家面具: [{string.Join(",", playerInfo.gotMaskIDList)}]");
    }

    /// <summary>
    /// 检测面具切换按键（每帧调用）
    /// </summary>
    private void DetectMaskInput()
    {
        // 电梯不可用时不检测
        if (!ElevatorMgr.Instance.CanUseMask)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[GamePanel] 按下按键 1");
            MaskMgr.Instance.TryUseMask(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[GamePanel] 按下按键 2");
            MaskMgr.Instance.TryUseMask(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("[GamePanel] 按下按键 3");
            MaskMgr.Instance.TryUseMask(3);
        }
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
            case "BtnMask":
                // 点击按钮使用当前面具
                MaskMgr.Instance.TryUseMask(GameDataMgr.Instance.PlayerInfo.nowMaskID);
                break;
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
        MonoMgr.Instance.RemoveUpdateListener(DetectMaskInput);
    }
}
