using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;


/// <summary>
/// 电梯状态枚举
/// </summary>
public enum E_ElevatorState
{
    Moving,
    Arriving,
    Stopped,
    Departing
}

/// <summary>
/// 电梯管理器
/// </summary>
public class ElevatorMgr : BaseSingleton<ElevatorMgr>
{
    /// <summary>
    /// 当前电梯状态
    /// </summary>
    private E_ElevatorState currentElevatorState = E_ElevatorState.Stopped;

    private Text txtFloor;
    
    private int waveNum = 0;
    
    private int levelNum = 0;


    /// <summary>
    /// 开启电梯的方法 进入游戏时调用
    /// </summary>
    public void StartElevator()
    {
        EnterMovingState();
    }

    /// <summary>
    /// 电梯进入移动状态(电梯状态1) 
    /// </summary>
    public void EnterMovingState()
    {
        currentElevatorState = E_ElevatorState.Moving;
        Debug.Log("电梯正在运行...");

        TimerMgr.Instance.CreateTimer(false, Random.Range(30, 50) * 1000, () => 
        {
            EnterArrivingState();
        }, 100, () => 
        {
            //改变楼层显示
            txtFloor.text = Random.Range(2, 18).ToString();
        });
    }

    /// <summary>
    /// 电梯进入到到达状态(电梯状态2)
    /// </summary>
    public void EnterArrivingState()
    {
        currentElevatorState = E_ElevatorState.Arriving;

        var wave = ResourcesMgr.Instance.waveSOList[waveNum];
        if (levelNum >= wave.levelDetails.Count)
        {
            levelNum = 0; // 重置到当前 wave 的第一个 level
            waveNum++; // 切换到下一个 wave
            wave = ResourcesMgr.Instance.waveSOList[waveNum];
        }

        // 获取当前楼层
        int stopFloor = wave.levelDetails[levelNum].level;
        levelNum++; // 准备下次调用时切换到下一个 level

        // 改变楼层 UI 显示
        txtFloor.text = stopFloor.ToString();

        // 播放开门动画

        // 等待开门动画播放完（假设动画1秒）
        TimerMgr.Instance.CreateTimer(false, 1000, () =>
        {
            // 门开了，进入交互状态
            EnterStoppedState();
        });
    }


    /// <summary>
    /// 电梯进入停靠状态(电梯状态3)
    /// 此处实现乘客生成和玩家操作时间倒计时
    /// </summary>
    public void EnterStoppedState()
    {
        currentElevatorState = E_ElevatorState.Stopped;

        // 1. 生成乘客
        PassengerMgr.Instance.SpawnWave();

        // 2. 倒计时（玩家操作时间）
        TimerMgr.Instance.CreateTimer(false, GameLevelMgr.Instance.currentLevelDetail.dockingTime*1000, () =>
        {
            // 时间到，关门
            EnterDepartingState();
        }, 100, () =>
        {
            Debug.Log("电梯停靠中，玩家操作时间...");
        });
    }

    /// <summary>
    /// 电梯进入离开状态(电梯状态4)
    /// 此处实现结算逻辑和清理乘客
    /// </summary>
    public void EnterDepartingState()
    {
        currentElevatorState = E_ElevatorState.Departing;

        // 播放关门动画

        // 等待关门动画播放完（假设动画1秒）
        TimerMgr.Instance.CreateTimer(false, 1000, () => 
        {
            // 1. 结算逻辑
            CheckResults();

            // 2. 清理乘客 不确定是否每波都清理 这里先写死
            PassengerMgr.Instance.ClearAllPassengers();

            // 3. 增加难度并循环
            GameLevelMgr.Instance.AddWave();

            // 检查是否游戏结束，否则继续循环
            //TODO: 游戏结束判断

            EnterMovingState();
        });



    }

    /// <summary>
    /// 调用外部方法检查还剩多少鬼
    /// </summary>
    private void CheckResults()
    {
        // 调用外部方法检查还剩多少鬼
        Debug.Log("本轮结算完成");
    }

    private ElevatorMgr()
    {
        UIMgr.Instance.GetPanel<GamePanel>((panel) => 
        {
            txtFloor = panel.GetControl<Text>("TxtFloor");
        });
    }
}
