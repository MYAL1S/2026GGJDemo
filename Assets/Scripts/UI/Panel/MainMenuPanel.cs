using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : BasePanel
{
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
