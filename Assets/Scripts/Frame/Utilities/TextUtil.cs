using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class TextUtil
{
    private static StringBuilder stringBuilder = new StringBuilder();
    #region 字符串拆分
    /// <summary>
    /// 分割字符串为字符串数组的方法
    /// </summary>
    /// <param name="strInput">需要进行分割的字符串</param>
    /// <param name="typeIndex">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_ </param>
    /// <returns></returns>
    public static string[] StringSpilt2StrArray(string strInput, int typeIndex)
    {
        switch (typeIndex)
        {
            case 1:
                //为了避免英文符号填成了中文符号 我们先进行一个替换
                while (strInput.IndexOf("；") != -1)
                    strInput = strInput.Replace("；", ";");
                return strInput.Split(';');
            case 2:
                //为了避免英文符号填成了中文符号 我们先进行一个替换
                while (strInput.IndexOf("，") != -1)
                    strInput = strInput.Replace("，", ",");
                return strInput.Split(',');
            case 3:
                //为了避免英文符号填成了中文符号 我们先进行一个替换
                while (strInput.IndexOf("：") != -1)
                    strInput = strInput.Replace("：", ":");
                return strInput.Split(':');
            case 4:
                return strInput.Split('%');
            case 5:
                return strInput.Split(' ');
            case 6:
                return strInput.Split('|');
            case 7:
                return strInput.Split('_');
            default:
                return new string[0];
        }
    }

    /// <summary>
    /// 将字符串拆分为整数数组的方法
    /// </summary>
    /// <param name="strInput">需要进行分割的字符串</param>
    /// <param name="typeIndex">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_</param>
    /// <returns></returns>

    public static int[] StringSpilt2IntArray(string strInput, int typeIndex)
    {
        //先将字符串拆分为字符串数组
        string[] spiltedStrArray = StringSpilt2StrArray(strInput, typeIndex);
        //如果字符串数组长度为0 则直接返回空的整形数组
        if (spiltedStrArray.Length == 0)
            return new int[0];
        //将字符串数组转换为整形数组并返回
        return Array.ConvertAll<string, int>(spiltedStrArray, s => int.Parse(s));
    }

    /// <summary>
    /// 将字符串拆分为整数键值对数组的方法
    /// 用于拆分类似 "1,2;3,4;5,6" 这种格式的字符串
    /// </summary>
    /// <param name="strInput">需要拆分的字符串</param>
    /// <param name="typeIndex1">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_</param>
    /// <param name="typeIndex2">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_</param>
    /// <param name="callback">回调函数 用于将拆分出的int数组传递出去使用</param>
    public static void StringSpilt2IntKeyPairArray(string strInput, int typeIndex1, int typeIndex2, UnityAction<int, int> callback)
    {
        //先将字符串进行第一次拆分
        string[] firstSplitArray = StringSpilt2StrArray(strInput, typeIndex1);
        //如果拆分后的数组长度为0 则直接返回
        if (firstSplitArray.Length == 0)
            return;
        //遍历第一次拆分得到的字符串数组
        //对每一项进行第二次拆分 并通过回调函数将拆分得到的键值对传递出去
        foreach (string firstSplitItem in firstSplitArray)
        {
            int[] secondSplitIntArray = StringSpilt2IntArray(firstSplitItem, typeIndex2);
            if (secondSplitIntArray.Length != 2)
                continue;
            callback?.Invoke(secondSplitIntArray[0], secondSplitIntArray[1]);
        }
    }

    /// <summary>
    /// 将字符串拆分为字符串键值对数组的方法
    /// 用于拆分类似 "key1:value1;key2:value2;key3:value3" 这种格式的字符串
    /// </summary>
    /// <param name="strInput">需要拆分的字符串</param>
    /// <param name="typeIndex1">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_</param>
    /// <param name="typeIndex2">用于分割的间隔符 1-; 2-, 3-: 4-% 5-空格 6-| 7-_</param>
    /// <param name="callback">回调函数 用于将拆分出的string数组传递出去使用</param>
    public static void StringSpilt2StringKeyPairArray(string strInput, int typeIndex1, int typeIndex2, UnityAction<string, string> callback)
    {
        //先将字符串进行第一次拆分
        string[] firstSplitArray = StringSpilt2StrArray(strInput, typeIndex1);
        //如果拆分后的数组长度为0 则直接返回
        if (firstSplitArray.Length == 0)
            return;
        //遍历第一次拆分得到的字符串数组
        //对每一项进行第二次拆分 并通过回调函数将拆分得到的键值对传递出去
        foreach (string firstSplitItem in firstSplitArray)
        {
            string[] secondSplitIntArray = StringSpilt2StrArray(firstSplitItem, typeIndex2);
            if (secondSplitIntArray.Length != 2)
                continue;
            callback?.Invoke(secondSplitIntArray[0], secondSplitIntArray[1]);
        }
    }
    #endregion

    #region 数字转字符串相关
    /// <summary>
    /// 将整数转换为指定长度的字符串数字 前面补0
    /// 如果长度不足 则补0
    /// 如果长度过多 则不做处理
    /// </summary>
    /// <param name="value">想要转换的整数</param>
    /// <param name="length">指定的长度</param>
    /// <returns>转换后的字符串</returns>
    public static string GetNumStr(int value,int length)
    {
        return value.ToString($"D{length}");
    }

    /// <summary>
    /// 将浮点数转换为指定小数位数的字符串数字
    /// 如果小数位数不足 则补0
    /// 如果小数位数过多 则会舍弃多余的小数位
    /// </summary>
    /// <param name="value">想要进行转换的浮点数</param>
    /// <param name="length">保留的小数位数</param>
    /// <returns>转换后的字符串</returns>
    public static string GetDecimalStr(float value,int length)
    {
        return value.ToString($"F{length}");
    }

    /// <summary>
    /// 将大数字转换为字符串显示的方法
    /// </summary>
    /// <param name="num">需要进行转换的大数字</param>
    /// <returns>转换后的字符串(如n亿n千万,n万n千)</returns>
    public static string GetBigDataToString(int num)
    {
        int hundredMillion = 100000000;
        int tenThousand = 10000;
        //如果大于1亿 那么就显示 n亿n千万
        if (num >= hundredMillion)
        {
            return BigDataToString(num, hundredMillion, "亿", "千万");
        }
        //如果大于1万 那么就显示 n万n千
        else if (num >= tenThousand)
        {
            return BigDataToString(num, tenThousand, "万", "千");
        }
        else
            return num.ToString();
    }

    /// <summary>
    /// 实际进行大数字转换为字符串的方法
    /// </summary>
    /// <param name="num">进行转换的大数字</param>
    /// <param name="company">大的运算单位(亿，万)</param>
    /// <param name="bigCompanyStr">大的运算单位字符</param>
    /// <param name="littltCompanyStr">小的运算单位字符</param>
    /// <returns>转换后的大数字字符</returns>
    private static string BigDataToString(int num, int company, string bigCompanyStr, string littltCompanyStr)
    {
        int bigNum = num / company;
        int littleNum = (num % company) / (company / 10);
        stringBuilder.Clear();
        stringBuilder.Append(bigNum);
        stringBuilder.Append(bigCompanyStr);
        if (littleNum != 0)
        {
            stringBuilder.Append(littleNum);
            stringBuilder.Append(littltCompanyStr);
        }

        return stringBuilder.ToString();
    }
    #endregion

    #region 时间转换相关
    /// <summary>
    /// 将秒数转换为 时分秒 字符串的方法
    /// </summary>
    /// <param name="second">传入的需要转换的秒数</param>
    /// <param name="isIgnoreZero">是否需要忽略0(如忽略则60秒显示为1分0秒)</param>
    /// <param name="isKeepLength">是否需要保留两位显示(如23时01分03秒，不开启则显示默认)</param>
    /// <param name="hourStr">小时后的字符(时/h)</param>
    /// <param name="minuteStr">分钟后的字符(分/m)</param>
    /// <param name="secondStr">秒后的字符(秒/s)</param>
    /// <returns>转换后的时分秒字符串</returns>
    public static string SecondToHMS(int second,bool isIgnoreZero,bool isKeepLength = false,string hourStr = "时",string minuteStr = "分",string secondStr = "秒")
    {
        if (second < 0)
            second = 0;

        int hour = second / 3600;
        int minute = (second % 3600)/60;
        int sec = second % 60;

        //先清理掉之前的内容
        stringBuilder.Clear();
        //如果小时不为0或者不忽略0 则拼接小时部分
        if (hour != 0 || !isIgnoreZero)
        {
            stringBuilder.Append(isKeepLength ? GetNumStr(hour,2) : hour);
            stringBuilder.Append(hourStr);
        }
        //如果分钟不为0或者不忽略0或者小时不为0 则拼接分钟部分
        if (minute != 0 || !isIgnoreZero || hour != 0)
        {
            stringBuilder.Append(isKeepLength ? GetNumStr(minute,2) : minute);
            stringBuilder.Append(minuteStr);
        }
        //如果秒不为0或者不忽略0或者小时不为0或者分钟不为0 则拼接秒部分
        if (sec != 0 || hour != 0 || minute != 0)
        {
            stringBuilder.Append(isKeepLength ? GetNumStr(sec,2) : sec);
            stringBuilder.Append(secondStr);
        }
        //如果全部都是0 则默认拼接0秒
        if (second == 0)
        {
            stringBuilder.Append(0);
            stringBuilder.Append(secondStr);
        }

        return stringBuilder.ToString();
    }

    public static string SecondToHMSStandardDisplay(int second,bool isIngoreZero)
    {
        return SecondToHMS(second,isIngoreZero,true,":",":","");
    }
    #endregion
}
