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
    /// 符咒生成位置数组
    /// </summary>
    public List<Vector3> charmSpawnPositionArray;
    /// <summary>
    /// 普通乘客数量
    /// </summary>
    public int normalPassengerCount;
    /// <summary>
    /// 鬼魂数量
    /// </summary>
    public int ghostCount;
    /// <summary>
    /// 符咒数量
    /// </summary>
    public int charmCount;
    /// <summary>
    /// 停留时间 单位为秒
    /// </summary>
    public int dockingTime;
}
