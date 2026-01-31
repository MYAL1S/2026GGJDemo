using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //#region TestCode
        ////此处仅为测试使用 测试完成后请删除
        //UIMgr.Instance.ShowPanel<GamePanel>();
        //ResMgr.Instance.LoadAsync<GameObject>("ResourcesMgr/ResourcesMgr", (obj) =>
        //{
        //    GameObject resourcesMgrObj = GameObject.Instantiate(obj);
        //    ResourcesMgr resources = resourcesMgrObj.GetComponent<ResourcesMgr>();
        //    if (resources == null)
        //        resources = resourcesMgrObj.AddComponent<ResourcesMgr>();
        //});
        //ResMgr.Instance.LoadAsync<GameObject>("Prefab/Ghost/1", (obj) => 
        //{
        //    GameObject passengerObj = GameObject.Instantiate(obj,new Vector3(0,0,0),Quaternion.identity);
        //    Passenger passenger = passengerObj.GetComponent<Passenger>();
        //    if (passenger == null)
        //        passenger = passengerObj.AddComponent<Passenger>();
        //});
        //ResMgr.Instance.LoadAsync<GameObject>("Prefab/Player/Player", (obj) =>
        //{
        //    GameObject playerObj = GameObject.Instantiate(obj, new Vector3(0, 0, 0), Quaternion.identity);
        //    Player player = playerObj.GetComponent<Player>();
        //    if (player == null)
        //        player = playerObj.AddComponent<Player>();
        //    player.Init(GameDataMgr.Instance.PlayerInfo);
        //});

        //#endregion
        // UIMgr.Instance.ShowPanel<LoginPanel>();
        UIMgr.Instance.ShowPanel<UIBackgroundPanel>(E_UILayer.Bottom);
        UIMgr.Instance.ShowPanel<GamePanel>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    ElevatorMgr.Instance.StartElevator();
        //}
        if (Input.GetKeyDown(KeyCode.S))
        {
            UIMgr.Instance.GetPanel<GamePanel>((gamePanel) =>
            {
                gamePanel.ShowMirrorUI();
            });
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            UIMgr.Instance.GetPanel<GamePanel>((gamePanel) =>
            {
                gamePanel.HideMirrorUI();
            });
        }
    }
}
