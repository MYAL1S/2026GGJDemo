using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 玩家对象类
/// 挂载在玩家预制体上
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    public PlayerInfo playerInfo;
    /// <summary>
    ///最大移动速度
    ///序列化私有字段的目的是 不希望其他类直接访问这些变量 但又希望能够在Inspector窗口中进行调整和设置
    /// </summary>
    [Header("MovementParameters")]
    [SerializeField] private float moveSpeed = 5f;
    /// <summary>
    /// 加速率
    /// </summary>
    [SerializeField] private float acceleration = 20f;
    /// <summary>
    /// 减速率
    /// </summary>
    [SerializeField] private float deceleration = 30f;


    /// <summary>
    ///Rigidbody2D 组件 用于物理移动
    /// </summary>
    private Rigidbody2D rigidbody2D;
    /// <summary>
    /// 来自输入事件的方向
    /// </summary>
    private Vector2 moveInput;
    /// <summary>
    /// 期望的速度
    /// </summary>
    private Vector2 targetVel;
    /// <summary>
    /// 移动方向
    /// </summary>
    private Vector2 moveDir;
    /// <summary>
    /// 速度变化率
    /// </summary>
    private float rate;

    /// <summary>
    /// 初始化玩家数据
    /// </summary>
    /// <param name="info">玩家信息</param>
    public void Init(PlayerInfo info)
    {
        playerInfo = info;
        rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // 订阅输入轴事件（由 InputMgr 每帧触发）
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontal);
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Vertical, OnVertical);
    }

    private void OnDisable()
    {
        // 取消订阅输入轴事件
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontal);
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Vertical, OnVertical);
    }

    /// <summary>
    /// 水平轴输入事件处理
    /// </summary>
    /// <param name="value">水平轴输入数值</param>
    private void OnHorizontal(float value)
    {
        moveInput.x = value;
    }

    /// <summary>
    /// 竖直轴输入事件处理
    /// </summary>
    /// <param name="value">竖直轴输入数值</param>
    private void OnVertical(float value)
    {
        moveInput.y = value;
    }

    /// <summary>
    /// 移动事件处理 在fixedupdate中调用
    /// </summary>
    private void MoveEvent()
    {
        // 归一化以避免斜方向超速
        moveDir = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
        targetVel = moveDir * moveSpeed;

        // 根据是否有输入选择加速/减速率
        // 如果没有输入 则进行减速
        rate = moveDir.sqrMagnitude > 0.0001f ? acceleration : deceleration;

        // 速度平滑过渡
        rigidbody2D.velocity = Vector2.MoveTowards(rigidbody2D.velocity, targetVel, rate * Time.fixedDeltaTime);
    }

    private void FixedUpdate()
    {
        MoveEvent();
    }
}
