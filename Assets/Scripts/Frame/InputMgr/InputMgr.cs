using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 输入状态枚举
/// </summary>
public enum InputStatus
{
    /// <summary>
    /// 长按
    /// </summary>
    Press,
    /// <summary>
    /// 按下
    /// </summary>
    Down,
    /// <summary>
    /// 抬起
    /// </summary>
    Up
}

public enum InputType
{
    /// <summary>
    /// 键盘输入
    /// </summary>
    Key,
    /// <summary>
    /// 鼠标输入
    /// </summary>
    Mouse,
}

/// <summary>
/// 输入信息类
/// </summary>
/// <typeparam name="T">输入类型(按键,鼠标,热键)</typeparam>
public class InputInfo
{
    /// <summary>
    /// 输入状态
    /// 按下,抬起,长按
    /// </summary>
    public InputStatus status;
    /// <summary>
    /// 输入类型
    /// 键盘，鼠标
    /// </summary>
    public InputType type;
    /// <summary>
    /// 键盘按键
    /// </summary>
    public KeyCode keyCode;
    /// <summary>
    /// 鼠标ID
    /// </summary>
    public int mouseID;

    /// <summary>
    /// 提供给外部输入初始化的方法(键盘输入信息)
    /// </summary>
    /// <param name="input">键盘按键</param>
    /// <param name="inputStatus">键盘按键状态</param>
    public InputInfo(KeyCode input,InputStatus inputStatus)
    {
        keyCode = input;
        type = InputType.Key;
        status = inputStatus;
    }

    /// <summary>
    /// 提供给外部输入初始化的方法(鼠标输入信息)
    /// </summary>
    /// <param name="input">鼠标ID</param>
    /// <param name="inputStatus">鼠标按键状态</param>
    public InputInfo(int input, InputStatus inputStatus)
    {
        mouseID = input;
        type = InputType.Mouse;
        status = inputStatus;
    }
}


/// <summary>
/// 输入管理模块
/// </summary>
public class InputMgr : BaseSingleton<InputMgr>
{

    /// <summary>
    /// 存储输入信息的字典
    /// </summary>
    private Dictionary<E_EventType,InputInfo> inputDic = new Dictionary<E_EventType, InputInfo>();
    /// <summary>
    /// 用于当前遍历时取出的输入信息
    /// </summary>
    private InputInfo nowInputInfo;

    /// <summary>
    /// 是否开启了输入系统检测
    /// </summary>
    public bool isStart = false;

    /// <summary>
    /// 是否检测键盘输入
    /// </summary>
    public bool isDetectKeyboard = true;

    /// <summary>
    /// 是否检测鼠标输入
    /// </summary>
    public bool isDetectMouse = true;

    /// <summary>
    /// 是否检测热键输入
    /// </summary>
    public bool isDetectHotKey = true;

    /// <summary>
    /// 获取输入信息的回调委托
    /// </summary>
    private UnityAction<InputInfo> getInputInfoCallback;

    /// <summary>
    /// 是否开始检测输入信息
    /// </summary>
    private bool isBeginCheckInput = false;

    private InputMgr()
    {
        MonoMgr.Instance.AddUpdateListener(InputListener);
    }

    /// <summary>
    /// 提供给外部开启或者关闭输入检测的方法
    /// </summary>
    /// <param name="isStart">是否开启输入检测</param>
    public void StartOrStopDetectInput(bool isStart)
    {
        this.isStart = isStart;
    }

    /// <summary>
    /// 提供给外部开启或者关闭键盘输入检测的方法
    /// </summary>
    /// <param name="isDetect">是否开启键盘检测</param>
    public void StartOrStopDetectKeyboard(bool isDetect)
    {
        this.isDetectKeyboard = isDetect;
    }

    /// <summary>
    /// 提供给外部开启或者关闭鼠标输入检测的方法
    /// </summary>
    /// <param name="isDetect">是否开启鼠标检测</param>
    public void StartOrStopDetectMouse(bool isDetect)
    {
        this.isDetectMouse = isDetect;
    }

    /// <summary>
    /// 提供给外部开启或者关闭热键输入检测的方法
    /// </summary>
    /// <param name="isDetect">是否开启热键检测</param>
    public void StartOrStopDetectHotKey(bool isDetect)
    {
        this.isDetectHotKey = isDetect;
    }

