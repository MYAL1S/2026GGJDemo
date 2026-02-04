using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : BasePanel
{
    private Text txtResult;
    private CanvasGroup panelCanvasGroup;
    private float alphaSpeed = 3f;
    private bool isPanelShowing = false;

    /// <summary>
    /// GameOverPanel 排序层级（最高，覆盖所有UI）
    /// </summary>
    private const int PANEL_SORTING_ORDER = 500;
    private const int MASK_SORTING_ORDER = 450;

    public override void Init()
    {
        base.Init();
        txtResult = GetControl<Text>("TxtResult");

        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetupCanvasSorting();
        SetupBlockingMask();
    }

    /// <summary>
    /// 设置 Canvas 排序层级（最高）
    /// </summary>
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

    /// <summary>
    /// 设置全屏遮罩
    /// </summary>
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
        maskImage.color = new Color(0, 0, 0, 0.7f);  // 较深的遮罩
        maskImage.raycastTarget = true;
    }

    public override void ShowMe()
    {
        isPanelShowing = true;
        panelCanvasGroup.alpha = 0;

        // ? 暂停游戏
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 显示结果
    /// </summary>
    public void ShowResult(bool isWin)
    {
        if (txtResult != null)
            txtResult.text = isWin ? "恭喜你驱逐了电梯中所有的鬼，并将乘客安全送到了一楼" : "很遗憾，因为你错误的决断，你坠入了异界";
    }

    protected override void Update()
    {
        // 淡入（使用 unscaledDeltaTime 因为游戏暂停）
        if (isPanelShowing && panelCanvasGroup.alpha < 1)
        {
            panelCanvasGroup.alpha += alphaSpeed * Time.unscaledDeltaTime;
            if (panelCanvasGroup.alpha >= 1)
                panelCanvasGroup.alpha = 1;
        }
    }

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                Time.timeScale = 1f;
                MusicMgr.Instance.StopBKMusic();
                MusicMgr.Instance.ClearSound();  // ⭐ 清理音效

                // ⭐⭐⭐【必须添加以下重置代码】⭐⭐⭐
                // 如果不加这些，上一局的异常状态和计时器会带入下一局，导致卡死

                // 1. 停止电梯（清理电梯内部计时器和状态）
                ElevatorMgr.Instance.StopElevator();

                // 2. 重置事件管理器（清除异常状态标记，这是最关键的！）
                EventMgr.Instance.ResetState();

                // 3. 清理乘客
                PassengerMgr.Instance.ClearAllPassengers();

                // 4. 重置游戏数据
                GameDataMgr.Instance.ResetGameData();
                GameLevelMgr.Instance.ResetRuntimeCounters();

                // ⭐⭐⭐【结束添加】⭐⭐⭐

                UIMgr.Instance.ClearAllPanels();
                SceneMgr.Instance.LoadSceneAsync("BeginScene", () =>
                {
                    UIMgr.Instance.ShowPanel<MainMenuPanel>();
                    MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
                });
                break;
            case "BtnRestart":
                Time.timeScale = 1f;
                MusicMgr.Instance.StopBKMusic();
                MusicMgr.Instance.ClearSound();  // ⭐ 清理音效
                UIMgr.Instance.ClearAllPanels();
                SceneMgr.Instance.LoadSceneAsync("GameScene", () =>
                {
                    // 重置所有游戏数据
                    GameDataMgr.Instance.ResetGameData();
                    GameLevelMgr.Instance.ResetRuntimeCounters();
                    PassengerMgr.Instance.ClearAllPassengers();
                    EventMgr.Instance.ResetState();
                    
                    // 显示游戏界面
                    UIMgr.Instance.ShowPanel<UIBackgroundPanel>(E_UILayer.Bottom, (bgPanel) =>
                    {
                        UIMgr.Instance.ShowPanel<GamePanel>(E_UILayer.Middle, (gamePanel) =>
                        {
                            ElevatorMgr.Instance.StartElevator();
                        });
                    });
                });
                break;
        }
    }

    public override void HideMe()
    {
        isPanelShowing = false;
        Time.timeScale = 1f;
        base.HideMe();
    }
}
