using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    public void Init(PassengerSO passengerSO)
    {
        passengerInfo = passengerSO;

        mainRender.sprite = passengerSO.normalSprite;
        mainRender.SetNativeSize();

        if (ghostFeatureRenderer != null)
        {
            ghostFeatureRenderer.sprite = passengerSO.isGhost
                ? passengerSO.ghostSprite
                : passengerSO.normalSprite;
            ghostFeatureRenderer.SetNativeSize();
            ghostFeatureRenderer.gameObject.SetActive(false);
        }

        originalColor = mainRender.color;

        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = Mathf.Min(BASE_SORTING_ORDER + passengerSO.oderInLayer, MAX_SORTING_ORDER);

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        // 初始化特殊乘客灵能恢复
        InitSpecialPassengerAbility();
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

    private void OnDestroy()
    {
        StopPsychicRestore();
        isHighlighted = false;
    }
}
