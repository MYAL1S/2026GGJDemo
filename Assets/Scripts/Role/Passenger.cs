using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 乘客逻辑脚本
/// 挂载于乘客预制体上
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
    /// 该乘客的数据配置
    /// </summary>
    [HideInInspector]
    public PassengerSO passengerInfo;

    /// <summary>
    /// 高亮颜色
    /// </summary>
    [SerializeField]
    private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    /// <summary>
    /// 原始颜色
    /// </summary>
    private Color originalColor = Color.white;

    /// <summary>
    /// 是否处于高亮状态
    /// </summary>
    private bool isHighlighted = false;

    /// <summary>
    /// 初始化乘客信息
    /// </summary>
    /// <param name="passengerSO">乘客数据ScriptableObject</param>
    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;
        // 初始化乘客外观
        mainRender.sprite = passengerSO.normalSprite;
        mainRender.sortingOrder = passengerSO.oderInLayer;
        ghostFeatureRenderer.sprite = passengerSO.ghostSprite;
        ghostFeatureRenderer.gameObject.SetActive(true);
        ghostFeatureRenderer.sortingOrder = passengerSO.oderInLayer + 1;

        // 保存原始颜色
        originalColor = mainRender.color;
    }

    /// <summary>
    /// 设置高亮状态
    /// </summary>
    /// <param name="highlight">是否高亮</param>
    public void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight)
            return;

        isHighlighted = highlight;

        if (highlight)
        {
            // 高亮显示
            mainRender.color = highlightColor;
            if (ghostFeatureRenderer != null && ghostFeatureRenderer.gameObject.activeSelf)
                ghostFeatureRenderer.color = highlightColor;
        }
        else
        {
            // 恢复原始颜色
            mainRender.color = originalColor;
            if (ghostFeatureRenderer != null)
                ghostFeatureRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// 模拟被揭露/暴露时（响应MaskSystem使用）
    /// </summary>
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
        // 停靠状态才允许点击，防止在电梯运行时能点击
        if (!ElevatorMgr.Instance.CanInteractPassengers)
            return;

        // 分发乘客点击事件
        EventCenter.Instance.EventTrigger<Passenger>(E_EventType.E_PassengerClicked, this);
    }

    void Start()
    {
    }

    void Update()
    {
    }

    /// <summary>
    /// 对象销毁时确保取消高亮
    /// </summary>
    private void OnDestroy()
    {
        isHighlighted = false;
    }
}
