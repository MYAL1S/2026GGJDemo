using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// UI层级枚举
/// </summary>
public enum E_UILayer
{
    Bottom,
    Middle,
    Top,
    System
}

/// <summary>
/// UI管理器
/// </summary>
public class UIMgr : BaseSingleton<UIMgr>
{
    private abstract class BasePanelInfo { }

    private class PanelInfo<T> : BasePanelInfo
    {
        public T panel;
        public UnityAction<T> callback;
        public bool isHide = false;

        public PanelInfo(UnityAction<T> callback)
        {
            if (callback != null)
                this.callback += callback;
        }
    }

    //渲染UI的画布 相机 事件系统
    private Canvas uiCanvas;
    private EventSystem uiEventSystem;
    private Camera uiCamera;
    private EnsureCameraRenders ensureCameraRenders;

    //UI层级父对象
    private Transform bottomLayer;
    private Transform middleLayer;
    private Transform topLayer;
    private Transform systemLayer;

    /// <summary>
    /// 存储所有面板
    /// </summary>
    private Dictionary<string, BasePanelInfo> panelDic = new Dictionary<string, BasePanelInfo>();

    /// <summary>
    /// 存储所有后处理效果
    /// </summary>
    private Dictionary<string, IPostProcessEffect> postProcessEffects = new Dictionary<string, IPostProcessEffect>();

    private UIMgr()
    {
        // 初始化UI画布、相机、事件系统
        uiCamera = GameObject.Instantiate(ResMgr.Instance.Load<GameObject>("UI/UICamera")).GetComponent<Camera>();
        GameObject.DontDestroyOnLoad(uiCamera);

        uiCanvas = GameObject.Instantiate(ResMgr.Instance.Load<GameObject>("UI/Canvas")).GetComponent<Canvas>();
        GameObject.DontDestroyOnLoad(uiCanvas);

        uiEventSystem = GameObject.Instantiate(ResMgr.Instance.Load<GameObject>("UI/EventSystem")).GetComponent<EventSystem>();
        GameObject.DontDestroyOnLoad(uiEventSystem);

        uiCanvas.worldCamera = uiCamera;

        bottomLayer = uiCanvas.transform.Find("Bottom");
        middleLayer = uiCanvas.transform.Find("Middle");
        topLayer = uiCanvas.transform.Find("Top");
        systemLayer = uiCanvas.transform.Find("System");

        // 添加辅助渲染组件（确保后处理触发）
        if (ensureCameraRenders == null)
            ensureCameraRenders = uiCamera.gameObject.AddComponent<EnsureCameraRenders>();

        Debug.Log("[UIMgr] UI 系统初始化完成");
    }

    #region 后处理效果管理

    /// <summary>
    /// 添加后处理效果到 UICamera
    /// </summary>
    /// <typeparam name="T">后处理效果类型，必须实现 IPostProcessEffect 接口</typeparam>
    /// <param name="autoInitialize">是否自动调用初始化方法（默认为 true）</param>
    /// <returns>添加的后处理效果实例</returns>
    public T AddPostProcessEffect<T>(bool autoInitialize = true) where T : MonoBehaviour, IPostProcessEffect
    {
        string effectName = typeof(T).Name;

        // 检查是否已经添加过
        if (postProcessEffects.ContainsKey(effectName))
        {
            Debug.LogWarning($"[UIMgr] 后处理效果 '{effectName}' 已存在，返回现有实例");
            return postProcessEffects[effectName] as T;
        }

        // 添加组件到 UICamera
        T effect = uiCamera.gameObject.AddComponent<T>();

        // 调用初始化方法
        if (autoInitialize)
        {
            effect.Initialize();
        }

        // 存储到字典
        postProcessEffects.Add(effectName, effect);

        Debug.Log($"[UIMgr] 已添加后处理效果: {effect.EffectName}");

        return effect;
    }

    /// <summary>
    /// 获取指定的后处理效果
    /// </summary>
    /// <typeparam name="T">后处理效果类型</typeparam>
    /// <returns>后处理效果实例，如果不存在则返回 null</returns>
    public T GetPostProcessEffect<T>() where T : MonoBehaviour, IPostProcessEffect
    {
        string effectName = typeof(T).Name;

        if (postProcessEffects.ContainsKey(effectName))
        {
            return postProcessEffects[effectName] as T;
        }

        Debug.LogWarning($"[UIMgr] 未找到后处理效果: {effectName}");
        return null;
    }

    /// <summary>
    /// 移除指定的后处理效果
    /// </summary>
    /// <typeparam name="T">后处理效果类型</typeparam>
    public void RemovePostProcessEffect<T>() where T : MonoBehaviour, IPostProcessEffect
    {
        string effectName = typeof(T).Name;

        if (postProcessEffects.ContainsKey(effectName))
        {
            IPostProcessEffect effect = postProcessEffects[effectName];
            effect.Cleanup();
            GameObject.Destroy(effect as MonoBehaviour);
            postProcessEffects.Remove(effectName);

            Debug.Log($"[UIMgr] 已移除后处理效果: {effectName}");
        }
        else
        {
            Debug.LogWarning($"[UIMgr] 无法移除不存在的后处理效果: {effectName}");
        }
    }

