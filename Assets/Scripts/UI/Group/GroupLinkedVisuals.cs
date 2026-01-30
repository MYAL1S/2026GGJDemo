using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 组级联动：当组内任一交互控件（Dropdown/Button/Toggle/Slider/InputField等）被鼠标悬停/按下/选中时，
/// 将组内所有 Text / TextMeshProUGUI 统一改变视觉（颜色 / 加粗）。
/// 仅在鼠标按下触发 Dropdown 值变更时才清除选中恢复 Normal（键盘导航不清除）。
/// 挂在父对象上（包含 Text 与 Dropdown 的组）即可自动绑定子控件。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class GroupLinkedVisuals : MonoBehaviour
{
    [Header("视觉设置")]
    /// <summary>正常状态颜色</summary>
    public Color normalColor = Color.white;
    /// <summary>悬停状态颜色</summary>
    public Color highlightColor = Color.cyan;
    /// <summary>按下状态颜色</summary>
    public Color pressedColor = Color.grey;
    /// <summary>选中状态颜色</summary>
    public Color selectedColor = Color.cyan;
    /// <summary>选中时是否加粗</summary>
    public bool selectedBold = true;

    /// <summary>可选按名字过滤要受控的文本（为空则控制所有 Text/TMP）</summary>
    public string[] textNameFilter = new string[0];
 
    /// <summary>内部状态：悬停对象集合，避免重复计数</summary>
    private HashSet<GameObject> hovered = new HashSet<GameObject>();
    /// <summary>内部状态：按下对象集合，避免重复计数</summary>
    private HashSet<GameObject> pressed = new HashSet<GameObject>();
    /// <summary>当前选中的子对象</summary>
    private GameObject selectedChild = null;
 
    /// <summary>记录由鼠标按下触发的控件（用于 Dropdown 值变更判断）</summary>
    private HashSet<GameObject> mousePressedSet = new HashSet<GameObject>();
 
    /// <summary>缓存需要联动变色的 Text 组件</summary>
    private List<Text> texts = new List<Text>();
    /// <summary>缓存需要联动变色的 TextMeshProUGUI 组件</summary>
    private List<TextMeshProUGUI> tmps = new List<TextMeshProUGUI>();

    /// <summary>初始化缓存并绑定事件</summary>
    void Awake()
    {
        CacheTexts();
        BindChildren();
        ApplyNormal();
    }
 
    /// <summary>编辑器校验时刷新文本缓存</summary>
    void OnValidate()
    {
        if (!Application.isPlaying) CacheTexts();
    }
 
    /// <summary>缓存组内所有需要联动的文本组件</summary>
    void CacheTexts()
    {
        texts.Clear();
        tmps.Clear();
        foreach (var t in GetComponentsInChildren<Text>(true))
        {
            if (MatchesFilter(t.gameObject.name)) texts.Add(t);
        }
        foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (MatchesFilter(t.gameObject.name)) tmps.Add(t);
        }
    }

    /// <summary>按名称过滤文本是否需要被联动控制</summary>
    bool MatchesFilter(string name)
    {
        if (textNameFilter == null || textNameFilter.Length == 0) return true;
        foreach (var f in textNameFilter) if (!string.IsNullOrEmpty(f) && name.Contains(f)) return true;
        return false;
    }
 
    /// <summary>为组内所有 Selectable 绑定事件转发器和 Dropdown 特殊回调</summary>
    void BindChildren()
    {
        // 为所有 Selectable 子对象添加 forwarder（避免根覆盖问题）
        var selectables = GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
        {
            var forwarder = s.gameObject.GetComponent<GroupChildForwarder>();
            if (forwarder == null) forwarder = s.gameObject.AddComponent<GroupChildForwarder>();
            forwarder.parent = this;

            // Dropdown 特殊监听值变更
            var uiDropdown = s as Dropdown;
            if (uiDropdown != null)
            {
                uiDropdown.onValueChanged.AddListener(_ => OnDropdownValueChanged(s.gameObject));
            }

            var tmpDropdown = s.GetComponent<TMP_Dropdown>();
            if (tmpDropdown != null)
            {
                tmpDropdown.onValueChanged.AddListener(_ => OnDropdownValueChanged(s.gameObject));
            }
        }

        // 如果组内没有 Selectable，但你希望根也响应 pointer，手动在 Inspector 为根添加 GroupChildForwarder（可选）
        // 这里不自动在根上加 Graphic，避免覆盖其他组事件。
    }
    
    #region 子事件（由 GroupChildForwarder 调用）

    /// <summary>清理已失效或被隐藏的悬停/按下/选中记录</summary>
    private void CleanupInactiveEntries()
    {
        hovered.RemoveWhere(go => go == null || !go.activeInHierarchy);
        pressed.RemoveWhere(go => go == null || !go.activeInHierarchy);
        if (selectedChild != null && !selectedChild.activeInHierarchy) selectedChild = null;
    }

    /// <summary>在状态变化后根据当前集合决定视觉状态</summary>
    private void ApplyStateAfterChange()
    {
        CleanupInactiveEntries();
        if (hovered.Count == 0 && pressed.Count == 0 && selectedChild == null) ApplyNormal();
        else if (selectedChild != null) ApplySelected();
        else if (pressed.Count > 0) ApplyPressed();
        else ApplyHighlight();
    }

    /// <summary>子对象 PointerEnter 回调</summary>
    internal void ChildPointerEnter(GameObject go)
    {
        if (hovered.Add(go))
        {
            if (pressed.Count > 0) ApplyPressed();
            else ApplyHighlight();
        }
    }

    /// <summary>子对象 PointerExit 回调</summary>
    internal void ChildPointerExit(GameObject go)
    {
        if (hovered.Remove(go))
        {
            ApplyStateAfterChange();
        }
    }

    /// <summary>子对象 PointerDown 回调</summary>
    internal void ChildPointerDown(GameObject go)
    {
        if (pressed.Add(go))
        {
            // 记录为鼠标按下，供 Dropdown 值变更时判断
            mousePressedSet.Add(go);
            ApplyPressed();
        }
    }

    /// <summary>子对象 PointerUp 回调</summary>
    internal void ChildPointerUp(GameObject go)
    {
        if (pressed.Remove(go))
        {
            ApplyStateAfterChange();
        }
    }

    /// <summary>子对象 Select 回调</summary>
    internal void ChildSelect(GameObject go)
    {
        selectedChild = go;
        ApplySelected();
    }

    /// <summary>子对象 Deselect 回调</summary>
    internal void ChildDeselect(GameObject go)
    {
        if (selectedChild == go) selectedChild = null;
        ApplyStateAfterChange();
    }

    /// <summary>子对象被禁用时回调，清理状态</summary>
    internal void ChildDisabled(GameObject go)
    {
        hovered.Remove(go);
        pressed.Remove(go);
        if (selectedChild == go) selectedChild = null;
        ApplyStateAfterChange();
    }

    #endregion

    #region Dropdown 值变更（仅鼠标触发时清除选中）

    /// <summary>Dropdown 值变更回调（区分鼠标/键盘触发）</summary>
    private void OnDropdownValueChanged(GameObject dropdownGO)
    {
        StartCoroutine(HandleDropdownValueChangedNextFrame(dropdownGO));
    }

    /// <summary>下一帧处理 Dropdown 值变更，确保模板关闭后清理残留状态</summary>
    private IEnumerator HandleDropdownValueChangedNextFrame(GameObject dropdownGO)
    {
        yield return null; // 等一帧，等待内部流程完成

        if (mousePressedSet.Contains(dropdownGO))
        {
            // 鼠标触发：清除 EventSystem 的选中以触发 Deselect 流程，恢复 Normal/Highlight
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(null);

            // 保底调用
            ChildDeselect(dropdownGO);

            mousePressedSet.Remove(dropdownGO);
        }
        // Dropdown 关闭时，其模板项会被隐藏/销毁，清理悬停/按下残留
        ApplyStateAfterChange();
        // 否则：键盘触发，不清除选中（保持选中态）
    }

    #endregion

    #region 视觉实现

    /// <summary>应用 Normal 视觉</summary>
    void ApplyNormal()
    {
        SetTexts(normalColor, false);
    }

    /// <summary>应用 Highlight 视觉</summary>
    void ApplyHighlight()
    {
        SetTexts(highlightColor, false);
    }

    /// <summary>应用 Pressed 视觉</summary>
    void ApplyPressed()
    {
        SetTexts(pressedColor, false);
    }

    /// <summary>应用 Selected 视觉</summary>
    void ApplySelected()
    {
        SetTexts(selectedColor, selectedBold);
    }

    /// <summary>批量设置文本颜色与加粗</summary>
    void SetTexts(Color c, bool bold)
    {
        foreach (var t in texts)
        {
            if (t == null) continue;
            t.color = c;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }
        foreach (var t in tmps)
        {
            if (t == null) continue;
            t.color = c;
            if (bold) t.fontStyle = FontStyles.Bold;
            else t.fontStyle &= ~FontStyles.Bold;
        }
    }

    #endregion

    /// <summary>销毁时卸载 Dropdown 监听，防止残留</summary>
    void OnDestroy()
    {
        // 卸载 Dropdown 监听（安全）
        var selectables = GetComponentsInChildren<Selectable>(true);
        foreach (var s in selectables)
        {
            var uiDropdown = s as Dropdown;
            if (uiDropdown != null) uiDropdown.onValueChanged.RemoveAllListeners();
            var tmpDropdown = s.GetComponent<TMP_Dropdown>();
            if (tmpDropdown != null) tmpDropdown.onValueChanged.RemoveAllListeners();
        }
    }
}

