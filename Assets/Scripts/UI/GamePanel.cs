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

    public override void Init()
    {
        base.Init();
        txtMask = GetControl<Text>("TxtMask");
        //开启面具系统
        MaskMgr.Instance.Start();
        //注册更新面具UI事件
        EventCenter.Instance.AddEventListener<int>(E_EventType.E_UpdateMaskUI, UpdateMaskUI);
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
}
