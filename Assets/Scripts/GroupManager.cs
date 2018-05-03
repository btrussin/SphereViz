using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupManager : MonoBehaviour {

    public GroupInfo groupInfo;
    public GameObject popupTextPrefab;
    public DataObjectManager dataObjManager;
    public GameObject sphereCenterReference;
    Dictionary<NodeInfo, GameObject> nodeInfoMap = new Dictionary<NodeInfo, GameObject>();
   
    PopupTextFade popupTextFade;

    Color mainColor;

    Transform projSphereTrans;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void addNode(NodeInfo info, GameObject node)
    {
        nodeInfoMap.Add(info, node);
    }

    public void init(string name = "")
    {
        if( name.Length > 0 ) gameObject.name = name;

        GameObject popupTextObject = (GameObject)Instantiate(popupTextPrefab);
        popupTextObject.transform.position = transform.position;
        popupTextObject.transform.SetParent(transform);

        popupTextFade = popupTextObject.GetComponent<PopupTextFade>();
        popupTextFade.parentObject = gameObject;
        TextMesh tMesh = popupTextFade.GetComponent<TextMesh>();
        tMesh.text = gameObject.name;

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();
        gazeScript.addTextObject(popupTextFade);

    }

    public PopupTextFade getTextFadeScript()
    {
        return popupTextFade;
    }

    public void startActive()
    {
        mainColor = gameObject.GetComponent<Renderer>().material.color;
        Color tColor = mainColor;
        tColor.a = 1f;
        gameObject.GetComponent<Renderer>().material.color = tColor;
    }

    public void endActive()
    {
        gameObject.GetComponent<Renderer>().material.color = mainColor;
    }

    public void startMove()
    {
        sphereCenterReference.SetActive(true);
        projSphereTrans = gameObject.transform.parent;

        //NodeInfo info;
        GameObject node;

        foreach(KeyValuePair<NodeInfo, GameObject> kv in nodeInfoMap)
        {
            //info = kv.Key;
            node = kv.Value;

            node.transform.SetParent(gameObject.transform);
        }

        gameObject.transform.SetParent(null);
    }


    public void endMove()
    {
        sphereCenterReference.SetActive(false);
        //projSphere.transform.TransformPoint(currNodeInfo.position3);
        Vector3 sphereCenter = projSphereTrans.position;
        Vector3 centerToPt;

        NodeInfo info;
        GameObject node;
        float y, z;
        foreach (KeyValuePair<NodeInfo, GameObject> kv in nodeInfoMap)
        {
            info = kv.Key;
            node = kv.Value;

            centerToPt = node.transform.position - sphereCenter;
            centerToPt.Normalize();

            node.transform.position = projSphereTrans.localScale.x * 0.5f * centerToPt + sphereCenter;
            Utils.getYZSphericalCoordinates(projSphereTrans, node.transform, out y, out z);

            info.position2.x = y / 90f;
            info.position2.y = z / 60f;
            dataObjManager.updateProjectedPointsForNodeInfo(info);
            node.transform.SetParent(projSphereTrans);
        }


        centerToPt = transform.position - sphereCenter;
        centerToPt.Normalize();

        Utils.getYZSphericalCoordinates(projSphereTrans, transform, out y, out z);

        groupInfo.center2.x = y / 90f;
        groupInfo.center2.y = z / 60f;
        dataObjManager.updateProjectedPointsForGroupInfo(groupInfo);

        transform.position = sphereCenter + centerToPt * projSphereTrans.localScale.x * dataObjManager.radius * 0.8f;

        gameObject.transform.SetParent(projSphereTrans);
    }
}
