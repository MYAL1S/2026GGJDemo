using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroPanel : BasePanel
{
    private Image imgIntro1;
    private Image imgIntro2;
    private Image imgIntro3;
    private Image nowIntro;

    public override void Init()
    {
        base.Init();
        imgIntro1 = GetControl<Image>("ImgIntro1");
        imgIntro2 = GetControl<Image>("ImgIntro2");
        imgIntro3 = GetControl<Image>("ImgIntro3");
        imgIntro2.gameObject.SetActive(false);
        imgIntro3.gameObject.SetActive(false);
        nowIntro = imgIntro1;
    }

    public override void ShowMe()
    {
        base.ShowMe();
    }

    /// <summary>
    /// 重置所有游戏数据
    /// </summary>
    private void ResetAllGameData()
    {
        // 重置玩家数据（信任度、灵能值）
        GameDataMgr.Instance.ResetGameData();
        
        // 重置关卡配额
        GameLevelMgr.Instance.ResetRuntimeCounters();
        
        // 清理乘客
        PassengerMgr.Instance.ClearAllPassengers();
        
        // 重置事件管理器
        EventMgr.Instance.ResetState();
        
        Debug.Log("[IntroPanel] 所有游戏数据已重置");
    }

    /// <summary>
    /// 用于切换介绍图片的状态
    /// </summary>
    private void ChangeState()
    {
        switch (nowIntro.name)
        {
            case "ImgIntro1":
                imgIntro2.gameObject.SetActive(true);
                nowIntro = imgIntro2;
                break;
            case "ImgIntro2":
                imgIntro3.gameObject.SetActive(true);
                nowIntro = imgIntro3;
                break;
            case "ImgIntro3":
                // 重置所有游戏数据
                ResetAllGameData();

                // ⭐ 清理音效列表
                MusicMgr.Instance.ClearSound();

                // 清理所有面板
                UIMgr.Instance.ClearAllPanels();

                SceneMgr.Instance.LoadSceneAsync("GameScene", () =>
                {
                    // 显示游戏界面
                    UIMgr.Instance.ShowPanel<UIBackgroundPanel>(E_UILayer.Bottom, (bgPanel) =>
                    {
                        UIMgr.Instance.ShowPanel<GamePanel>(E_UILayer.Middle, (gamePanel) =>
                        {
                            ElevatorMgr.Instance.StartElevator();
                        });
                    });
                });
                break;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (Input.GetMouseButtonDown(0))
        {
            ChangeState();
        }
    }
}
