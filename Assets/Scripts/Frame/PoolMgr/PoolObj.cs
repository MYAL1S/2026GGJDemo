using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有需要使用缓存池功能的预设体对象都需要挂载该脚本
/// 通过该脚本来设置该对象在缓存池中的上限数量
/// </summary>
public class PoolObj : MonoBehaviour
{
    public int maxNum = 10;
}
