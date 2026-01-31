using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// 乘客逻辑脚本
/// 挂载在乘客预制体上
/// </summary>
public class Passenger : MonoBehaviour
{
    /// <summary>
    /// 用于显示乘客本体的SpriteRenderer
    /// </summary>
    public SpriteRenderer mainRender;
    /// <summary>
    /// 用于显示鬼魂特征的SpriteRenderer
    /// </summary>
    public SpriteRenderer ghostFeatureRenderer;
    /// <summary>
    /// 该乘客的配置数据
    /// </summary>
    [HideInInspector]
    public PassengerSO passengerInfo;

    /// <summary>
    /// 初始化乘客信息
    /// </summary>
    /// <param name="passengerSO">乘客配置ScriptableObject</param>
    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;
        // 初始化乘客外观
        mainRender.sprite = passengerSO.normalSprite;
        mainRender.sortingOrder = passengerSO.oderInLayer;
        ghostFeatureRenderer.sprite = passengerSO.ghostSprite;
        ghostFeatureRenderer.gameObject.SetActive(true);
        ghostFeatureRenderer.sortingOrder = passengerSO.oderInLayer + 1;
    }

    // 模拟被面具“揭露”时的响应（MaskSystem使用）
    public void OnMaskReveal(bool maskActive)
    {
        if (passengerInfo.isGhost)
        {
            ghostFeatureRenderer.gameObject.SetActive(maskActive);
        }
    }

    /// <summary>
    /// 乘客被点击时
    /// </summary>
    public void OnMouseDown()
    {
        // 非停靠状态禁止交互，防止在电梯运行时仍能点击
        if (!ElevatorMgr.Instance.CanInteractPassengers)
            return;

        // 分发乘客点击事件
        EventCenter.Instance.EventTrigger<Passenger>(E_EventType.E_PassengerClicked, this);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
