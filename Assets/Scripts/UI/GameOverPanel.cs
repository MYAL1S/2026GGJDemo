using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : BasePanel
{
    private Text txtResult;
    public override void Init()
    {
        base.Init();
        txtResult = GetControl<Text>("TxtResult");
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                Time.timeScale = 1;
                UIMgr.Instance.SetAllPanelsActive(false);
                SceneMgr.Instance.LoadSceneAsync("BeginScene");
                break;
        }
    }

    /// <summary>
    /// ÏÔÊ¾½á¹û
    /// </summary>
    /// <param name="isWin"></param>
    public void ShowResult(bool isWin)
    {
        txtResult.text = isWin ? "You Win!" : "Game Over!";
    }
}
