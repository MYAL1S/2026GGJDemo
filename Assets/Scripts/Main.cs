using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        
        UIMgr.Instance.ShowPanel<LoginPanel>(E_UILayer.Middle, (BasePanel) =>
        {
            GameObject go = ResMgr.Instance.Load<GameObject>("ResourcesMgr/ResourcesMgr");
            GameObject reallyObj = Instantiate(go);
            DontDestroyOnLoad(reallyObj);
        });
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
