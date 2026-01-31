using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDetail_",menuName = "Scriptable Objects/LevelDetail")]
public class LevelDetailSO : ScriptableObject
{
    /// <summary>
    /// 乘客生成位置数组
    /// </summary>
    public List<Vector3> passengerSpawnPositionArray;
    /// <summary>
    /// 普通乘客数量
    /// </summary>
    public int normalPassengerCount;
    /// <summary>
    /// 鬼魂数量
    /// </summary>
    public int ghostCount;
    /// <summary>
    /// 停留时间 单位为秒
    /// </summary>
    public int dockingTime;
    /// <summary>
    /// 这一层是第几层
    /// </summary>
    public int level;
    /// <summary>
    /// 特殊乘客数量
    /// </summary>
    public int specialPassengerCount;
}
