using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ViveController : MonoBehaviour
{

    public SteamVR_TrackedObject svrto;

    Dictionary<string, NodeManager> currCollisionNodeManagers = new Dictionary<string, NodeManager>();

    protected CVRSystem vrSystem;
    protected VRControllerState_t state;
    protected VRControllerState_t prevState = new VRControllerState_t();

    public TextMesh textMesh;

    public DataLoader dataLoader;

    public Ray deviceRay;

    public MoveScaleObject moveScaleObjScript;

    public ViveController( int n)
    {

    }

    // Use this for initialization
    void Start () {
        vrSystem = OpenVR.System;
    }
	
	// Update is called once per frame
	void Update () {

        gameObject.transform.position = svrto.transform.position;
        gameObject.transform.forward = svrto.transform.forward;
        gameObject.transform.right = svrto.transform.right;
        gameObject.transform.up = svrto.transform.up;



        deviceRay.origin = transform.position;

        Quaternion rayRotation = Quaternion.AngleAxis(60.0f, transform.right);

        deviceRay.direction = rayRotation * transform.forward;

        updateState();
    }
    
    void updateState()
    {
        bool stateIsValid = vrSystem.GetControllerState((uint)svrto.index, ref state, 0);


        if (stateIsValid && state.GetHashCode() != prevState.GetHashCode())
        {
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) != 0)
            {
                dataLoader.deselectAllNodes();
            }
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {

                if (prevState.rAxis1.x < 1.0f && state.rAxis1.x == 1.0f)
                {
                    
                    if (currCollisionNodeManagers.Count > 0)
                    {
                        NodeManager[] nodes = new NodeManager[currCollisionNodeManagers.Count];
                        currCollisionNodeManagers.Values.CopyTo(nodes, 0);
                        dataLoader.toggleSubNodes(nodes);
                    }
                    
                }
                else if (prevState.rAxis1.x == 1.0f && state.rAxis1.x < 1.0f)
                {
                    /*
                    if (currCollisionNodeManagers.Count > 0)
                    {
                        NodeManager[] nodes = new NodeManager[currCollisionNodeManagers.Count];
                        currCollisionNodeManagers.Values.CopyTo(nodes, 0);
                        dataLoader.toggleSubNodes(nodes);
                    }
                    */
                }
            }
            //if ((state.ulButtonTouched & SteamVR_Controller.ButtonMask.Touchpad) != 0) textMesh.text = "Touched Touchpad";
            //if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) != 0) textMesh.text = "Pressed Touchpad";

            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) == 0)
            {
                moveScaleObjScript.grabSphereWithObject(gameObject);
            }
            else if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) == 0 &&
                    (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0)
            {
                moveScaleObjScript.releaseSphereWithObject(gameObject);
            }

            prevState = state;
        }
    }

  
    void OnCollisionEnter(Collision collision)
    {
        NodeManager nodeMan = collision.gameObject.GetComponent<NodeManager>();
        if( nodeMan != null )
        {
            if (!currCollisionNodeManagers.ContainsKey(nodeMan.name)) currCollisionNodeManagers.Add(nodeMan.name, nodeMan);

            return;
        }

        ConnectionManager connMan = collision.gameObject.GetComponent<ConnectionManager>();
        if (connMan != null)
        {
            //connMan.displayText(gameObject.transform.position + gameObject.transform.forward * 0.08f);
            connMan.displayText(collision.contacts[0].point);
            return;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        NodeManager nodeMan = collision.gameObject.GetComponent<NodeManager>();
        if (nodeMan != null)
        {
            if (currCollisionNodeManagers.ContainsKey(nodeMan.name)) currCollisionNodeManagers.Remove(nodeMan.name);

            return;
        }

        ConnectionManager connMan = collision.gameObject.GetComponent<ConnectionManager>();
        if (connMan != null)
        {
            connMan.hideText();
            return;
        }
    }
    
}
