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
    /// <summary>
    /// 镜子图片
    /// </summary>
    private RawImage imgMirror;
    /// <summary>
    /// 镜子对象
    /// </summary>
    private Transform mirrorObj;
    /// <summary>
    /// 渲染隐藏层的RawImage
    /// </summary>
    private RawImage randerTexture;
    /// <summary>
    /// 渲染隐藏层的RenderTexture对象
    /// </summary>
    private Transform renderTextureObj;
    /// <summary>
    /// 乘客交互面板的RawImage
    /// </summary>
    private RawImage passengerPanel;
    /// <summary>
    /// 乘客交互面板对象
    /// </summary>
    private Transform passengerPanelObj;
    /// <summary>
    /// 当前选中的乘客
    /// </summary>
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
        //开启面具系统
        MaskMgr.Instance.Start();
        //注册更新面具UI事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
        //注册更新与面具相关的UI事件
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
        //注册显示乘客交互面板UI事件
        EventCenter.Instance.AddEventListener<Passenger>(E_EventType.E_PassengerUIAppear, ShowPassengerPanelUI);
        HideMirrorUI();
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
                MaskMgr.Instance.MaskEventHandler(GameDataMgr.Instance.PlayerInfo.nowMaskID);
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
}
