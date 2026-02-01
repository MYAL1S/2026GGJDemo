using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        // 初始化鬼魂渲染系统
        GhostRenderSystem.Instance.Setup();
        
        UIMgr.Instance.ShowPanel<LoginPanel>(E_UILayer.Middle, (BasePanel) =>
        {
            GameObject go = ResMgr.Instance.Load<GameObject>("ResourcesMgr/ResourcesMgr");
            GameObject reallyObj = Instantiate(go);
            DontDestroyOnLoad(reallyObj);
        });
    }
}
