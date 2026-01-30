using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        MusicMgr.Instance.PlayBKMuic("Music/BKMusic/ÓæÖÛ³ªÍí");
    }
    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        if (name == "BtnPrompt")
        {
            //Òþ²ØµÇÂ¼Ãæ°å
            UIMgr.Instance.HidePanel<LoginPanel>();
            //ÏÔÊ¾Ö÷²Ëµ¥Ãæ°å
            UIMgr.Instance.ShowPanel<MainMenuPanel>();
        }
    }
}
