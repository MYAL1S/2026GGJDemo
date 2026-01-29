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
    public PassengerSO passengerInfo;


    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;
        // 初始化乘客信息
        mainRender.sprite = passengerSO.normalSprite;
        if (passengerSO.isGhost)
        {
            ghostFeatureRenderer.sprite = passengerSO.ghostSprite;
            ghostFeatureRenderer.gameObject.SetActive(true);
        }
        else
            ghostFeatureRenderer.gameObject.SetActive(false);
    }

    // 模拟被面具“照妖”时的反应（供MaskSystem调用）
    public void OnMaskReveal(bool maskActive)
    {
        if (passengerInfo.isGhost)
        {
            ghostFeatureRenderer.gameObject.SetActive(maskActive);
        }
    }

    // 点击交互（驱逐逻辑）
    void OnMouseDown()
    {
        // 通知管理器处理结果
        //PassengerMgr.Instance.OnPassengerKicked(this);
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
