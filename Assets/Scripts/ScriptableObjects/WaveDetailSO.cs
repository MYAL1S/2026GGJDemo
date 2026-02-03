using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MirrorEventConfig
{
    public int levelIndex;      // 关卡列表中的索引（第几个楼层，从0开始）
    public float triggerDelay;  // 离开状态后多少秒触发
}

[CreateAssetMenu(fileName = "WaveData_",menuName = "Scriptable Objects/WaveDetail")]
public class WaveDetailSO : ScriptableObject
{
    [Tooltip("这一波中存储的关卡列表")]
    public List<LevelDetailSO> levelDetails;
    [Tooltip("这一波中的第几个关卡需要触发铜镜事件（已废弃，建议只用mirrorEvents）")]
    public List<int> createMirrorLevelIndex;
    public List<MirrorEventConfig> mirrorEvents;
    [Header("特殊乘客配置")]
    [Tooltip("本波次中，第几个关卡生成特殊乘客（索引从0开始）。例如填0代表本波第1关生成。填-1代表本波不生成。")]
    public int specialPassengerSpawnIndex = -1;
}
