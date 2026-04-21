using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 铜镜交互面板。
/// 
/// 主要职责：
/// 1) 管理“注视铜镜”的完整流程（开始/中断/完成）。
/// 2) 根据注视时长推进进度条，并按间隔恢复玩家灵能。
/// 3) 在注视期间按概率触发“鬼杀死玩家”事件（含3秒警告缓冲）。
/// 4) 控制与该流程关联的视觉表现（遮罩层、镜面高亮、雾面板淡入淡出）。
/// 
/// 生命周期关键点：
/// - Init: 绑定控件并初始化遮罩和排序。
/// - ShowMe: 面板显示时重置状态。
/// - Update: 仅在注视中驱动计时、恢复、死亡判定。
/// - HideMe: 做统一收尾，确保状态与计时器被清理。
/// </summary>
public class MirrorPanel : BasePanel
{
    // 进度条前景图与其Rect（通过宽度变化模拟填充）
    private Image imgProgressFront;
    private RectTransform progressFrontRect;
    private float originalProgressWidth;

    // 注视状态与累计注视时长（秒）
    private bool isGazing = false;
    private float gazeTime = 0f;

    // 注视总时长阈值：达到后视为本次注视完成
    private const float TotalGazeDuration = 10f;
    // 灵能恢复间隔：每隔3秒恢复一次
    private const float PsychicRestoreInterval = 3f;
    private float lastRestoreTime = 0f;
    
    // 鬼杀死玩家判定相关（按间隔抽样，而非每帧抽样）
    private float lastDeathCheckTime = 0f;
    private bool hasTriggeredDeath = false;

    // 全局可见状态，供外部系统快速查询镜面面板是否展示中
    private static bool isShowing = false;
    public static bool IsShowing => isShowing;

    // UI层级：面板高于遮罩，遮罩高于普通UI
    private const int PANEL_SORTING_ORDER = 200;
    private const int MASK_SORTING_ORDER = 150;

    // 镜面“危险警告”阶段状态
    private bool isWarningActive = false;
    private int warningTimerId = 0;
    // 玩家在警告阶段成功离开时的回调
    private System.Action warningSafeExit;
    // 玩家在警告阶段超时未离开时的回调
    private System.Action warningFail;
    // 主镜面显示控件，用于警告时变红高亮
    private RawImage imgMirror;

    public override void Init()
    {
        base.Init();

        imgProgressFront = GetControl<Image>("ImgProgressFront");
        if (imgProgressFront != null)
        {
            progressFrontRect = imgProgressFront.GetComponent<RectTransform>();
            originalProgressWidth = progressFrontRect.sizeDelta.x;
        }

        // 获取主镜面图片控件（用于危险警告视觉反馈）
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
        // 保证该面板独立排序，不受父Canvas排序影响
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
        // 若已存在遮罩则复用，避免重复创建
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
        // 遮罩放在最底层子节点，避免覆盖本面板内部按钮渲染顺序
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
        // 点击遮罩 = 退出铜镜流程
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

        // 非注视状态或已触发死亡后，不再推进逻辑
        if (!isGazing || hasTriggeredDeath) return;

        gazeTime += Time.deltaTime;

        UpdateProgressBar();
        
        // 检查“鬼杀”概率事件
        CheckGhostKill();

        if (gazeTime - lastRestoreTime >= PsychicRestoreInterval)
        {
            RestorePsychicPower();
            lastRestoreTime = gazeTime;
        }

        if (gazeTime >= TotalGazeDuration)
        {
            // 达到注视上限，正常完成本次注视
            OnGazeComplete();
        }
    }

    /// <summary>
    /// 按配置间隔执行一次死亡判定。
    /// 判定命中后先进入3秒警告阶段，玩家若未在警告期退出才会真正死亡。
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
    /// 真正执行“鬼杀”后果。
    /// 执行顺序：锁定死亡状态 -> 停止注视 -> 关闭面板 -> 停止电梯 -> 触发失败事件。
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
    /// 更新进度条（从0线性增长到原始宽度）。
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

    /// <summary>
    /// 按玩家最大灵能的30%恢复当前灵能。
    /// 每次恢复由Update内的间隔判断驱动。
    /// </summary>
    private void RestorePsychicPower()
    {
        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null) return;

