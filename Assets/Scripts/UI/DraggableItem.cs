using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖曳物品基类
/// </summary>
public abstract class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// 物品的RectTransform
    /// </summary>
    protected RectTransform rectTransform;

    /// <summary>
    /// 所属Canvas
    /// </summary>
    protected Canvas canvas;

    /// <summary>
    /// 原始位置
    /// </summary>
    protected Vector2 originalPosition;

    /// <summary>
    /// 是否正在拖曳
    /// </summary>
    protected bool isDragging = false;

    /// <summary>
    /// 拖曳计时器
    /// </summary>
    protected float dragTimer = 0f;

    /// <summary>
    /// 最大拖曳时间（秒）
    /// </summary>
    [SerializeField]
    protected float maxDragTime = 5f;

    /// <summary>
    /// 是否计时结束
    /// </summary>
    protected bool isTimerExpired = false;

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        originalPosition = rectTransform.anchoredPosition;
    }

    protected virtual void Update()
    {
        if (isDragging && !isTimerExpired)
        {
            dragTimer += Time.deltaTime;
            if (dragTimer >= maxDragTime)
            {
                isTimerExpired = true;
                OnTimerExpired();
            }
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        // 检查电梯状态
        if (!ElevatorMgr.Instance.CanUseMask)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragging = true;
        dragTimer = 0f;
        isTimerExpired = false;

        Debug.Log($"[{GetType().Name}] 开始拖曳");
        OnDragStart();
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isTimerExpired)
            return;

        // 跟随鼠标移动
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        Debug.Log($"[{GetType().Name}] 结束拖曳");
        OnDragEnd();
        ResetPosition();
    }

    /// <summary>
    /// 计时结束时调用
    /// </summary>
    protected virtual void OnTimerExpired()
    {
        Debug.Log($"[{GetType().Name}] 计时结束");
        OnDragEnd();
        ResetPosition();
        isDragging = false;
    }

    /// <summary>
    /// 回到原点
    /// </summary>
    protected virtual void ResetPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
        isDragging = false;
        isTimerExpired = false;
        dragTimer = 0f;
    }

    /// <summary>
    /// 拖曳开始时的处理（子类实现）
    /// </summary>
    protected abstract void OnDragStart();

    /// <summary>
    /// 拖曳结束时的处理（子类实现）
    /// </summary>
    protected abstract void OnDragEnd();
}