/// <summary>
/// 子控件转发器：添加到组内的 Selectable GameObject 上（自动添加），
/// 将指针/选择事件回传给组。
/// </summary>
public class GroupChildForwarder : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [HideInInspector] public GroupLinkedVisuals parent;

    void Reset()
    {
        // 尝试自动找到父组
        if (parent == null) parent = GetComponentInParent<GroupLinkedVisuals>();
    }

    /// <summary>指针进入</summary>
    public void OnPointerEnter(PointerEventData eventData) => parent?.ChildPointerEnter(gameObject);
    /// <summary>指针移出</summary>
    public void OnPointerExit(PointerEventData eventData) => parent?.ChildPointerExit(gameObject);
    /// <summary>指针按下</summary>
    public void OnPointerDown(PointerEventData eventData) => parent?.ChildPointerDown(gameObject);
    /// <summary>指针抬起</summary>
    public void OnPointerUp(PointerEventData eventData) => parent?.ChildPointerUp(gameObject);
    /// <summary>获得选择</summary>
    public void OnSelect(BaseEventData eventData) => parent?.ChildSelect(gameObject);
    /// <summary>失去选择</summary>
    public void OnDeselect(BaseEventData eventData) => parent?.ChildDeselect(gameObject);

    /// <summary>被禁用时清理父组状态</summary>
    void OnDisable() => parent?.ChildDisabled(gameObject);
}