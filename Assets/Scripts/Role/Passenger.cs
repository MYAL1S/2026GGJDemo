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
    }

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
        isHighlighted = false;
    }
}
