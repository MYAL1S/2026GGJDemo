using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData_",menuName = "Scriptable Objects/WaveDetail")]
public class WaveDetailSO : ScriptableObject
{
    [Tooltip("当前波关联的层级信息列表(这一波需要停靠在哪几层)")]
    public List<LevelDetailSO> levelDetails;
    [Tooltip("在哪一层触发铜镜事件(根据在当前波关联的层级信息列表中的索引，注意不要越界，从0开始)")]
    public List<int> createMirrorLevelIndex;
}
