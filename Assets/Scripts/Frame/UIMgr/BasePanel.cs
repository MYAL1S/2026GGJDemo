using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 面板基类 所有UI面板类都需要继承自该类
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    /// <summary>
    /// 用于控制面板整体淡入淡出 的CanvasGroup组件
    /// </summary>
    private CanvasGroup canvasGroup;
    /// <summary>
    /// 面板是否在显示
    /// </summary>
    private bool isShow = false;
    /// <summary>
    /// 淡入淡出速度
    /// </summary>
    private float alphaSpeed = 5f;
    /// <summary>
    /// 控件字典 用于存储所有要用到的UI控件 用里式替换原则 父类装子类
    /// </summary>
    protected Dictionary<string,UIBehaviour> controlDic = new Dictionary<string, UIBehaviour>();
    // 控件默认名字 如果得到的控件名字存在于这个容器 意味着我们不会通过代码去使用它 它只会是起到显示作用的控件
    private List<string> defaultNameList = new List<string>(){ "Image",
                                                                   "Text (TMP)",
                                                                   "RawImage",
                                                                   "Background",
                                                                   "Checkmark",
                                                                   "Label",
                                                                   "Text (Legacy)",
                                                                   "Arrow",
                                                                   "Placeholder",
                                                                   "Fill",
                                                                   "Handle",
                                                                   "Viewport",
                                                                   "Scrollbar Horizontal",
                                                                   "Scrollbar Vertical"};

    private void Awake()
    {
        //获取CanvasGroup组件
        if (canvasGroup == null )
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        //查找子对象上的所有UI组件并存储到字典中
        FindChildrenControl<Button>();
        FindChildrenControl<Toggle>();
        FindChildrenControl<Slider>();
        FindChildrenControl<InputField>();
        FindChildrenControl<ScrollRect>();
        FindChildrenControl<Dropdown>();
        FindChildrenControl<Text>();
        FindChildrenControl<Image>();
        FindChildrenControl<TextMeshProUGUI>();

        Init();
    }

    /// <summary>
    /// 找到子对象上的指定类型的UI组件 并存储到字典中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void FindChildrenControl<T>() where T : UIBehaviour
    {
        //从子对象中获取指定类型的UI组件
        T[] uiComps = this.GetComponentsInChildren<T>(true);
        //遍历所有获取到的UI组件
        foreach (var item in uiComps)
        {
            string controlName = item.gameObject.name;
            //排除默认名称的控件
            if (defaultNameList.Contains(controlName))
                continue;
            //由于我们不会对需要使用的控件重名 所以这里可以进行判断
            //如果字典中不存在该控件名称 则添加到字典中
            if (!controlDic.ContainsKey(controlName))
            {
                controlDic.Add(controlName, item);
                //为特定类型的UI组件添加默认事件
                if (item is Button)
                {
                    (item as Button).onClick.AddListener(() =>
                    {
                        OnButtonClick(controlName);
                    });
                }
                else if (item is Toggle)
                {
                    (item as Toggle).onValueChanged.AddListener((value) =>
                    {
                        OnToggleValueChanged(controlName, value);
                    });
                }
                else if (item as Slider)
                {
                    (item as Slider).onValueChanged.AddListener((value) =>
                    {
                        OnSliderValueChanged(controlName, value);
                    });
                }
                else if (item as Dropdown)
                {
                    (item as Dropdown).onValueChanged.AddListener((value) =>
                    {
                        OnDropDownValueChanged(controlName, value);
                    });
                }
            }
        }
    }

    /// <summary>
    /// 获取指定名称的UI组件
    /// </summary>
    /// <typeparam name="T">控件类型</typeparam>
    /// <param name="controlName">控件名</param>
    /// <returns></returns>
    public T GetControl<T>(string controlName) where T : UIBehaviour
    {
        if (controlDic.ContainsKey(controlName))
        {
            T control = controlDic[controlName] as T;
            if (control == null)
                Debug.LogError($"获取的控件类型与实际类型不匹配！控件名：{controlName}，获取类型：{typeof(T)}，实际类型：{controlDic[controlName].GetType()}");
            return control;
        }
        else
        {
            Debug.LogError($"未找到指定名称的控件！控件名：{controlName}");
            return null;
        }
    }

    /// <summary>
    /// 提供给子类重写的初始化方法
    /// </summary>
    public virtual void Init()
    { }

    /// <summary>
    /// 显示面板的方法
    /// </summary>
    public virtual void ShowMe()
    {
        isShow = true;
        canvasGroup.alpha = 0;
        SetDefaultSelection();
    }

    /// <summary>
    /// 隐藏面板的方法
    /// </summary>
    public virtual void HideMe()
    {
        isShow = false;
        canvasGroup.alpha = 1;
    }

    /// <summary>
    /// 子类通过重写该方法为指定Button组件添加点击事件
    /// 在Awake方法中已经自动为Button组件添加了点击事件监听
    /// 只需要根据Button组件名进行区分处理即可
    /// </summary>
    /// <param name="name">Button组件名</param>
    protected virtual void OnButtonClick(string name){ }

    /// <summary>
    /// 子类通关重写该方法为指定Toggle组件添加值改变事件
    /// 在Awake方法中已经自动为Toggle组件添加了值改变事件监听
    /// 只需要根据Toggle组件名进行区分处理即可
    /// </summary>
    /// <param name="name">Toggle组件名</param>
    /// <param name="isOn">是否选中</param>
    protected virtual void OnToggleValueChanged(string name, bool isOn){ }

    /// <summary>
    /// 子类通过重写该方法为指定Slider组件添加值改变事件
    /// 在Awake方法中已经自动为Slider组件添加了值改变事件监听
    /// 只需要根据Slider组件名进行区分处理即可
    /// </summary>
    /// <param name="name">Slider组件名</param>
    /// <param name="value">Slider当前值</param>
    protected virtual void OnSliderValueChanged(string name, float value) { }

    /// <summary>
    /// 子类通过重写该方法为指定DropDown组件添加值改变事件
    /// 在Awake方法中已经自动为DropDown组件添加了值改变事件监听
    /// 只需要根据DropDown组件名进行区分处理即可
    /// </summary>
    /// <param name="name">DropDown组件名</param>
    /// <param name="value">DropDown当前索引</param>
    protected virtual void OnDropDownValueChanged(string name, int value) { }

    /// <summary>
    /// 提供默认选中的可交互控件（子类可重写）。
    /// </summary>
    protected virtual Selectable GetDefaultSelectable() { return null; }

    /// <summary>
    /// 将默认控件设为当前 EventSystem 的选中对象，触发高亮。
    /// </summary>
    protected void SetDefaultSelection()
    {
        var es = EventSystem.current;
        if (es == null) return;

        var target = GetDefaultSelectable();
        if (target == null) return;

        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(target.gameObject);
    }

    protected virtual void Update()
    {
        if (isShow && canvasGroup.alpha != 1)
        {
            canvasGroup.alpha += alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha >= 1)
                canvasGroup.alpha = 1;
        }
        else if (!isShow)
        {
            canvasGroup.alpha -= alphaSpeed * Time.deltaTime;
            if (canvasGroup.alpha <= 0)
            {
                canvasGroup.alpha = 0;
            }
        }
    }
}
