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
    
    // ⭐ 鬼杀死玩家相关
    private float lastDeathCheckTime = 0f;
    private bool hasTriggeredDeath = false;

    private static bool isShowing = false;
    public static bool IsShowing => isShowing;

    private const int PANEL_SORTING_ORDER = 200;
    private const int MASK_SORTING_ORDER = 150;

    private bool isWarningActive = false;
    private int warningTimerId = 0;
    private System.Action warningSafeExit;
    private System.Action warningFail;
    private RawImage imgMirror; // 假设主镜面图片控件名为 ImgMirror

    public override void Init()
    {
        base.Init();

        imgProgressFront = GetControl<Image>("ImgProgressFront");
        if (imgProgressFront != null)
        {
            progressFrontRect = imgProgressFront.GetComponent<RectTransform>();
            originalProgressWidth = progressFrontRect.sizeDelta.x;
        }

        // ⭐ 获取主镜面图片控件（如控件名不同请修改）
        imgMirror = GetControl<RawImage>("Mirror");

        SetupBlockingMask();
        SetupCanvasSorting();

        ResetGazeState();
    }

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

    protected override void Update()
    {
        base.Update();

        if (!isGazing || hasTriggeredDeath) return;

        gazeTime += Time.deltaTime;

        UpdateProgressBar();
        
        // ⭐ 检查是否被鬼杀死
        CheckGhostKill();

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
    /// ⭐ 检查是否被鬼杀死
    /// </summary>
    private void CheckGhostKill()
    {
        if (hasTriggeredDeath || isWarningActive) return; // 警告期间不再触发

        float checkInterval = ResourcesMgr.Instance.mirrorDeathCheckInterval;

        if (gazeTime - lastDeathCheckTime >= checkInterval)
        {
            lastDeathCheckTime = gazeTime;

            float deathChance = ResourcesMgr.Instance.mirrorDeathChance;

            if (Random.value < deathChance)
            {
                // 先触发警告
                ShowMirrorWarning(
                    onSafeExit: () => {
                        // 玩家及时退出，无事发生
                        Debug.Log("[MirrorPanel] 铜镜警告期间安全退出，无事发生");
                    },
                    onFail: () => {
                        // 3秒未退出才真正死亡
                        OnGhostKill();
                    }
                );
            }
        }
    }

    /// <summary>
    /// ⭐ 被鬼杀死
    /// </summary>
    private void OnGhostKill()
    {
        hasTriggeredDeath = true;
        
        Debug.Log("[MirrorPanel] 💀 注视铜镜时被鬼杀死！");
        
        // 停止注视
        StopGazing();
        
        // 关闭铜镜面板
        CloseMirrorPanel();
        
        // 停止电梯
        ElevatorMgr.Instance.StopElevator();
        
        // 触发游戏失败
        EventMgr.Instance.FallIntoAbyss();
    }

    /// <summary>
    /// 更新进度条（从0开始填充）
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressFrontRect == null) return;

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
        Debug.Log($"[MirrorPanel] 恢复了 {restoreAmount} 灵能");
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
        lastDeathCheckTime = 0f;  // ⭐ 重置死亡检查时间
        hasTriggeredDeath = false; // ⭐ 重置死亡标记

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

        Debug.Log($"[MirrorPanel] 停止注视，已注视了 {gazeTime:F1} 秒");
    }

    /// <summary>
    /// 重置注视状态（进度条归零）
    /// </summary>
    private void ResetGazeState()
    {
        isGazing = false;
        gazeTime = 0f;
        lastRestoreTime = 0f;
        lastDeathCheckTime = 0f;  // ⭐ 重置
        hasTriggeredDeath = false; // ⭐ 重置

        if (progressFrontRect != null)
        {
            Vector2 sizeDelta = progressFrontRect.sizeDelta;
            sizeDelta.x = 0f;
            progressFrontRect.sizeDelta = sizeDelta;
        }
    }

    // 警告高亮方法
    public void ShowMirrorWarning(System.Action onSafeExit, System.Action onFail)
    {
        if (isWarningActive) return;

        isWarningActive = true;
        warningSafeExit = onSafeExit;
        warningFail = onFail;

        SetWarningHighlight(true);

        // 启动3秒警告计时器
        warningTimerId = TimerMgr.Instance.CreateTimer(false, 3000, () =>
        {
            if (isWarningActive)
            {
                isWarningActive = false;
                SetWarningHighlight(false);
                warningFail?.Invoke();
            }
        });

        Debug.Log("[MirrorPanel] 铜镜高亮警告中，3秒内未退出将失败");
    }

    // 退出时调用
    public void OnExitMirror()
    {
        if (isWarningActive)
        {
            isWarningActive = false;
            SetWarningHighlight(false);
            if (warningTimerId != 0)
            {
                TimerMgr.Instance.RemoveTimer(warningTimerId);
                warningTimerId = 0;
            }
            warningSafeExit?.Invoke();
        }
    }

    // 设置高亮
    private void SetWarningHighlight(bool active)
    {
        if (imgMirror != null)
            imgMirror.color = active ? Color.red : Color.white;
    }

    private void CloseMirrorPanel()
    {
        OnExitMirror();
        UIMgr.Instance.HidePanel<MirrorPanel>(true);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, System.Action onComplete = null)
    {
        if (cg == null || cg.gameObject == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // ⭐ 每帧都检查 cg 是否已被销毁
            if (cg == null || cg.gameObject == null)
                yield break;

            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        if (cg != null && cg.gameObject != null)
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
        OnExitMirror();
        base.HideMe();
    }
}