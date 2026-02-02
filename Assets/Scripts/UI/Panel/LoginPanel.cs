using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
