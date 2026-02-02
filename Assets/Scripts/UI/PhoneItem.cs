using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhoneItem : DraggableItem
{
    [SerializeField] private ItemConfigSO itemConfig;
    [SerializeField] private GameObject phoneScreenObj;

    private int psychicCost = 1;
    private bool isDragStarted = false;
    private RectTransform phoneScreenRect;
    private RectTransform ghostLayer;
    private Dictionary<Passenger, Image> ghostImageCache = new Dictionary<Passenger, Image>();
    private RectTransform canvasRect;
    
    // ⭐ 鬼魂检测相关
    private Passenger lastDetectedGhost = null;
    private const string GhostAppearSoundPath = "Music/26GGJsound/ghost_appear";

    public void SetRenderTextureRoot(GameObject root)
    {
        phoneScreenObj = root;
        if (root != null)
            phoneScreenRect = root.GetComponent<RectTransform>();
    }

    public void SetItemConfig(ItemConfigSO config)
    {
        itemConfig = config;
        if (config != null)
        {
            maxDragTime = config.phoneMaxDragTime;
            psychicCost = config.phonePsychicCost;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        
        if (itemConfig != null)
        {
            maxDragTime = itemConfig.phoneMaxDragTime;
            psychicCost = itemConfig.phonePsychicCost;
        }

        if (parentCanvas != null)
            canvasRect = parentCanvas.GetComponent<RectTransform>();

        if (phoneScreenObj == null)
            phoneScreenObj = transform.Find("PhoneScreen")?.gameObject;

        if (phoneScreenObj != null)
        {
            phoneScreenRect = phoneScreenObj.GetComponent<RectTransform>();
            SetupPhoneScreen();
        }
    }

    private void SetupPhoneScreen()
    {
        if (phoneScreenObj == null) return;

        Image screenImage = phoneScreenObj.GetComponent<Image>();
        if (screenImage == null)
            screenImage = phoneScreenObj.AddComponent<Image>();

        Mask mask = phoneScreenObj.GetComponent<Mask>();
        if (mask == null)
            mask = phoneScreenObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        SetupGhostLayer();
        phoneScreenObj.SetActive(false);
    }

    private void SetupGhostLayer()
    {
        if (phoneScreenObj == null) return;

        Transform existingLayer = phoneScreenObj.transform.Find("GhostLayer");
        if (existingLayer != null)
        {
            ghostLayer = existingLayer.GetComponent<RectTransform>();
            return;
        }

        GameObject layerObj = new GameObject("GhostLayer");
        ghostLayer = layerObj.AddComponent<RectTransform>();
        ghostLayer.SetParent(phoneScreenObj.transform, false);

        ghostLayer.anchorMin = new Vector2(0.5f, 0.5f);
        ghostLayer.anchorMax = new Vector2(0.5f, 0.5f);
        ghostLayer.pivot = new Vector2(0.5f, 0.5f);
        ghostLayer.anchoredPosition = Vector2.zero;
        ghostLayer.sizeDelta = Vector2.zero;
        ghostLayer.localScale = Vector3.one;
    }

    protected override void OnDragStart()
    {
        isDragStarted = false;
        lastDetectedGhost = null;  // ⭐ 重置检测

        if (!ElevatorMgr.Instance.CanUseMask)
        {
            Debug.LogWarning($"[PhoneItem] 当前状态无法使用透视: {ElevatorMgr.Instance.CurrentState}");
            ResetPosition();
            return;
        }

        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            Debug.LogWarning("[PhoneItem] 灵能不足");
            ResetPosition();
            return;
        }

        isDragStarted = true;

        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(true);

        CreateGhostImages();
        UpdateGhostImagePositions();
        Debug.Log("[PhoneItem] 开始透视");
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!isDragStarted) return;

        base.OnDrag(eventData);
        UpdateGhostImagePositions();
        
        // ⭐ 检测手机屏幕是否覆盖到鬼魂
        CheckGhostUnderPhone();
    }

    /// <summary>
    /// ⭐ 检测手机屏幕下方是否有鬼魂
    /// </summary>
    private void CheckGhostUnderPhone()
    {
        if (phoneScreenRect == null) return;

        Passenger detectedGhost = null;
        
        var passengers = PassengerMgr.Instance.passengerList;
        if (passengers != null)
        {
            foreach (var passenger in passengers)
            {
                if (passenger == null || passenger.passengerInfo == null) continue;
                if (!passenger.passengerInfo.isGhost) continue;
                if (!passenger.gameObject.activeSelf) continue;
                if (passenger.mainRender == null) continue;

                // 检测手机屏幕是否与乘客重叠
                if (IsOverlapping(phoneScreenRect, passenger.mainRender.rectTransform))
                {
                    detectedGhost = passenger;
                    break;
                }
            }
        }

        // ⭐ 如果检测到新的鬼魂（之前没有检测到或检测到的是不同的鬼魂）
        if (detectedGhost != null && detectedGhost != lastDetectedGhost)
        {
            MusicMgr.Instance.PlaySound(GhostAppearSoundPath, false);
            Debug.Log($"[PhoneItem] 检测到鬼魂: {detectedGhost.passengerInfo.passengerName}");
        }

        lastDetectedGhost = detectedGhost;
    }

    /// <summary>
    /// ⭐ 检测两个 RectTransform 是否重叠
    /// </summary>
    private bool IsOverlapping(RectTransform rect1, RectTransform rect2)
    {
        Vector3[] corners1 = new Vector3[4];
        Vector3[] corners2 = new Vector3[4];
        rect1.GetWorldCorners(corners1);
        rect2.GetWorldCorners(corners2);

        float minX1 = Mathf.Min(corners1[0].x, corners1[2].x);
        float maxX1 = Mathf.Max(corners1[0].x, corners1[2].x);
        float minY1 = Mathf.Min(corners1[0].y, corners1[2].y);
        float maxY1 = Mathf.Max(corners1[0].y, corners1[2].y);

        float minX2 = Mathf.Min(corners2[0].x, corners2[2].x);
        float maxX2 = Mathf.Max(corners2[0].x, corners2[2].x);
        float minY2 = Mathf.Min(corners2[0].y, corners2[2].y);
        float maxY2 = Mathf.Max(corners2[0].y, corners2[2].y);

        return !(maxX1 < minX2 || minX1 > maxX2 || maxY1 < minY2 || minY1 > maxY2);
    }

    /// <summary>
    /// 创建所有乘客的鬼魂层图像
    /// </summary>
    private void CreateGhostImages()
    {
        if (ghostLayer == null) return;

        ClearGhostImages();

        var passengers = PassengerMgr.Instance.passengerList;
        if (passengers == null) return;

        foreach (var passenger in passengers)
        {
            if (passenger == null || passenger.passengerInfo == null) continue;
            if (passenger.mainRender == null) continue;

            RectTransform mainRenderRect = passenger.mainRender.rectTransform;

            GameObject ghostObj = new GameObject($"Ghost_{passenger.passengerInfo.passengerName}");
            RectTransform ghostRect = ghostObj.AddComponent<RectTransform>();
            ghostRect.SetParent(ghostLayer, false);

            Image ghostImage = ghostObj.AddComponent<Image>();
            ghostImage.sprite = passenger.passengerInfo.isGhost
                ? passenger.passengerInfo.ghostSprite
                : passenger.passengerInfo.normalSprite;
            ghostImage.raycastTarget = false;

            ghostRect.pivot = mainRenderRect.pivot;
            ghostRect.sizeDelta = mainRenderRect.sizeDelta;
            ghostRect.localScale = Vector3.one;

            ghostImageCache[passenger] = ghostImage;
        }
    }

    /// <summary>
    /// 更新所有鬼图像位置
    /// </summary>
    private void UpdateGhostImagePositions()
    {
        if (ghostLayer == null) return;

        foreach (var kvp in ghostImageCache)
        {
            Passenger passenger = kvp.Key;
            Image ghostImage = kvp.Value;

            if (passenger == null || passenger.mainRender == null || ghostImage == null) continue;

            RectTransform ghostRect = ghostImage.rectTransform;
            RectTransform mainRenderRect = passenger.mainRender.rectTransform;

            Vector3 worldPos = mainRenderRect.position;
            Vector3 localPos = ghostLayer.InverseTransformPoint(worldPos);
            ghostRect.localPosition = localPos;

            ghostRect.sizeDelta = mainRenderRect.sizeDelta;

            Vector3 mainWorldScale = mainRenderRect.lossyScale;
            Vector3 ghostLayerWorldScale = ghostLayer.lossyScale;
            
            if (ghostLayerWorldScale.x != 0 && ghostLayerWorldScale.y != 0)
            {
                ghostRect.localScale = new Vector3(
                    mainWorldScale.x / ghostLayerWorldScale.x,
                    mainWorldScale.y / ghostLayerWorldScale.y,
                    1f
                );
            }
        }
    }

    private void ClearGhostImages()
    {
        foreach (var kvp in ghostImageCache)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        ghostImageCache.Clear();
    }

    protected override void OnDragEnd()
    {
        if (!isDragStarted) return;

        ClearGhostImages();
        lastDetectedGhost = null;  // ⭐ 重置检测

        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(false);

        Debug.Log("[PhoneItem] 停止透视");
        isDragStarted = false;
    }

    private void OnDestroy()
    {
        ClearGhostImages();
    }
}