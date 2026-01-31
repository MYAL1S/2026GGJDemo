using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 手机物品 - 拖曳时显示隐藏层
/// </summary>
public class PhoneItem : DraggableItem
{
    /// <summary>
    /// 物品配置
    /// </summary>
    [SerializeField]
    private ItemConfigSO itemConfig;

    /// <summary>
    /// 渲染隐藏层的根对象
    /// </summary>
    [SerializeField]
    private GameObject renderTextureRoot;

    /// <summary>
    /// 灵能消耗值
    /// </summary>
    private int psychicCost = 1;

    /// <summary>
    /// 设置渲染隐藏层根对象的引用
    /// </summary>
    public void SetRenderTextureRoot(GameObject root)
    {
        renderTextureRoot = root;
    }

    /// <summary>
    /// 设置物品配置
    /// </summary>
    public void SetItemConfig(ItemConfigSO config)
    {
        itemConfig = config;
        if (itemConfig != null)
        {
            maxDragTime = itemConfig.phoneMaxDragTime;
            psychicCost = itemConfig.phonePsychicCost;
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
        // 使用 GameDataMgr 的方法消耗灵能值（会自动触发UI更新事件）
        if (!GameDataMgr.Instance.ConsumePsychicPower(psychicCost))
        {
            Debug.Log("[PhoneItem] 灵能值不足");
            ResetPosition();
            return;
        }

        // 激活渲染隐藏层UI
        if (renderTextureRoot != null)
        {
            renderTextureRoot.SetActive(true);
            Debug.Log("[PhoneItem] 激活隐藏层UI");
        }
    }

    protected override void OnDragEnd()
    {
        // 失活渲染隐藏层UI
        if (renderTextureRoot != null)
        {
            renderTextureRoot.SetActive(false);
            Debug.Log("[PhoneItem] 失活隐藏层UI");
        }
    }
}