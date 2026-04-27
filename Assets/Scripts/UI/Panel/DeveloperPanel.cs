using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 开发者面板类 显示开发者信息
/// </summary>
public class DeveloperPanel : BasePanel
{
    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                UIMgr.Instance.HidePanel<DeveloperPanel>();
                UIMgr.Instance.ShowPanel<MainMenuPanel>();
                break;
            default:
                break;
        }
    }
}
