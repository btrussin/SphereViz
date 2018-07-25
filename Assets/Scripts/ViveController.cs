using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

using HTC.UnityPlugin;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViveController : MonoBehaviour
{

    //List<RaycastResult> raycastResults = new List<RaycastResult>();
    //public List<Canvas> targetedCanvases = new List<Canvas>(); 

    SteamVR_TrackedObject svrto;

    Dictionary<string, NodeManager> currCollisionNodeManagers = new Dictionary<string, NodeManager>();
    Dictionary<string, GroupManager> currCollisionGroupManagers = new Dictionary<string, GroupManager>();
    List<GameObject> currMenuCollisionObjects = new List<GameObject>();

    protected CVRSystem vrSystem;
    protected VRControllerState_t state;
    protected VRControllerState_t prevState = new VRControllerState_t();

    public TextMesh textMesh;
    public SphereCollider sphereCollider;
    public GameObject colliderObject;

    public DataObjectManager dataManager;

    public Ray deviceRay;

    public MoveScaleObject moveScaleObjScript;

    int nodeLayerMask;
    int menuLayerMask;

    public LineRenderer beam;
    bool beamIsActive = false;
    bool sliderActiveByRay = false;
    bool sliderActiveByContact = false;
    SliderManager currSliderManagerByRay;
    SliderManager currSliderManagerByContact;
    float beamLength = 5f;
    Vector3[] beamPts = new Vector3[2];

    public GameObject mainMenu;
    public ViveController otherController;
    public bool menuIsAttached = false;
    public Vector3 foreRtUp;

    public int maxNumberCurvesToRedraw = 20;

    public void setColliderValues(float colliderRadius, float meshScale)
    {
        sphereCollider.radius = colliderRadius;
        colliderObject.transform.localScale = Vector3.one * meshScale;
    }

    public void setColliderValues(float scale)
    {
        float colliderScale = 0.027f * scale + 0.003f;
        float meshScale = 0.063f * scale + 0.007f;

        setColliderValues(colliderScale, meshScale);
    }

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

        if(sliderActiveByContact)
        {
            currSliderManagerByContact.tryMoveValue(deviceRay.origin);
        }
        if( beamIsActive )
        {
            beamPts[0] = deviceRay.origin;

            RaycastHit hitInfo;
            if( sliderActiveByRay )
            {
                beam.startColor = Color.yellow;
                beam.endColor = Color.yellow;
                beamPts[1] = beamPts[0] + deviceRay.direction * beamLength;

                currSliderManagerByRay.tryMoveValue(beamPts[1]);
            }
            else if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 5f, menuLayerMask))
            {
                beam.startColor = Color.yellow;
                beam.endColor = Color.yellow;
                beamPts[1] = beamPts[0] + deviceRay.direction * hitInfo.distance;
    
                if( currSliderManagerByRay == null )
                {
                    currSliderManagerByRay = hitInfo.transform.parent.GetComponent<SliderManager>();

                    if (currSliderManagerByRay != null) currSliderManagerByRay.pointColorOnContact();
                }

            }
            else
            {
                beam.startColor = Color.white;
                beam.endColor = Color.white;

                beamPts[1] = beamPts[0] + deviceRay.direction * 5f;

                if (currSliderManagerByRay != null)
                {
                    currSliderManagerByRay.pointColorOnRelease();
                    currSliderManagerByRay = null;
                }
            }

            beam.SetPositions(beamPts);

        }

        updateState();


        if(Input.GetKeyDown(KeyCode.R))
        {
            releaseAllCollidedObjects();
            //if (colliderObject.activeInHierarchy) colliderObject.SetActive(false);
            //else colliderObject.SetActive(true);  
        }

    }

    void UIRaysCast()
    {
        /*
        raycastResults.Clear();

        foreach (Canvas canvas in targetedCanvases)
        {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            for (int i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget) { continue; }

                //var dist = Vector3.Dot(transForward, trans.position - ray.origin) / Vector3.Dot(transForward, ray.direction);
                float dist;
                new Plane(graphic.transform.forward, graphic.transform.position).Raycast(deviceRay, out dist);
                if (dist > 20f) { continue; }

                raycastResults.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = raycaster,
                    distance = dist,
                    worldPosition = deviceRay.GetPoint(dist),
                    worldNormal = -graphic.transform.forward,
                    screenPosition = Vector2.zero,
                    index = raycastResults.Count,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder
                });
            }
        }
        */
       

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
                if (menuIsAttached)
                {
                    menuIsAttached = false;
                    mainMenu.transform.SetParent(null);
                }
                else
                {
                    menuIsAttached = true;
                    otherController.menuIsAttached = false;
                    mainMenu.SetActive(true);

                    mainMenu.transform.rotation = transform.rotation;
                    mainMenu.transform.Rotate(45.0f, 0f, 0f);

                    mainMenu.transform.position = transform.position +
                        transform.forward * foreRtUp.x +
                        transform.right * foreRtUp.y +
                        transform.up * foreRtUp.z;

                    mainMenu.transform.SetParent(transform);
                }

            }

            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                if ((prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) == 0)
                {
                    // trigger began being pulled
                    if (currCollisionNodeManagers.Count < 1)
                    {
                        beam.gameObject.SetActive(true);
                        beamIsActive = true;
                    }
                }

                if (prevState.rAxis1.x < 1.0f && state.rAxis1.x == 1.0f)
                {
                    // trigger just now pulled to the max

                    // new UI stuff

                    UIRaysCast();


                    // end new UI stuff






                    if (currSliderManagerByContact != null)
                    {
                        sliderActiveByContact = true;
                    }

                    // this is menu logic
                    RaycastHit hitInfo;

                    if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 10f, menuLayerMask))
                    {
                        currSliderManagerByRay = hitInfo.transform.parent.GetComponent<SliderManager>();
                        HLButtonManger buttonManager = hitInfo.collider.gameObject.GetComponent<HLButtonManger>();
                        CloseButtonManager closeManager = hitInfo.collider.gameObject.GetComponent<CloseButtonManager>();

                        if (currSliderManagerByRay != null)
                        {
                            currSliderManagerByRay.pointColorOnContact();
                            beamLength = hitInfo.distance;
                            sliderActiveByRay = true;
                        }
                        else if (buttonManager != null) buttonManager.takeAction();
                        else if (closeManager != null) closeManager.takeAction();
                        else if (hitInfo.collider.gameObject.name.Equals("Deselect"))
                        {
                            DeselectButtonAnimation anim = hitInfo.collider.gameObject.GetComponent<DeselectButtonAnimation>();
                            anim.setAltColors();
                            dataManager.deselectAllNodes();
                        }

                    }

                    // process the menu objects that are currently colliding with the controller
                    foreach(GameObject obj in currMenuCollisionObjects)
                    {
                        HLButtonManger buttonManager = obj.GetComponent<HLButtonManger>();
                        CloseButtonManager closeManager = obj.GetComponent<CloseButtonManager>();

                        if (buttonManager != null) buttonManager.takeAction();
                        else if (closeManager != null) closeManager.takeAction();
                        else if (obj.name.Equals("Deselect"))
                        {
                            DeselectButtonAnimation anim = obj.GetComponent<DeselectButtonAnimation>();
                            anim.setAltColors();
                            dataManager.deselectAllNodes();
                        }
                    }


                    int count = getNumCurvesAffectedByPulling() + otherController.getNumCurvesAffectedByPulling();
                    bool restrictCurveRedraw = count >= maxNumberCurvesToRedraw;

                    // this is to pull nodes
                    foreach (NodeManager nm in currCollisionNodeManagers.Values)
                    {
                        nm.gameObject.GetComponent<MoveScaleObject>().grabSphereWithObject(gameObject);
                        nm.beginPullEffect(dataManager.getCurrBarRadius(), restrictCurveRedraw);
                    }

                    if( currCollisionGroupManagers.Count > 1 ) dataManager.deselectAllNodes();
                    foreach (GroupManager gm in currCollisionGroupManagers.Values)
                    {
                        gm.gameObject.GetComponent<MoveScaleObject>().grabSphereWithObject(gameObject);
                        gm.startMove();
                    }

                    if (restrictCurveRedraw) Debug.Log("Restricting the Curves: " + count);


                }
                else if (prevState.rAxis1.x == 1.0f && state.rAxis1.x < 1.0f)
                {
                    // trigger just now released from the max
                    releaseAllCollidedObjects();
                    /*
                    if (currCollisionNodeManagers.Count > 0)
                    {
                        foreach (NodeManager nm in currCollisionNodeManagers.Values)
                        {
                            MoveScaleObject moveObj = nm.gameObject.GetComponent<MoveScaleObject>();
                            if (moveObj != null) moveObj.releaseSphereWithObject(gameObject);
                            nm.endPullEffect();
                        }
                    }

                    if (currGrpManager != null)
                    {
                        // move the group
                        currGrpManager.gameObject.GetComponent<MoveScaleObject>().releaseSphereWithObject(gameObject);
                        currGrpManager.endActive();
                        currGrpManager.endMove();
                        //currGrpManager = null;
                        dataManager.repopulateEdges();
                    }
                    */

                    if (currSliderManagerByRay != null)
                    {
                        if (currSliderManagerByRay.gameObject.name.Equals("slider_nodeSize")) dataManager.recalculateNodeSizes();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_barRadius")) dataManager.recalculateEdgeRadii();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_innerConnDist")) dataManager.repopulateEdges();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_outerConnDist")) dataManager.repopulateEdges();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_gazeAngle")) dataManager.updateGazeFactors();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_collSize")) dataManager.updateControllerColliderScale();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_sphereViz")) dataManager.updateSphereVisibility();
                        else if (currSliderManagerByRay.gameObject.name.Equals("slider_edgeThinning")) dataManager.updateEdgeThinningAmount();

                        currSliderManagerByRay.pointColorOnRelease();
                        sliderActiveByRay = false;

                        currSliderManagerByRay = null;
                    }


                    if (currSliderManagerByContact )
                    {
                        if (currSliderManagerByContact.gameObject.name.Equals("slider_nodeSize")) dataManager.recalculateNodeSizes();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_barRadius")) dataManager.recalculateEdgeRadii();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_innerConnDist")) dataManager.repopulateEdges();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_outerConnDist")) dataManager.repopulateEdges();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_gazeAngle")) dataManager.updateGazeFactors();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_collSize")) dataManager.updateControllerColliderScale();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_sphereViz")) dataManager.updateSphereVisibility();
                        else if (currSliderManagerByContact.gameObject.name.Equals("slider_edgeThinning")) dataManager.updateEdgeThinningAmount();

                        currSliderManagerByContact.pointColorOnRelease();
                        sliderActiveByContact = false;

                        currSliderManagerByContact = null;
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
                if( currCollisionNodeManagers.Count > 0 && state.rAxis0.y >= 0 )
                {
                    NodeManager[] nodes = new NodeManager[currCollisionNodeManagers.Count];
                    currCollisionNodeManagers.Values.CopyTo(nodes, 0);
                    dataManager.toggleSubNodes(nodes);
                }
                
                if(state.rAxis0.y < 0)
                {
                    dataManager.recenterProjectionSphere();
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

    void releaseAllCollidedObjects()
    {
        if (currCollisionNodeManagers.Count > 0)
        {
            foreach (NodeManager nm in currCollisionNodeManagers.Values)
            {
                MoveScaleObject moveObj = nm.gameObject.GetComponent<MoveScaleObject>();
                if (moveObj != null) moveObj.releaseSphereWithObject(gameObject);
                nm.endPullEffect();
            }
        }

        if (currCollisionGroupManagers.Count > 0)
        {
            foreach (GroupManager gm in currCollisionGroupManagers.Values)
            {
                // move the group
                gm.gameObject.GetComponent<MoveScaleObject>().releaseSphereWithObject(gameObject);
                gm.endActive();
                gm.endMove();
            }

            dataManager.repopulateEdges();
        }

    }

  
    void OnCollisionEnter(Collision collision)
    {
        GameObject firstGameObject = collision.gameObject;

        NodeManager nodeMan = firstGameObject.GetComponent<NodeManager>();
        if( nodeMan != null )
        {
            nodeMan.addCollision();
            if (!currCollisionNodeManagers.ContainsKey(nodeMan.name)) currCollisionNodeManagers.Add(nodeMan.name, nodeMan);

            return;
        }

        GroupManager grpMan = firstGameObject.GetComponent<GroupManager>();
        if (grpMan != null)
        {
            
            if (!currCollisionGroupManagers.ContainsKey(grpMan.name))
            {
                currCollisionGroupManagers.Add(grpMan.name, grpMan);
                grpMan.startActive();
            }

            return;
        }

        GameObject secondGameObject = collision.gameObject.transform.parent != null ? collision.gameObject.transform.parent.gameObject : null;
        if ( secondGameObject != null )
        {
            ConnectionManager connMan = secondGameObject.GetComponent<ConnectionManager>();
            if (connMan != null)
            {
                //connMan.displayText(gameObject.transform.position + gameObject.transform.forward * 0.08f);
                connMan.displayText(collision.contacts[0].point);
                return;
            }

            SliderManager sliderMan = secondGameObject.GetComponent<SliderManager>();
            if (sliderMan != null)
            {
                sliderMan.pointColorOnContact();
                currSliderManagerByContact = sliderMan;
                return;
            }

        }

        currMenuCollisionObjects.Add(collision.gameObject);
    }

    void OnCollisionExit(Collision collision)
    {
        GameObject firstGameObject = collision.gameObject;

        NodeManager nodeMan = firstGameObject.GetComponent<NodeManager>();
        if (nodeMan != null)
        {
            nodeMan.subtractCollision();
            if (currCollisionNodeManagers.ContainsKey(nodeMan.name)) currCollisionNodeManagers.Remove(nodeMan.name);

            return;
        }

        GroupManager grpMan = firstGameObject.GetComponent<GroupManager>();
        if (grpMan != null)
        {
            if (currCollisionGroupManagers.ContainsKey(grpMan.name)) currCollisionGroupManagers.Remove(grpMan.name);
            grpMan.endActive();
            return;
        }

        GameObject secondGameObject = collision.gameObject.transform.parent != null ? collision.gameObject.transform.parent.gameObject : null;

        if( secondGameObject != null )
        {
            ConnectionManager connMan = secondGameObject.GetComponent<ConnectionManager>();
            if (connMan != null)
            {
                connMan.hideText();
                currSliderManagerByContact = null;
                return;
            }
        }

        if (currMenuCollisionObjects.Contains(collision.gameObject)) currMenuCollisionObjects.Remove(collision.gameObject);
    }

    public int getNumCurvesAffectedByPulling()
    {
        int count = 0;
        if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
        {
            foreach (NodeManager nm in currCollisionNodeManagers.Values)
            {
                count += nm.getNumCurvesAffectedByPulling();
            }
        }

        return count;
    }

}
