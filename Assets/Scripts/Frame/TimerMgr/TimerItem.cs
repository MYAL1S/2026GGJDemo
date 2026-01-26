using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 计时器类
/// </summary>
public class TimerItem :IPoolObject
{
    /// <summary>
    /// 唯一标识id
    /// </summary>
    public int id;
    /// <summary>
    /// 延迟回调
    /// </summary>
    public UnityAction delayCallback;
    /// <summary>
    /// 重复回调
    /// </summary>
    public UnityAction intervalCallback;
    /// <summary>
    /// 总时间(ms)
    /// </summary>
    public int totalTime;
    /// <summary>
    /// 记录的最大总时间(ms)
    /// 用于一次性计时器的重置
    /// </summary>
    public int maxTotalTime;
    /// <summary>
    /// 间隔时间(ms)
    /// </summary>
    public int intervalTime;
    /// <summary>
    /// 记录的最大间隔时间(ms)
    /// 用于重复计时器的重置
    /// </summary>
    public int maxIntervalTime;
    /// <summary>
    /// 是否开启计时器
    /// </summary>
    public bool isRunning;

    /// <summary>
    /// 初始化计时器信息
    /// </summary>
    /// <param name="id">计时器id</param>
    /// <param name="totalTime">总时间</param>
    /// <param name="delayCallback">延迟执行的回调函数</param>
    /// <param name="intervalTime">间隔时间</param>
    /// <param name="intervalCallback">间隔执行的回调函数</param>
    public void InitInfo(int id, int totalTime, UnityAction delayCallback, int intervalTime = 0, UnityAction intervalCallback = null)
    {
        this.id = id;
        maxTotalTime = this.totalTime = totalTime;
        this.delayCallback = delayCallback;
        maxIntervalTime = this.intervalTime = intervalTime;
        this.intervalCallback = intervalCallback;
        isRunning = true;
    }

    /// <summary>
    /// 重置计时器
    /// </summary>
    public void RestTimer()
    {
        totalTime = maxTotalTime;
        intervalTime = maxIntervalTime;
        isRunning = true;
    }


    /// <summary>
    /// 对象池回收对象时调用 重置对象数据
    /// </summary>
    public void Reset()
    {
        delayCallback = null;
        intervalCallback = null;
    }
}
