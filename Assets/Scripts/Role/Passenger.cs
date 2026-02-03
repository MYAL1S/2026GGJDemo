using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;

public class Passenger : MonoBehaviour, IPointerClickHandler
{
    public Image mainRender;
    public Image ghostFeatureRenderer;

    [HideInInspector]
    public PassengerSO passengerInfo;

    [SerializeField]
    private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    private Color originalColor = Color.white;
    private bool isHighlighted = false;
    private Canvas canvas;

    /// <summary>
    /// 控制乘客UI透明度
    /// </summary>
    private CanvasGroup canvasGroup;

    private const int BASE_SORTING_ORDER = 5;
    private const int MAX_SORTING_ORDER = 90;

    #region 特殊乘客灵能恢复

    private bool isRestoringPsychic = false;
    private float psychicRestoreTimer = 0f;
    
    [SerializeField]
    private float psychicRestoreInterval = 2f;
    
    [SerializeField]
    private float maxRestoreDuration = 10f;
    
    private float totalRestoreTime = 0f;
    
    [SerializeField]
    private int psychicRestoreAmount = 1;
    
    private bool hasLostSpecialAbility = false;

    #endregion

    /// <summary>
    /// 是否为本轮新进入的乘客（本轮结算跳过）
    /// </summary>
    public bool isNewThisRound = false;

    /// <summary>
    /// 是否已经结算过伤害（鬼魂只结算一次）
    /// </summary>
    public bool hasDamageSettled = false;

    /// <summary>
    /// ⭐ 乘客所在的槽位索引
    /// </summary>
    public int SlotIndex { get; set; } = -1;

