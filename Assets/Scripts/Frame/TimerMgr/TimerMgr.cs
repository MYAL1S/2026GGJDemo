using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 计时器管理器
/// </summary>
public class TimerMgr : BaseSingleton<TimerMgr>
{
    /// <summary>
    /// 计时器id自增变量
    /// </summary>
    private int timerId = 0;
    /// <summary>
    /// 记录所有受TimeScale影响的计时器
    /// </summary>
    private Dictionary<int,TimerItem> timerDic = new Dictionary<int,TimerItem>();
    /// <summary>
    /// 记录所有不受TimeScale影响的计时器
    /// </summary>
    private Dictionary<int, TimerItem> realTimerDic = new Dictionary<int, TimerItem>();
    /// <summary>
    /// 待移除的计时器列表
    /// </summary>
    private readonly List<int> removeListScaled = new List<int>();
    private readonly List<int> removeListReal = new List<int>();
    // 临时Key缓存，避免遍历时集合被改动
    private readonly List<int> tempKeysScaled = new List<int>();
    private readonly List<int> tempKeysReal = new List<int>();

    /// <summary>
    /// 记录计时器管理器中的受TimeScale计时用的协同程序
    /// </summary>
    private Coroutine timer;

    /// <summary>
    /// 记录计时器管理器中的不受TimeScale计时用的协同程序
    /// </summary>
    private Coroutine realTimer;

    /// <summary>
    /// 受TimeScale影响的计时器等待时间
    /// </summary>
    private WaitForSeconds waitForSeconds = new WaitForSeconds(intervalTime);

    /// <summary>
    /// 不受TimeScale影响的计时器等待时间
    /// </summary>
    private WaitForSecondsRealtime waitForSecondsRealtime = new WaitForSecondsRealtime(intervalTime);

    /// <summary>
    /// 记录计时器管理器中的唯一计时用的协同程序 的间隔时间
    /// </summary>
    public const float intervalTime = 0.1f;

    private TimerMgr()
    {
        Start();
    }

    /// <summary>
    /// 开启计时器管理器的方法
    /// </summary>
    public void Start()
    {
        timer = MonoMgr.Instance.StartCoroutine(StartTiming(false, timerDic, tempKeysScaled, removeListScaled));
        realTimer = MonoMgr.Instance.StartCoroutine(StartTiming(true, realTimerDic, tempKeysReal, removeListReal));
    }

    /// <summary>
    /// 停止计时器管理器的方法
    /// </summary>
    public void Stop()
    {
        MonoMgr.Instance.StopCoroutine(timer);
        MonoMgr.Instance.StopCoroutine(realTimer);
    }

    public IEnumerator StartTiming(bool isReal,Dictionary<int,TimerItem> timerDic,List<int> tempKeys,List<int> removeList)
    {
        TimerItem timer;
        while (true)
        {
            //如果是不受TimeScale影响的计时器
            if (isReal)
                yield return waitForSecondsRealtime;
            //如果是受TimeScale影响的计时器
            else
                //100毫秒进行一次计时
                yield return waitForSeconds;
            // 遍历前拷贝 key，避免迭代中集合被改动
            tempKeys.Clear();
            tempKeys.AddRange(timerDic.Keys);
            foreach (int id in tempKeys)
             {
                 timer = timerDic[id];
                if (timer.isRunning)
                {
                    timer.intervalTime -= (int)(intervalTime * 1000);
                    if (timer.intervalTime <= 0)
                    {
                        timer.intervalCallback?.Invoke();
                        timer.intervalTime = timer.maxIntervalTime;
                    }

                    timer.totalTime -= (int)(intervalTime * 1000);
                    if (timer.totalTime <= 0)
                    {
                        timer.delayCallback?.Invoke();
                        RemoveTimer(id);
                    }
                }
            }

            foreach (int id in removeList)
             {
                 timerDic.Remove(id);
             }
             removeList.Clear();
         }
     }


    /// <summary>
    /// 创建单个计时器
    /// </summary>
    /// <param name="isReal">是否受TimeScale影响(受:false,不受:true)</param>
    /// <param name="maxTime">总时间(毫秒)</param>
    /// <param name="delayCallback">延迟执行的回调函数</param>
    /// <param name="intervalTime">间隔时间</param>
    /// <param name="intervalCallback">间隔执行的回调函数</param>
    /// <returns></returns>
    public int CreateTimer(bool isReal, int maxTime, UnityAction delayCallback, int intervalTime = 0, UnityAction intervalCallback = null)
    {
        int id = ++timerId;
        TimerItem timer;
        //选择对应的字典
        Dictionary<int, TimerItem> timerDic = isReal ? realTimerDic : this.timerDic;
        timer = PoolMgr.Instance.GetObj<TimerItem>();
        timer.InitInfo(id, maxTime, delayCallback, intervalTime, intervalCallback);
        timerDic.Add(id, timer);
        return id;
    }

    /// <summary>
    /// 移除单个计时器
    /// </summary>
    /// <param name="id">计时器id</param>
    public void RemoveTimer(int id)
    {
        //查看字典中是否存在该计时器
        //如果存在则移除
        if (timerDic.ContainsKey(id))
        {
            TimerItem timer = timerDic[id];
            PoolMgr.Instance.PushObj(timer);
            removeListScaled.Add(id);
        }
        else if (realTimerDic.ContainsKey(id))
        {
            TimerItem timer = realTimerDic[id];
            PoolMgr.Instance.PushObj(timer);
            removeListReal.Add(id);
        }
    }

    /// <summary>
    /// 重置单个计时器
    /// </summary>
    /// <param name="id">计时器id</param>
    public void ResetTimer(int id)
    {
        if (timerDic.ContainsKey(id))
        {
            TimerItem timer = timerDic[id];
            timer.RestTimer();
        }
        else if (realTimerDic.ContainsKey(id))
        {
            TimerItem timer = realTimerDic[id];
            timer.RestTimer();
        }
    }

    /// <summary>
    /// 开启单个计时器
    /// </summary>
    /// <param name="id">计时器id</param>
    public void StartTimer(int id)
    {
        if (timerDic.ContainsKey(id))
        {
            TimerItem timer = timerDic[id];
            timer.isRunning = true;
        }
        else if (realTimerDic.ContainsKey(id))
        {
            TimerItem timer = realTimerDic[id];
            timer.isRunning = true;
        }
    }

    /// <summary>
    /// 停止单个计时器
    /// </summary>
    /// <param name="id">计时器id</param>
    public void StopTimer(int id)
    {
        if (timerDic.ContainsKey(id))
        {
            TimerItem timer = timerDic[id];
            timer.isRunning = false;
        }
        else if (realTimerDic.ContainsKey(id))
        {
            TimerItem timer = realTimerDic[id];
            timer.isRunning = false;
        }
    }
}