        int restoreAmount = (int)(playerInfo.maxPsychicPowerValue * 0.3f);
        GameDataMgr.Instance.AddPsychicPower(restoreAmount);
        Debug.Log($"[MirrorPanel] 恢复了 {restoreAmount} 灵能");
    }

    /// <summary>
    /// 注视流程自然完成后的收尾。
    /// </summary>
    private void OnGazeComplete()
    {
        Debug.Log("[MirrorPanel] 注视完成");
        StopGazing();
        CloseMirrorPanel();
    }

    /// <summary>
    /// 开始注视：重置本轮计时，启动鬼靠近演出，显示并淡入雾面板。
    /// </summary>
    public void StartGazing()
    {
        if (isGazing) return;

        isGazing = true;
        gazeTime = 0f;
        lastRestoreTime = 0f;
        lastDeathCheckTime = 0f;
        hasTriggeredDeath = false;

        if (progressFrontRect != null)
        {
            Vector2 sizeDelta = progressFrontRect.sizeDelta;
            sizeDelta.x = 0f;
            progressFrontRect.sizeDelta = sizeDelta;
        }

        // 触发鬼靠近行为（用于制造压迫感）
        PassengerMgr.Instance.GhostApproaching();

        UIMgr.Instance.ShowPanel<FogPanel>(E_UILayer.Top, panel =>
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            // 雾层从透明到可见，强化危险氛围
            MonoMgr.Instance.StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, 1.5f));
        });

        Debug.Log("[MirrorPanel] 开始注视铜镜");
    }

    /// <summary>
    /// 停止注视：停止鬼靠近演出，并将雾面板平滑淡出后关闭。
    /// </summary>
    public void StopGazing()
    {
        if (!isGazing) return;

        isGazing = false;

        // 撤销鬼靠近状态
        PassengerMgr.Instance.StopGhostApproaching();

        UIMgr.Instance.GetPanel<FogPanel>(panel =>
        {
            if (panel != null)
            {
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    // 雾层淡出结束后再隐藏，避免视觉突变
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
    /// 重置注视状态（用于Show或初始化收尾）。
    /// 该方法只重置本轮数据，不负责关闭面板或停止外部演出。
    /// </summary>
    private void ResetGazeState()
    {
        isGazing = false;
        gazeTime = 0f;
        lastRestoreTime = 0f;
        lastDeathCheckTime = 0f;
        hasTriggeredDeath = false;

        if (progressFrontRect != null)
        {
            Vector2 sizeDelta = progressFrontRect.sizeDelta;
            sizeDelta.x = 0f;
            progressFrontRect.sizeDelta = sizeDelta;
        }
    }

    /// <summary>
    /// 进入镜面警告阶段：高亮镜面并开启3秒倒计时。
    /// - 倒计时结束仍未退出：触发warningFail。
    /// - 倒计时内退出：由OnExitMirror触发warningSafeExit。
    /// </summary>
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

    /// <summary>
    /// 玩家离开铜镜时调用。
    /// 若当前处于警告阶段，则清理计时器并执行“安全退出”回调。
    /// </summary>
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

    /// <summary>
    /// 设置镜面警告高亮（红/白）。
    /// </summary>
    private void SetWarningHighlight(bool active)
    {
        if (imgMirror != null)
            imgMirror.color = active ? Color.red : Color.white;
    }

    /// <summary>
    /// 关闭铜镜面板前的统一出口：先处理退出逻辑，再隐藏面板。
    /// </summary>
    private void CloseMirrorPanel()
    {
        OnExitMirror();
        UIMgr.Instance.HidePanel<MirrorPanel>(true);
    }

    /// <summary>
    /// CanvasGroup透明度补间。
    /// 额外做了对象销毁判空，避免异步淡出过程中因面板销毁导致异常。
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, System.Action onComplete = null)
    {
        if (cg == null || cg.gameObject == null) yield break;

        cg.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 每帧检查对象是否仍有效，防止协程访问失效引用
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
            // 开始注视按钮
            case "BtnGaze":
                StartGazing();
                break;
            // 离开按钮
            case "BtnLeave":
                StopGazing();
                CloseMirrorPanel();
                break;
        }
    }

    public override void HideMe()
    {
        // Hide路径上的统一收尾：停止注视、关闭警告、更新展示标记
        StopGazing();
        isShowing = false;
        OnExitMirror();
        base.HideMe();
    }
}