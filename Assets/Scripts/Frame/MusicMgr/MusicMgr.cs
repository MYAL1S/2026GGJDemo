using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 音乐音效管理器
/// </summary>
public class MusicMgr : BaseSingleton<MusicMgr>
{
    /// <summary>
    /// 主音量 控制总体音量大小
    /// 背景音乐以及音效乘以总音量得到最终的值
    /// </summary>
    private float masterValue = 1f;
    /// <summary>
    /// 由于背景音乐在整个游戏过程中只会有一个
    /// 因此在这里直接用一个AudioSource来播放背景音乐
    /// </summary>
    private AudioSource bkMusic = null;
    /// <summary>
    /// 背景音乐音量大小
    /// </summary>
    private float bkMusicValue = 0.1f;
    /// <summary>
    /// 音效列表
    /// </summary>
    private List<AudioSource> soundList = new List<AudioSource>();
    /// <summary>
    /// 音效音量大小
    /// </summary>
    private float soundValue = 0.1f;
    /// <summary>
    /// 是否播放音效
    /// </summary>
    private bool isPlaying = true;

    private MusicMgr() 
    { 
        MonoMgr.Instance.AddFixedUpdateListener(AutoDestroySound);
    }

    /// <summary>
    /// 自动删除播放完毕或已被销毁的音效组件
    /// </summary>
    public void AutoDestroySound()
    {
        if (!isPlaying)
            return;

        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            var src = soundList[i];
            // 已被销毁的 AudioSource（场景切换或对象池被清理）
            if (src == null || src.Equals(null))
            {
                soundList.RemoveAt(i);
                continue;
            }

            if (!src.isPlaying)
            {
                src.clip = null;
                PoolMgr.Instance.PushObj(src.gameObject);
                soundList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 提供给外部用于更改主音量的方法
    /// </summary>
    /// <param name="volume">音量大小</param>
    public void SetMasterValue(float volume)
    {
        masterValue = volume;
        SetBKMusicValue(bkMusicValue);
        SetSoundValue(soundValue);
    }

    /// <summary>
    /// 当前播放的背景音乐路径
    /// </summary>
    private string currentBKMusicPath = null;

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="forceRestart">是否强制重新播放（默认 false，相同音乐不重新播放）</param>
    public void PlayBKMuic(string path, bool forceRestart = false)
    {
        // 如果正在播放同一首音乐且不强制重播，则跳过
        if (!forceRestart && currentBKMusicPath == path && bkMusic != null && bkMusic.isPlaying)
        {
            Debug.Log($"[MusicMgr] 背景音乐已在播放: {path}");
            return;
        }

        if (bkMusic == null)
        {
            GameObject obj = new GameObject("BKMusic");
            bkMusic = obj.AddComponent<AudioSource>();
            Object.DontDestroyOnLoad(obj);
        }

        currentBKMusicPath = path;

        ResMgr.Instance.LoadAsync<AudioClip>(path, (clip) => 
        {
            bkMusic.clip = clip;
            bkMusic.loop = true;
            bkMusic.volume = bkMusicValue * masterValue;
            bkMusic.Play();
        });
    }

    /// <summary>
    /// 停止播放背景音乐
    /// </summary>
    public void StopBKMusic()
    {
        if (bkMusic != null)
        {
            bkMusic.Stop();
            currentBKMusicPath = null;  // 清除当前播放路径
        }
    }

    /// <summary>
    /// 暂停播放背景音乐
    /// </summary>
    public void PauseBKMusic()
    {
        if (bkMusic != null)
            bkMusic.Pause();
    }

    /// <summary>
    /// 设置背景音乐大小
    /// </summary>
    /// <param name="value"></param>
    public void SetBKMusicValue(float value)
    {
        bkMusicValue = value;
        if (bkMusic != null)
            bkMusic.volume = bkMusicValue * masterValue;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="path">音效路径</param>
    /// <param name="isLoop">是否循环播放</param>
    public void PlaySound(string path, bool isLoop,UnityAction<AudioSource> callback = null)
    {
        //异步加载音效资源 加载完成后 播放音效
        ResMgr.Instance.LoadAsync<AudioClip>(path, (clip) => 
        {
            //添加音效组件 并且存入字典 便于管理音效
            AudioSource source = PoolMgr.Instance.GetObj("Sound/SoundObj").GetComponent<AudioSource>();
            if (!soundList.Contains(source))
                soundList.Add(source);
            source.Stop();
            source.clip = clip;
            source.loop = isLoop;
            source.volume = soundValue * masterValue;
            source.Play();
            callback?.Invoke(source);
        });
    }

    /// <summary>
    /// 停止播放所有音效
    /// </summary>
    public void StopSound(AudioSource audioSource)
    {
        if (audioSource == null || audioSource.Equals(null))
            return;

        if (soundList.Contains(audioSource))
        {
            audioSource.Stop();
            soundList.Remove(audioSource);
            audioSource.clip = null;
            PoolMgr.Instance.PushObj(audioSource.gameObject);
        }
    }

    /// <summary>
    /// 设置音效大小
    /// </summary>
    /// <param name="value">音效大小</param>
    public void SetSoundValue(float value)
    {
        soundValue = value;
        
        // 遍历时检查空引用并清理
        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            var item = soundList[i];
            
            // 检查是否已被销毁
            if (item == null || item.Equals(null))
            {
                soundList.RemoveAt(i);
                continue;
            }
            
            item.volume = soundValue * masterValue;
        }
    }

    /// <summary>
    /// 暂停或播放所有音效
    /// </summary>
    /// <param name="isPlay"></param>
    public void PlayOrPauseSound(bool isPlay)
    {
        if (isPlay)
        {
            isPlaying = true;
            // 遍历时检查空引用
            for (int i = soundList.Count - 1; i >= 0; i--)
            {
                var item = soundList[i];
                if (item == null || item.Equals(null))
                {
                    soundList.RemoveAt(i);
                    continue;
                }
                item.Play();
            }
        }
        else 
        {
            isPlaying = false;
            for (int i = soundList.Count - 1; i >= 0; i--)
            {
                var item = soundList[i];
                if (item == null || item.Equals(null))
                {
                    soundList.RemoveAt(i);
                    continue;
                }
                item.Pause();
            }
        }
    }

    /// <summary>
    /// 清除所有音效
    /// 由于在过场景时 对象池内的音效对象会被销毁
    /// 因此需要在过场景时 清除音效列表
    /// </summary>
    public void ClearSound()
    {
        for (int i = soundList.Count - 1; i >= 0; i--)
        {
            var item = soundList[i];
            
            // 检查是否已被销毁
            if (item == null || item.Equals(null))
            {
                soundList.RemoveAt(i);
                continue;
            }
            
            item.Stop();
            item.clip = null;
            PoolMgr.Instance.PushObj(item.gameObject);
        }
        soundList.Clear();
    }
}
