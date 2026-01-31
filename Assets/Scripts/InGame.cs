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

        // 初始化玩家面具数据
        InitPlayerMaskData();

        Debug.Log("[InGame] 游戏初始化完成");
    }

    /// <summary>
    /// 初始化玩家面具数据（确保有面具可用）
    /// </summary>
    private void InitPlayerMaskData()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[InGame] PlayerInfo 为空！");
            return;
        }

        // 确保面具列表存在
        if (playerInfo.gotMaskIDList == null)
            playerInfo.gotMaskIDList = new List<int>();

        // 添加默认面具 1, 2, 3
        if (!playerInfo.gotMaskIDList.Contains(1))
            playerInfo.gotMaskIDList.Add(1);
        if (!playerInfo.gotMaskIDList.Contains(2))
            playerInfo.gotMaskIDList.Add(2);
        if (!playerInfo.gotMaskIDList.Contains(3))
            playerInfo.gotMaskIDList.Add(3);

        // 默认装备普通面具
        if (playerInfo.nowMaskID == 0)
            playerInfo.nowMaskID = 1;

        // 确保所有面具可用状态
        var maskList = GameDataMgr.Instance.MaskInfoList;
        if (maskList != null)
        {
            foreach (var mask in maskList)
            {
                if (mask != null)
                    mask.canUseInElevator = true;
            }
        }

        Debug.Log($"[InGame] 玩家面具列表: [{string.Join(",", playerInfo.gotMaskIDList)}]");
    }

    void Update()
    {
        // 直接在这里检测面具按键输入（最可靠）
        DetectMaskInput();

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

    /// <summary>
    /// 检测面具切换按键
    /// </summary>
    private void DetectMaskInput()
    {
        // 检查电梯状态
        if (!ElevatorMgr.Instance.CanUseMask)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[InGame] 按下按键 1");
            MaskMgr.Instance.TryUseMask(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("[InGame] 按下按键 2");
            MaskMgr.Instance.TryUseMask(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("[InGame] 按下按键 3");
            MaskMgr.Instance.TryUseMask(3);
        }
    }
}
