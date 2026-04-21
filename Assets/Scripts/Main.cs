using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        // 从本地读取数据并应用设置
        var gameData = GameDataMgr.Instance;
        if (gameData.SettingInfo != null)
            SettingsApplier.ApplyAll(gameData.SettingInfo);

        // 显示主菜单界面
        UIMgr.Instance.ShowPanel<MainMenuPanel>(E_UILayer.Middle, (panel) =>
        {
            GameObject go = ResMgr.Instance.Load<GameObject>("ResourcesMgr/ResourcesMgr");
            GameObject reallyObj = Instantiate(go);
            DontDestroyOnLoad(reallyObj);
        });
    }
}
