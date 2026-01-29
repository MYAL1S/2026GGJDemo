using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏关卡管理器
/// </summary>
public class GameLevelMgr : BaseSingleton<GameLevelMgr>
{
    /// <summary>
    /// 当前游戏波数
    /// </summary>
    private int currentWave = 0;
    /// <summary>
    /// 记录当前的楼层信息 从18开始计数
    /// </summary>
    public int currentLevel = 18;
    /// <summary>
    /// 虚假的楼层信息 用于显示在ui上
    /// </summary>
    private int visualLevel = 0;
    /// <summary>
    /// 记录当前关卡的详细信息
    /// </summary>
    public LevelDetailSO currentLevelDetail;
   

    private GameLevelMgr()
    {
        //此处先写死 后续根据currentLevel动态获取
        currentLevelDetail = ResourcesMgr.Instance.levelDetailSOList[0];
    }

    public void AddWave()
    {
        currentWave++;
    }

    /// <summary>
    /// 更新当前楼层信息
    /// </summary>
    public void UpdateLevelInfo()
    {
        //根据当前楼层更新详细信息
        currentLevelDetail = ResourcesMgr.Instance.levelDetailSOList[currentLevel];
    }
}
