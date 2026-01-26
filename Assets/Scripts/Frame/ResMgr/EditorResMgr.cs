using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 编辑器资源管理器
/// </summary>
public class EditorResMgr : BaseSingleton<EditorResMgr>
{
    /// <summary>
    /// 编辑器资源根目录
    /// </summary>
    private string rootPath = "Assets/Editor/ArtRes/";
    private EditorResMgr() { }

    /// <summary>
    /// 从编辑器资源目录加载单个资源
    /// </summary>
    /// <typeparam name="T">加载的资源类型</typeparam>
    /// <param name="path">资源路径</param>
    /// <returns></returns>
    public T LoadEditorRes<T>(string path) where T : Object
    {
        string suffix = "";
        if (typeof(T) == typeof(GameObject))
            suffix = ".prefab";  
        else if (typeof(T) == typeof(Texture))
            suffix = ".png";
        else if (typeof(T) == typeof(Material))
            suffix = ".mat";
        else if (typeof(T) == typeof(AudioClip))
            suffix = ".mp3";
        return AssetDatabase.LoadAssetAtPath<T>(rootPath + path + suffix);
    }


    /// <summary>
    /// 从图集资源中加载指定名称的图片
    /// </summary>
    /// <param name="path">图集路径</param>
    /// <param name="spriteName">需要加载的资源名</param>
    /// <returns></returns>
    public Sprite LoadSprite(string path, string spriteName)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(rootPath +  path);
        foreach (var item in sprites)
        {
            if (spriteName == item.name)
                return item as Sprite;
        }
        return null;
    }


    /// <summary>
    /// 加载图集中的所有图片资源
    /// </summary>
    /// <param name="path">图集路径</param>
    /// <returns></returns>
    public Dictionary<string, Sprite> LoadSprites(string path)
    {
        Dictionary<string, Sprite> spriteDic = new Dictionary<string, Sprite>();
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(rootPath + path);
        foreach (var item in sprites)
        {
            spriteDic.Add(item.name, item as Sprite);
        }
        return spriteDic;
    }
}
#endif