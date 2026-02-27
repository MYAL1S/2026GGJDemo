using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        // ? 从本地读取数据并应用设置
        var gameData = GameDataMgr.Instance;
        if (gameData.SettingInfo != null)
        {
            SettingsApplier.ApplyAll(gameData.SettingInfo);
            Debug.Log("[Main] 已应用游戏设置");
        }
        
        // 初始化鬼魂渲染系统
        GhostRenderSystem.Instance.Setup();
        
        UIMgr.Instance.ShowPanel<MainMenuPanel>(E_UILayer.Middle, (panel) =>
        {
            GameObject go = ResMgr.Instance.Load<GameObject>("ResourcesMgr/ResourcesMgr");
            GameObject reallyObj = Instantiate(go);
            DontDestroyOnLoad(reallyObj);
        });
    }
}
