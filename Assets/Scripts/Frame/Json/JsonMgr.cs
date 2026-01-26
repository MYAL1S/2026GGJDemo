using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

/// <summary>
/// 存储方式枚举
/// </summary>
public enum JsonType
{
    /// <summary>
    /// 采用Unity自带的JsonUtility进行存储读取
    /// 无法存储读取字典信息
    /// </summary>
    JsonUtility,
    /// <summary>
    /// 采用LitJson进行存储读取
    /// </summary>
    LitJson,
}

/// <summary>
/// Json数据管理类 主要用于进行Json的序列化和反序列化
/// </summary>
public class JsonMgr
{
    private static JsonMgr instance = new JsonMgr();
    public static JsonMgr Instance => instance;

    private JsonMgr() { }
    /// <summary>
    /// 存储数据的方法
    /// </summary>
    /// <param name="data">需要进行存储的数据本身</param>
    /// <param name="fileName">存储的数据名</param>
    /// <param name="type">采用的存储方式(默认采用LitJson)</param>
    public void SaveData(object data, string fileName, JsonType type = JsonType.LitJson)
    {
        string jsonStr = null;
        switch (type)
        {
            case JsonType.JsonUtility:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        File.WriteAllText(Application.persistentDataPath + "/" + fileName + ".json", jsonStr);
    }

    /// <summary>
    /// 读取数据的方法
    /// </summary>
    /// <typeparam name="T">读取的数据类</typeparam>
    /// <param name="fileName">读取的文件名</param>
    /// <param name="type">采用的读取规则</param>
    /// <returns>读取出的数据</returns>
    public T LoadData<T>(string fileName, JsonType type = JsonType.LitJson) where T :new()
    {
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            path = Application.persistentDataPath + "/" + fileName + ".json";
        if (!File.Exists(path))
            return new T(); 
        string jsonStr = File.ReadAllText(path);
        switch (type)
        {
            case JsonType.JsonUtility:
                return JsonUtility.FromJson<T>(jsonStr);
            case JsonType.LitJson:
                return JsonMapper.ToObject<T>(jsonStr);
        }
        return default(T);
    }
}
