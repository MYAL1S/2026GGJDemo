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
            maxDragTime = itemConfig.phoneMaxDragTime;
    }

    protected override void Awake()
    {
        base.Awake();
        // 从配置读取参数
        if (itemConfig != null)
            maxDragTime = itemConfig.phoneMaxDragTime;
    }

    protected override void OnDragStart()
    {
        int cost = itemConfig != null ? itemConfig.phonePsychicCost : 1;

        var playerInfo = GameDataMgr.Instance.PlayerInfo;
        if (playerInfo == null)
        {
            Debug.LogError("[PhoneItem] PlayerInfo 为空");
            return;
        }

        if (playerInfo.nowPsychicPowerValue < cost)
        {
            Debug.Log("[PhoneItem] 灵能值不足");
            ResetPosition();
            return;
        }

        playerInfo.nowPsychicPowerValue -= cost;
        Debug.Log($"[PhoneItem] 消耗灵能 {cost}，剩余 {playerInfo.nowPsychicPowerValue}");

        if (renderTextureRoot != null)
        {
            renderTextureRoot.SetActive(true);
            Debug.Log("[PhoneItem] 激活隐藏层UI");
        }
    }

    protected override void OnDragEnd()
    {
        if (renderTextureRoot != null)
        {
            renderTextureRoot.SetActive(false);
            Debug.Log("[PhoneItem] 失活隐藏层UI");
        }
    }
}