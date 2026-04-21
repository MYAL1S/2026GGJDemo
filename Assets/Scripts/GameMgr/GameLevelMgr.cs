using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏关卡管理器
/// </summary>
public class GameLevelMgr : BaseSingleton<GameLevelMgr>
{
    /// <summary>记录当前的楼层信息 从18开始计数</summary>
    public int currentLevel = 18;
    /// <summary>记录当前关卡的详细信息</summary>
    public LevelDetailSO currentLevelDetail;
    /// <summary>本局已生成的特殊乘客数量</summary>
    private int spawnedSpecialCount = 0;

    private GameLevelMgr()
    {
        //此处先写死 后续根据currentLevel动态获取
        currentLevelDetail = ResourcesMgr.Instance.levelDetailSOList[0];
    }

    /// <summary>重置本局的配额计数（开局时调用）</summary>
    public void ResetRuntimeCounters()
    {
        spawnedSpecialCount = 0;
    }

    /// <summary>剩余可用的特殊乘客配额</summary>
    public int GetRemainingSpecialQuota()
    {
        return Mathf.Max(0, ResourcesMgr.Instance.maxSpecialPassengerCount - spawnedSpecialCount);
    }

    /// <summary>本波实际生成的特殊乘客数量回写</summary>
    public void AddSpecialSpawned(int count)
    {
        if (count <= 0) return;
        spawnedSpecialCount = Mathf.Min(ResourcesMgr.Instance.maxSpecialPassengerCount, spawnedSpecialCount + count);
    }
}
