using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class MirrorInfo
{
    /// <summary>
    /// 在层级信息列表中的索引
    /// </summary>
    public int levelIndex;
    /// <summary>
    /// 在该层创建镜子的时间点(相对于该层开始的时间，单位为秒)
    /// </summary>
    public int mirrorCrateTime;
}
[CreateAssetMenu(fileName = "WaveData_",menuName = "Scriptable Objects/WaveDetail")]
public class WaveDetailSO : ScriptableObject
{
    [Tooltip("当前波关联的层级信息列表(这一波需要停靠在哪几层)")]
    public List<LevelDetailSO> levelDetails;
    [Tooltip("镜子创建信息(需要创建镜子的层级在当前层级列表的索引，以及创建得的时间点)")]
    [SerializeField]
    public List<MirrorInfo> CreateMirrorLevelInfo;
}
