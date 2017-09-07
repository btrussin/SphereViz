using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ViveController : MonoBehaviour
{

    SteamVR_TrackedObject svrto;

    Dictionary<string, NodeManager> currCollisionNodeManagers = new Dictionary<string, NodeManager>();

    protected CVRSystem vrSystem;
    protected VRControllerState_t state;
    protected VRControllerState_t prevState = new VRControllerState_t();

    public TextMesh textMesh;

    public DataLoader dataLoader;

    public Ray deviceRay;

    public MoveScaleObject moveScaleObjScript;

    int nodeLayerMask;
    int menuLayerMask;

    public LineRenderer beam;
    bool beamIsActive = false;
    bool sliderActive;
    SliderManager currSliderManager;
    float beamLength = 5f;
    Vector3[] beamPts = new Vector3[2];

    public GameObject mainMenu;

    // Use this for initialization
    void Start () {
        vrSystem = OpenVR.System;

        svrto = gameObject.GetComponent<SteamVR_TrackedObject>();

        nodeLayerMask = 1 << LayerMask.NameToLayer("NodeLayer");
        menuLayerMask = 1 << LayerMask.NameToLayer("MenuLayer");

        beam.gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update () {

        deviceRay.origin = transform.position;

        Quaternion rayRotation = Quaternion.AngleAxis(60.0f, transform.right);

        deviceRay.direction = rayRotation * transform.forward;

        if( beamIsActive )
        {
            beamPts[0] = deviceRay.origin;

            RaycastHit hitInfo;
            if( sliderActive )
            {
                beam.startColor = Color.yellow;
                beam.endColor = Color.yellow;
                beamPts[1] = beamPts[0] + deviceRay.direction * beamLength;

                currSliderManager.tryMoveValue(beamPts[1]);
            }
            else if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 5f, menuLayerMask))
            {
                beam.startColor = Color.yellow;
                beam.endColor = Color.yellow;
                beamPts[1] = beamPts[0] + deviceRay.direction * hitInfo.distance;
            }
            else
            {
                beam.startColor = Color.white;
                beam.endColor = Color.white;

                beamPts[1] = beamPts[0] + deviceRay.direction * 5f;
            }

            beam.SetPositions(beamPts);

        }

        updateState();
    }
    
    void updateState()
    {
        bool stateIsValid;
        unsafe
        {
            stateIsValid = vrSystem.GetControllerState((uint)svrto.index, ref state, (uint)sizeof(VRControllerState_t));
        }


        if (stateIsValid && state.GetHashCode() != prevState.GetHashCode())
        {
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) != 0)
            {
                if(mainMenu.activeInHierarchy)
                {
                    mainMenu.SetActive(false);
                }
                else
                {
                    mainMenu.SetActive(true);
                    

                    mainMenu.transform.up = new Vector3(0f, 1f, 0f);
                    mainMenu.transform.forward = deviceRay.direction;
                    mainMenu.transform.position = transform.position;

                }
            }

            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                if ((prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) == 0)
                {
                    // trigger began being pulled
                    beam.gameObject.SetActive(true);
                    beamIsActive = true;
                }

                if (prevState.rAxis1.x < 1.0f && state.rAxis1.x == 1.0f)
                {
                    // trigger just now pulled to the max

                    // this is menu logic
                    RaycastHit hitInfo;

                    if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 10f, menuLayerMask))
                    {
                        currSliderManager = hitInfo.transform.parent.GetComponent<SliderManager>();
                  
                        if( currSliderManager != null )
                        {
                            beamLength = hitInfo.distance;
                            sliderActive = true;
                        }
                        else if (hitInfo.collider.gameObject.name.Equals("RefreshButton"))
                        {
                            dataLoader.repopulateEdges();
                        }
                        else if (hitInfo.collider.gameObject.name.Equals("DeselectButton"))
                        {
                            dataLoader.deselectAllNodes();
                        }
                    }


                    // this is to pull nodes
                    foreach (NodeManager nm in currCollisionNodeManagers.Values)
                    {
                        nm.gameObject.GetComponent<MoveScaleObject>().grabSphereWithObject(gameObject);
                        nm.beginPullEffect(dataLoader.getCurrBarRadius());
                    }




                }
                else if (prevState.rAxis1.x == 1.0f && state.rAxis1.x < 1.0f)
                {
                    // trigger just now released from the max
                    currSliderManager = null;
                    sliderActive = false;

                    if (currCollisionNodeManagers.Count > 0)
                    {
                        foreach (NodeManager nm in currCollisionNodeManagers.Values)
                        {
                            MoveScaleObject moveObj = nm.gameObject.GetComponent<MoveScaleObject>();
                            if( moveObj != null ) moveObj.releaseSphereWithObject(gameObject);
                            nm.endPullEffect();
                        }
                    }
                    
                }
            }
            else if((prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                // trigger released all the way
                beam.gameObject.SetActive(false);
                beamIsActive = false;
            }
            //if ((state.ulButtonTouched & SteamVR_Controller.ButtonMask.Touchpad) != 0) textMesh.text = "Touched Touchpad";
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) != 0 && 
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) == 0)
            {
                if(currCollisionNodeManagers.Count > 0 && 
                    state.rAxis0.y > 0 && 
                    Mathf.Abs(state.rAxis0.y) >= Mathf.Abs(state.rAxis0.x))
                {
                    NodeManager[] nodes = new NodeManager[currCollisionNodeManagers.Count];
                    currCollisionNodeManagers.Values.CopyTo(nodes, 0);
                    dataLoader.toggleSubNodes(nodes);
                }
            }

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
