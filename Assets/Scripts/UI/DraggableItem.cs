using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可拖放物品基类
/// </summary>
public abstract class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>
    /// 物品的RectTransform
    /// </summary>
    protected RectTransform rectTransform;

    /// <summary>
    /// 父级Canvas
    /// </summary>
    protected Canvas parentCanvas;

    /// <summary>
    /// 物品自身的Canvas（用于调整层级）
    /// </summary>
    protected Canvas itemCanvas;

    /// <summary>
    /// 原始位置
    /// </summary>
    protected Vector2 originalPosition;

    /// <summary>
    /// 是否正在拖放
    /// </summary>
    protected bool isDragging = false;

    /// <summary>
    /// 拖放计时器
    /// </summary>
    protected float dragTimer = 0f;

    /// <summary>
    /// 最大拖放时间（秒）
    /// </summary>
    [SerializeField]
    protected float maxDragTime = 5f;

    /// <summary>
    /// 是否超时过期
    /// </summary>
    protected bool isTimerExpired = false;

    /// <summary>
    /// 物品 UI 排序层级（高于乘客）
    /// </summary>
    protected const int ITEM_SORTING_ORDER = 100;
    
    // ⭐ 拖拽偏移量（用于更精确的位置计算）
    protected Vector2 dragOffset;

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        originalPosition = rectTransform.anchoredPosition;

        // 设置物品自身的 Canvas 排序层级
        SetupCanvasSorting();
    }

    /// <summary>
    /// 设置 Canvas 排序层级，确保显示在乘客上方
    /// </summary>
    protected virtual void SetupCanvasSorting()
    {
        itemCanvas = GetComponent<Canvas>();
        if (itemCanvas == null)
            itemCanvas = gameObject.AddComponent<Canvas>();
        itemCanvas.overrideSorting = true;
        itemCanvas.sortingOrder = ITEM_SORTING_ORDER;

        // 确保有 GraphicRaycaster 用于点击
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
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
        // 非拖拽状态
        if (!ElevatorMgr.Instance.CanUseMask)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragging = true;
        dragTimer = 0f;
        isTimerExpired = false;

        // ⭐ 计算初始拖拽偏移量
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out localPoint
        );
        dragOffset = rectTransform.anchoredPosition - localPoint;

        Debug.Log($"[{GetType().Name}] 开始拖放");
        OnDragStart();
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (isTimerExpired)
            return;

        // ⭐ 使用绝对位置而不是增量，避免快速移动时丢失精度
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        Debug.Log($"[{GetType().Name}] 结束拖放");
        OnDragEnd();
        ResetPosition();
    }

    /// <summary>
    /// 重置到原始位置
    /// </summary>
    protected virtual void ResetPosition()
    {
        rectTransform.anchoredPosition = originalPosition;
    }

    /// <summary>
    /// 超时回调
    /// </summary>
    protected virtual void OnTimerExpired()
    {
        Debug.Log($"[{GetType().Name}] 拖放超时");
        OnDragEnd();
        ResetPosition();
    }

    /// <summary>
    /// 拖放开始时调用
    /// </summary>
    protected abstract void OnDragStart();

    /// <summary>
    /// 拖放结束时调用
    /// </summary>
    protected abstract void OnDragEnd();
}