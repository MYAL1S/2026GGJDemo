using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGame : MonoBehaviour
{
    void Start()
    {
        UIMgr.Instance.ShowPanel<UIBackgroundPanel>(E_UILayer.Bottom);
        UIMgr.Instance.ShowPanel<GamePanel>();

        // 启动电梯
        ElevatorMgr.Instance.StartElevator();

        Debug.Log("[InGame] 游戏初始化完成");
    }


    void Update()
    {
        // 其他测试按键
        if (Input.GetKeyDown(KeyCode.S))
        {
            UIMgr.Instance.GetPanel<GamePanel>((gamePanel) =>
            {
                gamePanel.ShowMirrorUI();
            });
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            UIMgr.Instance.GetPanel<GamePanel>((gamePanel) =>
            {
                gamePanel.HideMirrorUI();
            });
        }
    }
}