    /// <summary>
    /// 启用或禁用指定的后处理效果
    /// </summary>
    /// <typeparam name="T">后处理效果类型</typeparam>
    /// <param name="enabled">是否启用</param>
    public void SetPostProcessEffectEnabled<T>(bool enabled) where T : MonoBehaviour, IPostProcessEffect
    {
        string effectName = typeof(T).Name;

        if (postProcessEffects.ContainsKey(effectName))
        {
            postProcessEffects[effectName].IsEnabled = enabled;
            Debug.Log($"[UIMgr] 后处理效果 '{effectName}' 已{(enabled ? "启用" : "禁用")}");
        }
        else
        {
            Debug.LogWarning($"[UIMgr] 无法设置不存在的后处理效果: {effectName}");
        }
    }

    /// <summary>
    /// 获取所有后处理效果的列表（用于调试）
    /// </summary>
    public List<string> GetAllPostProcessEffectNames()
    {
        return new List<string>(postProcessEffects.Keys);
    }

    #endregion

    /// <summary>
    /// 获取 UICamera 引用（供外部访问）
    /// </summary>
    public Camera GetUICamera()
    {
        return uiCamera;
    }

    /// <summary>
    /// 提供给外部获取UI层级的接口
    /// </summary>
    /// <param name="uILayer">UI层级枚举</param>
    /// <returns></returns>
    public Transform GetLayer(E_UILayer uILayer)
    {
        switch (uILayer)
        {
            case E_UILayer.Bottom:
                return bottomLayer;
            case E_UILayer.Middle:
                return middleLayer;
            case E_UILayer.Top:
                return topLayer;
            case E_UILayer.System:
                return systemLayer;
            default: return null;
        }
    }

    /// <summary>
    /// 显示面板方法
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="uilayer">面板所在的层级(可选，默认为MiddleLayer)</param>
    /// <param name="callback">面板显示完成后调用的回调函数(可选，默认为空)</param>
    public void ShowPanel<T>(E_UILayer uilayer = E_UILayer.Middle, UnityAction<T> callback = null) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (panelDic.ContainsKey(panelName))
        {
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
            //如果此时资源没有加载完就调用ShowPanel，这样就会回调函数添加到委托里
            if (panelInfo.panel == null)
            {
                panelInfo.isHide = false;
                if (callback != null)
                    panelInfo.callback += callback;
                return;
            }

            //走到此逻辑 说明资源已经加载完了
            //面板处于隐藏 那么重新激活
            if (!panelInfo.panel.gameObject.activeSelf)
                panelInfo.panel.gameObject.SetActive(true);
            //直接显示面板
            panelInfo.panel.ShowMe();
            callback?.Invoke(panelDic[panelName] as T);
            return;
        }

        panelDic.Add(panelName, new PanelInfo<T>(callback));

        ResMgr.Instance.LoadAsync<GameObject>("UI/" + panelName, (panel) =>
        {
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
            if (panelInfo.isHide)
            {
                panelDic.Remove(panelName);
                return;
            }
            Transform faterTransform = GetLayer(uilayer);
            if (faterTransform == null)
            {
                Debug.LogError("没有传递正确的层级名字,请检查传递的参数");
                return;
            }

            GameObject panelObj = GameObject.Instantiate(panel, faterTransform, false);
            T panelCom = panelObj.GetComponent<T>();
            panelInfo.panel = panelCom;
            panelCom.ShowMe();
            panelInfo.callback?.Invoke(panelInfo.panel);
            panelInfo.callback = null;
        });
    }

    /// <summary>
    /// 隐藏面板方法
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    public void HidePanel<T>(bool isDestroy = false) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
        if (panelDic.ContainsKey(panelName))
        {
            //如果资源还没加载完成
            //则把isHide设置为true，等资源加载完成后隐藏面板
            if (panelInfo.panel == null)
            {
                panelInfo.isHide = true;
                panelInfo.callback = null;
                return;
            }
            //如果资源已经加载完成则直接隐藏面板
            panelInfo.panel.HideMe();
            if (isDestroy)
            {
                GameObject.Destroy(panelInfo.panel.gameObject);
                panelDic.Remove(panelName);
            }
            else
                panelInfo.panel.gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// 获取面板方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public void GetPanel<T>(UnityAction<T> callback) where T : BasePanel
    {
        string panelName = typeof(T).Name;

        if (panelDic.ContainsKey(panelName))
        {
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
            //资源还未加载完成
            if (panelInfo.panel != null)
            {
                panelInfo.callback += callback;
            }
            //资源加载完成
            else if(!panelInfo.isHide)
            {
                callback.Invoke(panelInfo.panel);
            }
        }
    }

    /// <summary>
    /// 为控件添加自定义事件
    /// </summary>
    /// <param name="controller">需要添加自定义事件的控件</param>
    /// <param name="type">自定义事件类型</param>
    /// <param name="callback">回调函数</param>
    public void AddCustomEventListener(UIBehaviour controller,EventTriggerType type,UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = controller.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = controller.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }
}
