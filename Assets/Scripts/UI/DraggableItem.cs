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
    /// 物品自身的Canvas（用于排序）
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
    /// 是否超时结束
    /// </summary>
    protected bool isTimerExpired = false;

    /// <summary>
    /// 物品 UI 排序层级（高于乘客）
    /// </summary>
    protected const int ITEM_SORTING_ORDER = 100;

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

        // 确保有 GraphicRaycaster 用于点击检测
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
        // 非停靠状态
        if (!ElevatorMgr.Instance.CanUseMask)
        {
            eventData.pointerDrag = null;
            return;
        }

        isDragging = true;
        dragTimer = 0f;
        isTimerExpired = false;

        Debug.Log($"[{GetType().Name}] 开始拖放");
        OnDragStart();
    }

    // 确保 OnDrag 方法是 virtual 的（如果还不是的话）
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (isTimerExpired)
            return;

        // 跟随鼠标移动
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
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