using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncryptionUtil
{
    /// <summary>
    /// 生成加密Key
    /// </summary>
    /// <returns></returns>
    public static int GenerateKey()
    {
        return Random.Range(100000, 999999) + 888;
    }


    /// <summary>
    /// 对数据进行加密
    /// </summary>
    /// <param name="key">加密key</param>
    /// <param name="value">需要加密的数据</param>
    /// <returns></returns>
    public static int LockValue(int key, int value)
    {
        //采用异或加密算法
        value = value ^ key;
        value = value ^ (key >> 16);
        value = value ^ 0xADAD;
        value += key;
        return value;
    }

    /// <summary>
    /// 对数据进行解密
    /// </summary>
    /// <param name="key">加密key</param>
    /// <param name="value">需要解密的数据</param>
    /// <returns></returns>
    public static int UnlockValue(int key, int value)
    {
        //采用异或解密算法
        //如果加密值为0 说明并未加密 则直接返回0
        if (value == 0)
            return 0;
        value -= key;
        value = value ^ key;
        value = value ^ (key >> 16);
        value = value ^ 0xADAD;
        return value;
    }
}