    /// <summary>
    /// ⭐ 设置位置和缩放
    /// </summary>
    public void SetPositionAndScale(Vector2 position, Vector3 scale)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
            rect.localScale = scale;
        }
    }

    /// <summary>
    /// ⭐ 平滑过渡到目标位置和缩放（可选）
    /// </summary>
    public void TransitionToPositionAndScale(Vector2 targetPos, Vector3 targetScale, float duration = 0.3f)
    {
        StartCoroutine(TransitionCoroutine(targetPos, targetScale, duration));
    }

    private IEnumerator TransitionCoroutine(Vector2 targetPos, Vector3 targetScale, float duration)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) yield break;
        
        Vector2 startPos = rect.anchoredPosition;
        Vector3 startScale = rect.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            rect.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            yield return null;
        }
        
        rect.anchoredPosition = targetPos;
        rect.localScale = targetScale;
    }

    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;

        mainRender.sprite = passengerSO.normalSprite;
        mainRender.SetNativeSize();

        // 设置阈值为 0.1，意味着透明度低于 10% 的区域将不再响应点击，而是穿透过去
        mainRender.alphaHitTestMinimumThreshold = 0.1f;

        if (ghostFeatureRenderer != null)
        {

            ghostFeatureRenderer.sprite = passengerSO.isGhost
                ? passengerSO.ghostSprite
                : passengerSO.normalSprite;
            ghostFeatureRenderer.SetNativeSize();
            ghostFeatureRenderer.gameObject.SetActive(false);
        }

        originalColor = mainRender.color;

        // ⭐ 2. 初始化 CanvasGroup 并设为透明
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Min(BASE_SORTING_ORDER + passengerSO.oderInLayer, MAX_SORTING_ORDER);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // 初始设为 0 (完全透明)，等待调用 FadeIn
        canvasGroup.alpha = 0f;

        // 初始化特殊乘客灵能恢复
        InitSpecialPassengerAbility();
    }

    // ⭐ 3. 新增：淡入方法
    public void FadeIn(float duration = 0.5f)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        // 确保物体是激活的
        gameObject.SetActive(true);
        StartCoroutine(FadeRoutine(0f, 1f, duration, null));
    }


    // ⭐ 4. 新增：淡出方法
    public void FadeOut(float duration = 0.5f, Action onComplete = null)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // 禁用交互，防止淡出过程中被再次点击
        canvasGroup.blocksRaycasts = false;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, duration, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    // ⭐ 5. 新增：淡入淡出协程
    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        float timer = 0f;
        if (canvasGroup != null) canvasGroup.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = endAlpha;

        onComplete?.Invoke();
    }

    private void InitSpecialPassengerAbility()
    {
        if (passengerInfo != null && passengerInfo.isSpecialPassenger && !hasLostSpecialAbility)
        {
            StartPsychicRestore();
        }
    }

    /// <summary>
    /// 开始恢复灵能
    /// </summary>
    public void StartPsychicRestore()
    {
        if (hasLostSpecialAbility) return;
        if (passengerInfo == null || !passengerInfo.isSpecialPassenger) return;

        isRestoringPsychic = true;
        psychicRestoreTimer = 0f;
        totalRestoreTime = 0f;
        Debug.Log($"[Passenger] {passengerInfo.passengerName} 开始为玩家恢复灵能");
    }

    /// <summary>
    /// 停止恢复灵能
    /// </summary>
    public void StopPsychicRestore()
    {
        if (!isRestoringPsychic) return;

        isRestoringPsychic = false;
        Debug.Log($"[Passenger] {passengerInfo.passengerName} 停止恢复灵能，已恢复 {totalRestoreTime:F1} 秒");
    }

    private void Update()
    {
        if (!isRestoringPsychic || hasLostSpecialAbility) return;

        totalRestoreTime += Time.deltaTime;
        psychicRestoreTimer += Time.deltaTime;

        // 每隔一段时间恢复一次灵能
        if (psychicRestoreTimer >= psychicRestoreInterval)
        {
            psychicRestoreTimer = 0f;
            GameDataMgr.Instance.AddPsychicPower(psychicRestoreAmount);
            Debug.Log($"[Passenger] {passengerInfo.passengerName} 恢复了 {psychicRestoreAmount} 点灵能");
        }

        // 达到最大恢复时间，失去特殊能力
        if (totalRestoreTime >= maxRestoreDuration)
        {
            LoseSpecialAbility();
        }
    }

    private void LoseSpecialAbility()
    {
        if (hasLostSpecialAbility) return;

        hasLostSpecialAbility = true;
        isRestoringPsychic = false;

        Debug.Log($"[Passenger] {passengerInfo.passengerName} 已失去特殊能力，共恢复了 {totalRestoreTime:F1} 秒");
        EventCenter.Instance.EventTrigger(E_EventType.E_SpecialPassengerExpired);
    }

    public bool HasSpecialAbility => passengerInfo != null 
                                     && passengerInfo.isSpecialPassenger 
                                     && !hasLostSpecialAbility;

    public void SetSortingOrder(int order)
    {
        if (canvas != null)
            canvas.sortingOrder = Mathf.Min(BASE_SORTING_ORDER + order, MAX_SORTING_ORDER);
    }

    public void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return;
        isHighlighted = highlight;
        mainRender.color = highlight ? highlightColor : originalColor;
    }

    public void SetGhostFeatureVisible(bool visible)
    {
        if (ghostFeatureRenderer != null)
            ghostFeatureRenderer.gameObject.SetActive(visible);
    }

    public void OnMaskReveal(bool maskActive)
    {
        SetGhostFeatureVisible(maskActive);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnMouseDown();
    }

    public void OnMouseDown()
    {
        if (PassengerPanel.IsShowing) return;
        if (!ElevatorMgr.Instance.CanInteractPassengers) return;
        EventCenter.Instance.EventTrigger<Passenger>(E_EventType.E_PassengerClicked, this);
    }

    /// <summary>
    /// 标记为本轮新进入
    /// </summary>
    public void MarkAsNewThisRound()
    {
        isNewThisRound = true;
    }

    /// <summary>
    /// 清除本轮新进入标记（进入下一轮时调用）
    /// </summary>
    public void ClearNewThisRoundMark()
    {
        isNewThisRound = false;
    }

    /// <summary>
    /// 标记已结算伤害
    /// </summary>
    public void MarkDamageSettled()
    {
        hasDamageSettled = true;
    }

    private void OnDestroy()
    {
        StopPsychicRestore();
        isHighlighted = false;
    }
}
