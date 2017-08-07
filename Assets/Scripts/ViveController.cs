using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ViveController : SteamVR_TrackedObject
{

    static NodeManager managerA = null;
    static NodeManager managerB = null;

    NodeManager currCollisionNodeManager = null;

    protected CVRSystem vrSystem;
    protected VRControllerState_t state;
    protected VRControllerState_t prevState = new VRControllerState_t();

    public TextMesh textMesh;

    public DataLoader dataLoader;

    // Use this for initialization
    void Start () {
        vrSystem = OpenVR.System;
    }
	
	// Update is called once per frame
	void Update () {
        updateState();
    }

    void updateState()
    {
        bool stateIsValid = vrSystem.GetControllerState((uint)index, ref state);

        if (stateIsValid && state.GetHashCode() != prevState.GetHashCode())
        {
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) != 0) textMesh.text = "Pressed Menu Button";
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                
                if ( prevState.rAxis1.x < 1.0f && state.rAxis1.x == 1.0f )
                {
                    if(currCollisionNodeManager!=null) addNodeManager(currCollisionNodeManager);
                }

            }
            if ((state.ulButtonTouched & SteamVR_Controller.ButtonMask.Touchpad) != 0) textMesh.text = "Touched Touchpad";
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) != 0) textMesh.text = "Pressed Touchpad";
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0) textMesh.text = "Pressed Grip";



            prevState = state;
        }
    }

    public  void addNodeManager(NodeManager manager)
    {
        if (managerA == null) activateA(manager);
        else if (managerB == null) activateB(manager);
        else
        {
            dataLoader.removeSubNodes(managerA);
            managerA = managerB;
            managerB = manager;
            dataLoader.populateSubNodes(managerB);
        }

        if( managerA != null && managerB != null && managerA != managerB )
        {
            dataLoader.populateSubNodeConnections(managerA, managerB);
        }
    }

    void activateA(NodeManager manager)
    {
        dataLoader.removeSubNodes(managerA);

        managerA = manager;

        dataLoader.populateSubNodes(managerA);
        
    }

    void activateB(NodeManager manager)
    {

        dataLoader.removeSubNodes(managerB);

        managerB = manager;

        dataLoader.populateSubNodes(managerB);
    }

    
    void OnCollisionEnter(Collision collision)
    {
        NodeManager nodeMan = collision.gameObject.GetComponent<NodeManager>();
        if( nodeMan != null )
        {
            currCollisionNodeManager = nodeMan;
        }
        

    }

    void OnCollisionExit(Collision collision)
    {
        NodeManager nodeMan = collision.gameObject.GetComponent<NodeManager>();
        if (nodeMan != null && nodeMan == currCollisionNodeManager)
        {
            currCollisionNodeManager = null;
        }
    }
    
}
