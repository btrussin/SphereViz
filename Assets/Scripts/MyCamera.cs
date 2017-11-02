using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCamera : MonoBehaviour
{

    public Vector3 currPosition = Vector3.zero;

    public DataObjectManager dataManager;

    Transform mainTrans;
    Camera mainCamera;

    float forward = 0.0f;
    float right = 0.0f;
    float up = 0.0f;

    // Use this for initialization
    void Start()
    {
        mainCamera = Camera.main;
        mainTrans = mainCamera.transform;

    }

    // Update is called once per frame
    void Update()
    {

        Vector3 pos = mainTrans.position;
        if (Input.GetKeyDown(KeyCode.W)) forward = 0.05f;
        else if (Input.GetKeyDown(KeyCode.S)) forward = -0.05f;
        else if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S)) forward = 0.0f;

        if (Input.GetKeyDown(KeyCode.D)) right = 0.05f;
        else if (Input.GetKeyDown(KeyCode.A)) right = -0.05f;
        else if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A)) right = 0.0f;

        if (Input.GetKeyDown(KeyCode.Z)) up = 0.05f;
        else if (Input.GetKeyDown(KeyCode.X)) up = -0.05f;
        else if (Input.GetKeyUp(KeyCode.Z) || Input.GetKeyUp(KeyCode.X)) up = 0.0f;

        mainTrans.position = mainTrans.position + mainTrans.right * right + mainTrans.forward * forward + mainTrans.up * up;

        currPosition = mainTrans.position;

        if (Input.GetMouseButtonDown(0)) fire();
    }

    void fire()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));

        RaycastHit hitInfo;

        if (Physics.Raycast(ray.origin, ray.direction, out hitInfo, 10.0f))
        {
            NodeManager nodeMan = hitInfo.collider.gameObject.GetComponent<NodeManager>();
            if (nodeMan != null)
            {
                NodeManager[] nodes = new NodeManager[1];
                nodes[0] = nodeMan;

                dataManager.toggleSubNodes(nodes);
            }
        }
    }
}
