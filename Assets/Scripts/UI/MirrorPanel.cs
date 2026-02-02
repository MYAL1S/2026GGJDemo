using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 铜镜面板
/// </summary>
public class MirrorPanel : BasePanel
{
    private Image imgProgressFront;
    private RectTransform progressFrontRect;
    private float originalProgressWidth;

    private bool isGazing = false;
    private float gazeTime = 0f;

    private const float TotalGazeDuration = 10f;
    private const float PsychicRestoreInterval = 3f;
    private float lastRestoreTime = 0f;

    private static bool isShowing = false;
    public static bool IsShowing => isShowing;

    private const int PANEL_SORTING_ORDER = 200;
    private const int MASK_SORTING_ORDER = 150;

    public override void Init()
    {
        base.Init();

        imgProgressFront = GetControl<Image>("ImgProgressFront");
        if (imgProgressFront != null)
        {
            progressFrontRect = imgProgressFront.GetComponent<RectTransform>();
            originalProgressWidth = progressFrontRect.sizeDelta.x;
        }

        SetupBlockingMask();
        SetupCanvasSorting();

        ResetGazeState();
    }

    // ? 重写 ShowMe，调用父类方法并设置状态
    public override void ShowMe()
    {
        base.ShowMe();
        isShowing = true;
        ResetGazeState();
    }

    private void SetupCanvasSorting()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = PANEL_SORTING_ORDER;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupBlockingMask()
    {
        Transform existingMask = transform.Find("BlockingMask");
        if (existingMask != null)
        {
            Canvas existingCanvas = existingMask.GetComponent<Canvas>();
            if (existingCanvas == null)
            {
                existingCanvas = existingMask.gameObject.AddComponent<Canvas>();
                existingCanvas.overrideSorting = true;
                existingCanvas.sortingOrder = MASK_SORTING_ORDER;
                existingMask.gameObject.AddComponent<GraphicRaycaster>();
            }
            return;
        }

        GameObject maskObj = new GameObject("BlockingMask");
        maskObj.transform.SetParent(transform, false);
        maskObj.transform.SetAsFirstSibling();

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        Canvas maskCanvas = maskObj.AddComponent<Canvas>();
        maskCanvas.overrideSorting = true;
        maskCanvas.sortingOrder = MASK_SORTING_ORDER;
        maskObj.AddComponent<GraphicRaycaster>();

        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = new Color(0, 0, 0, 0.3f);
        maskImage.raycastTarget = true;

        Button maskButton = maskObj.AddComponent<Button>();
        maskButton.transition = Selectable.Transition.None;
        maskButton.onClick.AddListener(OnMaskClicked);
    }

    private void OnMaskClicked()
    {
        StopGazing();
        CloseMirrorPanel();
    }

    // ? 重写 Update，必须调用 base.Update() 来处理淡入淡出
    protected override void Update()
    {
        base.Update();  // ? 关键：调用父类的 Update 处理淡入淡出

        if (!isGazing) return;

        gazeTime += Time.deltaTime;

        UpdateProgressBar();

        if (gazeTime - lastRestoreTime >= PsychicRestoreInterval)
        {
            RestorePsychicPower();
            lastRestoreTime = gazeTime;
        }

        if (gazeTime >= TotalGazeDuration)
        {
            OnGazeComplete();
        }
    }

    /// <summary>
    /// 更新进度条宽度（从0开始填充）
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressFrontRect == null) return;

        // ? 计算填充比例（注视越久，进度条越满）
        float fillRatio = gazeTime / TotalGazeDuration;
        fillRatio = Mathf.Clamp01(fillRatio);

        Vector2 sizeDelta = progressFrontRect.sizeDelta;
        sizeDelta.x = originalProgressWidth * fillRatio;
        progressFrontRect.sizeDelta = sizeDelta;
    }

    private void RestorePsychicPower()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null) return;

        int restoreAmount = (int)(playerInfo.maxPsychicPowerValue * 0.3f);
        GameDataMgr.Instance.AddPsychicPower(restoreAmount);
        Debug.Log($"[MirrorPanel] 恢复了 {restoreAmount} 点灵能");
    }

    private void OnGazeComplete()
    {
        Debug.Log("[MirrorPanel] 注视完成");
        StopGazing();
        CloseMirrorPanel();
    }

    public void StartGazing()
    {
        if (isGazing) return;

        isGazing = true;
        gazeTime = 0f;
        lastRestoreTime = 0f;

        // ? 开始时进度条宽度为0
        if (progressFrontRect != null)
        {
            Vector2 sizeDelta = progressFrontRect.sizeDelta;
            sizeDelta.x = 0f;
            progressFrontRect.sizeDelta = sizeDelta;
        }

        PassengerMgr.Instance.GhostApproaching();

        UIMgr.Instance.ShowPanel<FogPanel>(E_UILayer.Top, panel =>
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            MonoMgr.Instance.StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, 1.5f));
        });

        Debug.Log("[MirrorPanel] 开始注视铜镜");
    }

    public void StopGazing()
    {
        if (!isGazing) return;

        isGazing = false;

        PassengerMgr.Instance.StopGhostApproaching();

        UIMgr.Instance.GetPanel<FogPanel>(panel =>
        {
            if (panel != null)
            {
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    MonoMgr.Instance.StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 0f, 1f, () =>
                    {
                        UIMgr.Instance.HidePanel<FogPanel>();
                    }));
                }
                else
                {
                    UIMgr.Instance.HidePanel<FogPanel>();
                }
            }
        });

        Debug.Log($"[MirrorPanel] 停止注视，共注视了 {gazeTime:F1} 秒");
    }

    /// <summary>
    /// 重置注视状态（进度条归零）
    /// </summary>
    private void ResetGazeState()
    {
        isGazing = false;
        gazeTime = 0f;
        lastRestoreTime = 0f;

        // ? 重置时进度条宽度为0
        if (progressFrontRect != null)
        {
            Vector2 sizeDelta = progressFrontRect.sizeDelta;
            sizeDelta.x = 0f;
            progressFrontRect.sizeDelta = sizeDelta;
        }
    }

    private void CloseMirrorPanel()
    {
        UIMgr.Instance.HidePanel<MirrorPanel>(true);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, System.Action onComplete = null)
    {
        if (cg == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnGaze":
                StartGazing();
                break;
            case "BtnLeave":
                StopGazing();
                CloseMirrorPanel();
                break;
        }
    }

    public override void HideMe()
    {
        StopGazing();
        isShowing = false;
        base.HideMe();
    }
}