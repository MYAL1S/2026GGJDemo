using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
