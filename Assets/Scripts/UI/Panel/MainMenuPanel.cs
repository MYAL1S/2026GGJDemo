using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板类，负责显示和管理游戏主菜单界面，允许玩家开始游戏、进入设置、查看制作人员名单或退出游戏
/// </summary>
public class MainMenuPanel : BasePanel
{
    public override void ShowMe()
    {
        base.ShowMe();
        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
    }

    protected override Selectable GetDefaultSelectable()
    {
        return GetControl<Button>("BtnGameStart");
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnGameStart":
                //隐藏主菜单面板
                UIMgr.Instance.HidePanel<MainMenuPanel>();
                //显示提示面板
                UIMgr.Instance.ShowPanel<IntroPanel>();
                break;
            case "BtnOptions":
                //隐藏主菜单面板
                UIMgr.Instance.HidePanel<MainMenuPanel>();
                //显示选项面板
                UIMgr.Instance.ShowPanel<OptionsPanel>();
                break;
            case "BtnCredits":
                //隐藏主菜单面板
                UIMgr.Instance.HidePanel<MainMenuPanel>();
                //显示制作人员名单面板
                UIMgr.Instance.ShowPanel<DeveloperPanel>();
                break;
            case "BtnExit":
                //退出游戏
                Application.Quit();
                break;
            default:
                break;
        }
    }
}
