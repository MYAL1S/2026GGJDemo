using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏结束面板
/// </summary>
public class GameOverPanel : BasePanel
{
    /// <summary>
    /// 游戏结果文本组件
    /// </summary>
    private Text txtResult;

    /// <summary>
    /// GameOverPanel 排序层级（最高，覆盖所有UI）
    /// </summary>
    private const int PANEL_SORTING_ORDER = 500;
    /// <summary>
    /// 遮罩层排序层级（必须在 GameOverPanel 之下，覆盖所有游戏UI）
    /// </summary>
    private const int MASK_SORTING_ORDER = 450;

    public override void Init()
    {
        base.Init();
        txtResult = GetControl<Text>("TxtResult");

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
        base.ShowMe();
        //暂停游戏
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

    protected override void OnButtonClick(string name)
    {
        base.OnButtonClick(name);
        switch (name)
        {
            case "BtnReturn":
                Time.timeScale = 1f;
                MusicMgr.Instance.StopBKMusic();
                MusicMgr.Instance.ClearSound();  //清理音效

                // 1. 停止电梯（清理电梯内部计时器和状态）
                ElevatorMgr.Instance.StopElevator();

                // 2. 重置事件管理器
                EventMgr.Instance.ResetState();

                // 3. 清理乘客
                PassengerMgr.Instance.ClearAllPassengers();

                // 4. 重置游戏数据
                GameDataMgr.Instance.ResetGameData();
                GameLevelMgr.Instance.ResetRuntimeCounters();

                // 5. 清理所有UI面板
                UIMgr.Instance.ClearAllPanels();
                SceneMgr.Instance.LoadSceneAsync("BeginScene", () =>
                {
                    UIMgr.Instance.ShowPanel<MainMenuPanel>();
                    MusicMgr.Instance.PlayBKMuic("Music/26GGJsound/elevator_ambience_norml");
                });
                break;
        }
    }
}
