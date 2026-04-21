using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 登陆面板(已废弃)
/// </summary>
[Obsolete("LoginPanel is deprecated and will be removed in future versions.")]
public class LoginPanel : BasePanel
{
    public override void Init()
    {
        base.Init();
    }
    
    public override void ShowMe()
    {
        base.ShowMe();
        MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
    }
    
    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        if (name == "BtnPrompt")
        {
            UIMgr.Instance.HidePanel<LoginPanel>();
            UIMgr.Instance.ShowPanel<MainMenuPanel>();
        }
    }
}
