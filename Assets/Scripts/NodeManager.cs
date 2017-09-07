using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour {

    public string nodeName = "";
    Color origColor = Color.white;
    Material material = null;
    MeshRenderer meshRend = null;
    public NodeInfo nodeInfo = null;

    List<string> subNodeNames = new List<string>();

    List<ConnectionManager> innerConnections = new List<ConnectionManager>();

    int numCollisions = 0;
    bool origRotationSet = false;
    public Vector3 positionOnSphere = Vector3.zero;
    Quaternion origRotation;
    Quaternion snapRotation;
    Vector3 snapPosition;
    public Transform baseSphereTransform = null;
    GameObject stretchEdge = null;
    BezierBar bezBar;

    public GameObject curvePrefab;

    bool activePull = false;
    bool snapBack = false;

    Vector3[] curveBasePoints = new Vector3[4];

    float _timeToSnapBack = 2.0f;
    float timeToSnapBack_inv = 0.5f;
    float snapTime;

    public void addInnerConnection(ConnectionManager conn)
    {
        innerConnections.Add(conn);
    }

    public void removeInnerConnections()
    {
        innerConnections.Clear();
    }

    public float timeToSnapBack
    {
        get { return _timeToSnapBack; }
        set
        {
            if (value > 0.0f)
            {
                timeToSnapBack_inv = 1.0f / value;
                _timeToSnapBack = value;
            }
        }
    }

    // Use this for initialization
    void Start () {
        meshRend = gameObject.GetComponent<MeshRenderer>();
        if( meshRend != null )
        {
            material = meshRend.material;

            if (material != null)
            {
                origColor = material.color;
            }
        }

        if (!origRotationSet)
        {
            origRotation = gameObject.transform.localRotation;
            origRotationSet = true;
        }

    }
	
	// Update is called once per frame
	void Update () {
        if (activePull) updatePullCurve();
        else if (snapBack)
        {
            doSnapBack();
            updatePullCurve();
        }

	}

    public void beginPullEffect(float barRadius)
    {
        if (stretchEdge == null)
        {
            stretchEdge = (GameObject)Instantiate(curvePrefab);
            bezBar = stretchEdge.GetComponent<BezierBar>();
            bezBar.sphereCoords = false;
        }

        bezBar.radius = barRadius;
        activePull = true;

        stretchEdge.SetActive(true);

           /*
        if (!origRotationSet) {
            origRotation = gameObject.transform.localRotation;
            origRotationSet = true;
        }
        */
    }

    public void endPullEffect()
    {
        if (stretchEdge == null) return;

        activePull = false;

        snapRotation = gameObject.transform.localRotation;
        snapPosition = gameObject.transform.position;
        snapBack = true;
        snapTime = 0.0f;
    }

    void updatePullCurve()
    {
        
        curveBasePoints[0] = baseSphereTransform.TransformPoint(positionOnSphere);
        curveBasePoints[3] = gameObject.transform.position;

        Vector3 centerVec = baseSphereTransform.position - curveBasePoints[0];
        centerVec.Normalize();
        Vector3 nodeVec = curveBasePoints[3] - curveBasePoints[0];
        float mag = nodeVec.magnitude;
        nodeVec *= 1.0f / mag;

        curveBasePoints[1] = curveBasePoints[0] + centerVec * mag * 0.5f;
        curveBasePoints[2] = curveBasePoints[0] + nodeVec * mag * 0.5f;

        bezBar.init(curveBasePoints, origColor, origColor);

        foreach(ConnectionManager conn in innerConnections) conn.recalculateEdge();
        
    }

    void doSnapBack()
    {
        snapTime += Time.deltaTime;

        gameObject.transform.localRotation = Quaternion.Slerp(snapRotation, origRotation, snapTime * timeToSnapBack_inv);

        gameObject.transform.position = Vector3.Slerp(snapPosition, baseSphereTransform.TransformPoint(positionOnSphere), snapTime * timeToSnapBack_inv);


        if (snapTime >= timeToSnapBack)
        {
            stretchEdge.SetActive(false);
            snapBack = false;
        }

    }

    public void setSubNodeNames(List<string> list)
    {
        subNodeNames.Clear();
        subNodeNames.AddRange(list);
    }

    public List<string> getSubNodeNames()
    {
        return subNodeNames;
    }

    void OnCollisionEnter(Collision collision)
    {
        numCollisions++;
        if (material == null) material = gameObject.GetComponent<Material>();
        if (material != null) material.color = Color.white;
    }

    void OnCollisionExit(Collision collision)
    {
        numCollisions--;
        if (numCollisions <= 0)
        {
            numCollisions = 0;
            if (material != null) material.color = origColor;
        }
    }
    
    //public 

}
