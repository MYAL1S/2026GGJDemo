using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏关卡管理器
/// </summary>
public class GameLevelMgr : BaseSingleton<GameLevelMgr>
{
    /// <summary>当前游戏波数</summary>
    private int currentWave = 0;
    /// <summary>记录当前的楼层信息 从18开始计数</summary>
    public int currentLevel = 18;
    /// <summary>虚假的楼层信息 用于显示在ui上</summary>
    private int visualLevel = 0;
    /// <summary>记录当前关卡的详细信息</summary>
    public LevelDetailSO currentLevelDetail;

    /// <summary>本局已生成的特殊乘客数量</summary>
    private int spawnedSpecialCount = 0;
    /// <summary>本局已触发的镜像次数</summary>
    private int mirrorOccurenceCount = 0;

    private GameLevelMgr()
    {
        //此处先写死 后续根据currentLevel动态获取
        currentLevelDetail = ResourcesMgr.Instance.levelDetailSOList[0];
    }

    public void AddWave()
    {
        currentWave++;
    }

    /// <summary>更新当前楼层信息</summary>
    public void UpdateLevelInfo()
    {
        //根据当前楼层更新详细信息
        currentLevelDetail = ResourcesMgr.Instance.levelDetailSOList[currentLevel];
    }

    /// <summary>重置本局的配额计数（开局时调用）</summary>
    public void ResetRuntimeCounters()
    {
        spawnedSpecialCount = 0;
        mirrorOccurenceCount = 0;
        Debug.Log("[GameLevelMgr] 配额计数已重置");
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

    /// <summary>尝试消耗一次镜像机会</summary>
    public bool TryConsumeMirrorOccurence()
    {
        if (mirrorOccurenceCount >= ResourcesMgr.Instance.maxMirrorOccourence)
            return false;
        mirrorOccurenceCount++;
        return true;
    }
}
