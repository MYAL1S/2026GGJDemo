using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// 乘客对象类
/// 挂载在乘客预制体上
/// </summary>
public class Passenger : MonoBehaviour
{
    /// <summary>
    /// 用于显示乘客主体的SpriteRenderer
    /// </summary>
    public SpriteRenderer mainRender;
    /// <summary>
    /// 用于显示鬼魂特征的SpriteRenderer
    /// </summary>
    public SpriteRenderer ghostFeatureRenderer;
    /// <summary>
    /// 关联的乘客信息数据
    /// </summary>
    [HideInInspector]
    public PassengerSO passengerInfo;

    /// <summary>
    /// 初始化乘客信息
    /// </summary>
    /// <param name="passengerSO">含有乘客信息的可脚本化对象</param>
    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;
        // 初始化乘客信息
        mainRender.sprite = passengerSO.normalSprite;
        mainRender.sortingOrder = passengerSO.oderInLayer;
        ghostFeatureRenderer.sprite = passengerSO.ghostSprite;
        ghostFeatureRenderer.gameObject.SetActive(true);
        ghostFeatureRenderer.sortingOrder = passengerSO.oderInLayer + 1;

    }

    // 模拟被面具“照妖”时的反应（供MaskSystem调用）
    public void OnMaskReveal(bool maskActive)
    {
        if (passengerInfo.isGhost)
        {
            ghostFeatureRenderer.gameObject.SetActive(maskActive);
        }
    }

    /// <summary>
    /// 当乘客被点击时触发
    /// </summary>
    public void OnMouseDown()
    {
        // 发布乘客被点击事件
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
