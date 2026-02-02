using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroPanel : BasePanel
{
    private Image imgIntro1;
    private Image imgIntro2;

    public override void Init()
    {
        base.Init();
        imgIntro1 = GetControl<Image>("ImgIntro1");
        imgIntro2 = GetControl<Image>("ImgIntro2");
    }

    public override void ShowMe()
    {
        base.ShowMe();

        if (imgIntro2 != null)
             imgIntro2.gameObject.SetActive(false);

        if (imgIntro1 != null)
            TimerMgr.Instance.CreateTimer(false, 100, () => 
            {
                imgIntro1.gameObject.SetActive(false);
                if (imgIntro2 != null)
                    imgIntro2.gameObject.SetActive(true);
            });
        
        TimerMgr.Instance.CreateTimer(false, 100, () => 
        {
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
        });
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
}
