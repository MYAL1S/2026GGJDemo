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
    public override void Init()
    {
        base.Init();
        txtMask = GetControl<Text>("TxtMask");
        imgMirror = GetControl<RawImage>("Mirror");
        mirrorObj = imgMirror.GetComponent<Transform>();
        randerTexture = GetControl<RawImage>("RenderTexture");
        renderTextureObj = randerTexture.GetComponent<Transform>();
        //开启面具系统
        MaskMgr.Instance.Start();
        //注册更新面具UI事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
        //注册更新与面具相关的UI事件
        EventCenter.Instance.AddEventListener(E_EventType.E_MirrorUIUpdate, UpdateMirrorUI);
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
            case "BtnWatch":
                //触发观看铜镜事件
                EventMgr.Instance.StartWatchMirror();
                break;
            case "BtnCancel":
                //取消观看铜镜事件
                EventMgr.Instance.StopWatchMirror();
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
    /// 更新与面具相关的UI显示
    /// </summary>
    private void UpdateMirrorUI()
    {
        
    }
}
