using UnityEngine;
using UnityEngine.EventSystems;

public class PhoneItem : DraggableItem
{
    [SerializeField] private ItemConfigSO itemConfig;
    [SerializeField] private GameObject phoneScreenObj;

    private int psychicCost = 1;
    private bool isDragStarted = false;

    public void SetRenderTextureRoot(GameObject root) => phoneScreenObj = root;

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
    }

    protected override void OnDragStart()
    {
        isDragStarted = false;

        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            ResetPosition();
            return;
        }

        isDragStarted = true;

        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(true);

        // 显示所有鬼魂特征
        GhostRenderSystem.Instance.StartGhostRendering();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
    }

    protected override void OnDragEnd()
    {
        if (!isDragStarted) return;

        if (phoneScreenObj != null)
            phoneScreenObj.SetActive(false);

        // 隐藏所有鬼魂特征
        GhostRenderSystem.Instance.StopGhostRendering();

        isDragStarted = false;
    }
}