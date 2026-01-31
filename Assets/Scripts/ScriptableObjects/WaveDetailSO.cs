using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData_",menuName = "Scriptable Objects/WaveDetail")]
public class WaveDetailSO : ScriptableObject
{
    [Tooltip("这一波中存储的关卡列表")]
    public List<LevelDetailSO> levelDetails;
    [Tooltip("这一波中的第几个关卡需要触发铜镜事件")]
    public List<int> createMirrorLevelIndex;
}