    /// <summary>
    /// 监听输入
    /// </summary>
    public void InputListener()
    {
        // 外部调用GetInputInfo方法获取输入信息
        // 检测获取输入信息
        // 延迟一帧，避免和其他输入冲突
        if (isBeginCheckInput)
        {
            if (Input.anyKeyDown)
            {
                InputInfo nowInfo = null;
                //遍历按键枚举
                //获取键盘输入信息
                Array keycodes = Enum.GetValues(typeof(KeyCode));
                foreach (KeyCode key in keycodes)
                {
                    if (Input.GetKeyDown(key))
                    {
                        nowInfo = new InputInfo(key,InputStatus.Down);
                        break;
                    }
                }
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        nowInfo = new InputInfo(i,InputStatus.Down);
                        break;
                    }
                }

                // 回调获取输入信息
                getInputInfoCallback?.Invoke(nowInfo);
                getInputInfoCallback = null;
                isBeginCheckInput = false;
            }
        }


        //如果输入系统没有开启则不检测输入
        if (!isStart)
            return;

        //遍历输入字典，检测对应按键输入状态，触发对应事件
        foreach (E_EventType events in inputDic.Keys)
        {
            nowInputInfo =  inputDic[events];
            if (isDetectKeyboard && nowInputInfo.type == InputType.Key)
            {
                switch (nowInputInfo.status)
                {
                    case InputStatus.Press:
                        if (Input.GetKey(nowInputInfo.keyCode))
                            TriggerKeyboardEvent(events, nowInputInfo.keyCode);
                        break;
                    case InputStatus.Down:
                        if (Input.GetKeyDown(nowInputInfo.keyCode))
                            TriggerKeyboardEvent(events, nowInputInfo.keyCode);
                        break;
                    case InputStatus.Up:
                        if (Input.GetKeyUp(nowInputInfo.keyCode))
                            TriggerKeyboardEvent(events, nowInputInfo.keyCode);
                        break;
                    default:
                        break;
                }
            }
            else if (isDetectMouse && nowInputInfo.type == InputType.Mouse)
            {
                switch (nowInputInfo.status)
                {
                    case InputStatus.Press:
                        if (Input.GetMouseButton(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(events);
                        break;
                    case InputStatus.Down:
                        if (Input.GetMouseButtonDown(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(events);
                        break;
                    case InputStatus.Up:
                        if (Input.GetMouseButtonUp(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(events);
                        break;
                    default:
                        break;
                }
            }
        }

        if (isDetectHotKey)
        {
            InputHotKey("Horizontal");
            InputHotKey("Vertical");
        }
    }

    /// <summary>
    /// 触发键盘事件
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="key">按键</param>
    private void TriggerKeyboardEvent(E_EventType eventType, KeyCode key)
    {
        if (EventCenter.Instance.eventDic.TryGetValue(eventType, out var info))
        {
            if (info is EventInfo<KeyCode>)
            {
                EventCenter.Instance.EventTrigger<KeyCode>(eventType, key);
                return;
            }
        }
        EventCenter.Instance.EventTrigger(eventType);
    }


    /// <summary>
    /// 提供给外部改键或初始化某一按键和事件绑定的方法(键盘)
    /// </summary>
    /// <param name="eventType">按键绑定的事件</param>
    /// <param name="key">按键</param>
    /// <param name="inputStatus">按键状态(按下，抬起，长按)</param>
    public void ChangeKeyCodeInfo(E_EventType eventType,KeyCode key,InputStatus inputStatus)
    {
        if (!inputDic.ContainsKey(eventType))
        {
            inputDic.Add(eventType, new InputInfo(key, inputStatus));
        }
        else
        {
            inputDic[eventType].type = InputType.Key;
            inputDic[eventType].keyCode = key;
            inputDic[eventType].status = inputStatus;
        }

    }


    /// <summary>
    /// 提供给外部改键或初始化某一按键和事件绑定的方法(鼠标)
    /// </summary>
    /// <param name="eventType">鼠标绑定的事件</param>
    /// <param name="mouseID">鼠标id</param>
    /// <param name="inputStatus">按键状态(按下，抬起，长按)</param>
    public void ChangeMouseInfo(E_EventType eventType, int mouseID,InputStatus inputStatus)
    {
        if (!inputDic.ContainsKey(eventType))
        {
            inputDic.Add(eventType,new InputInfo(mouseID,inputStatus));
        }
        else
        {
            inputDic[eventType].type = InputType.Mouse;
            inputDic[eventType].mouseID = mouseID;
            inputDic[eventType].status = inputStatus;
        }
    }

    /// <summary>
    /// 监听热键输入
    /// </summary>
    /// <param name="hotKey">热键名</param>
    public void InputHotKey(string hotKey)
    {
        switch (hotKey)
        {
            case "Horizontal":
                EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal,Input.GetAxis(hotKey));
                break;
            case "Vertical":
                EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Vertical, Input.GetAxis(hotKey));
                break;
        }
    }

    /// <summary>
    /// 提供给外部获取当前输入信息的方法
    /// </summary>
    /// <param name="callback">需要获取输入信息的回调函数</param>
    public void GetInputInfo(UnityAction<InputInfo> callback)
    {
        getInputInfoCallback = callback;
        MonoMgr.Instance.StartCoroutine(BeginCheckInput());
    }

    /// <summary>
    /// 延迟一帧开始检测输入信息
    /// </summary>
    /// <returns></returns>
    private IEnumerator BeginCheckInput()
    {
        yield return 0;
        isBeginCheckInput = true;
    }
}
