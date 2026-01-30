using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Passenger_",menuName = "Scriptable Objects/Passenger")]
public class PassengerSO : ScriptableObject
{
    /// <summary>
    /// 乘客ID
    /// </summary>
    public int passengerID;
    /// <summary>
    /// 乘客姓名
    /// </summary>
    public string passengerName;
    /// <summary>
    /// 正常状态下的图片
    /// </summary>
    public Sprite normalSprite;
    /// <summary>
    /// 鬼魂名
    /// </summary>
    public string ghostName;
    /// <summary>
    /// 鬼魂状态下的图片
    /// </summary>
    public Sprite ghostSprite;
    /// <summary>
    /// 是否为鬼魂乘客
    /// </summary>
    public bool isGhost;
    /// <summary>
    /// 是否为特殊乘客
    /// </summary>
    public bool isSpecialPassenger;
}
