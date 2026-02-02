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

        // ? 确保 GhostLayer 没有任何偏移和缩放
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

        if (!ElevatorMgr.Instance.CanUseMask)
        {
            Debug.LogWarning($"[PhoneItem] 当前状态无法使用透视: {ElevatorMgr.Instance.CurrentState}");
            ResetPosition();
            return;
        }

        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            Debug.LogWarning("[PhoneItem] 精神力不足");
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
    }

    /// <summary>
    /// 创建所有乘客的鬼魂特征图像
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

            // ? 复制 RectTransform 属性
            ghostRect.pivot = mainRenderRect.pivot;
            ghostRect.sizeDelta = mainRenderRect.sizeDelta;
            ghostRect.localScale = Vector3.one;

            ghostImageCache[passenger] = ghostImage;
        }
    }

    /// <summary>
    /// 更新所有鬼魂图像位置
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

            // ? 关键：将乘客的世界坐标转换为 ghostLayer 的本地坐标
            Vector3 worldPos = mainRenderRect.position;
            Vector3 localPos = ghostLayer.InverseTransformPoint(worldPos);
            ghostRect.localPosition = localPos;

            // 同步尺寸
            ghostRect.sizeDelta = mainRenderRect.sizeDelta;

            // ? 计算正确的缩放比例
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