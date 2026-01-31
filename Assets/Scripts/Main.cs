using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UIMgr.Instance.ShowPanel<LoginPanel>(E_UILayer.Middle, (BasePanel) =>
        {
            ResMgr.Instance.LoadAsync<GameObject>("ResourcesMgr/ResourcesMgr", (obj) =>
            {
                Instantiate(obj);
            });;
        });
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
