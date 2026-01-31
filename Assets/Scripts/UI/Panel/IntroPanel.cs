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
            TimerMgr.Instance.CreateTimer(false, 3000, () => 
            {
                imgIntro1.gameObject.SetActive(false);
                if (imgIntro2 != null)
                    imgIntro2.gameObject.SetActive(true);
            });

        TimerMgr.Instance.CreateTimer(false, 10000, () => 
        {
            //如果在切换场景过程中有需要执行的函数
            //可以在事件中心中添加监听 如根据加载进度做处理
            
            //如果切换完场景有需要处理的逻辑 可以在下面方法中传入委托 场景加载完成后自动调用
            SceneMgr.Instance.LoadSceneAsync("GameScene", () =>
            {
                UIMgr.Instance.HidePanel<IntroPanel>();
                UIMgr.Instance.ShowPanel<UIBackgroundPanel>(E_UILayer.Bottom);
                UIMgr.Instance.ShowPanel<GamePanel>(E_UILayer.Middle, (gamePanel) =>
                {
                    ElevatorMgr.Instance.StartElevator();
                });
            });
        });
    }
}
