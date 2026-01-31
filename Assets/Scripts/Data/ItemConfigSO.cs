using UnityEngine;

/// <summary>
/// 物品配置 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "ItemConfig", menuName = "GameConfig/ItemConfig")]
public class ItemConfigSO : ScriptableObject
{
    [Header("手机配置")]
    [Tooltip("使用手机消耗的灵能值")]
    public int phonePsychicCost = 1;
    [Tooltip("手机最大使用时间（秒）")]
    public float phoneMaxDragTime = 5f;

    [Header("铃铛配置")]
    [Tooltip("使用铃铛消耗的灵能值")]
    public int bellPsychicCost = 1;
    [Tooltip("铃铛检测乘客的半径")]
    public float bellDetectionRadius = 50f;
    [Tooltip("铃铛最大使用时间（秒）")]
    public float bellMaxDragTime = 5f;

    [Header("铃铛晃动配置")]
    [Tooltip("解决异常事件需要的晃动次数")]
    public int requiredShakeCount = 5;
    [Tooltip("每次晃动的最小移动距离")]
    public float shakeThreshold = 30f;
    [Tooltip("方向变化的时间窗口（秒）")]
    public float shakeTimeWindow = 0.3f;
}