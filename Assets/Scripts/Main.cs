using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        UIMgr.Instance.ShowPanel<LoginPanel>(E_UILayer.Middle, (BasePanel) =>
        {
            ResMgr.Instance.LoadAsync<GameObject>("ResourcesMgr/ResourcesMgr", (obj) =>
            {
                GameObject resMgrObj = Instantiate(obj);
                DontDestroyOnLoad(resMgrObj);
            });
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
