using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour {

    public string nodeName = "";
    Color origColor = Color.white;
    public Material nodeMaterial = null;
    MeshRenderer meshRend = null;
    public NodeInfo nodeInfo = null;

    public MeshFilter meshFilter;
    public Mesh mesh = null;

    List<string> subNodeNames = new List<string>();

    ///List<ConnectionManager> innerConnections = new List<ConnectionManager>();
    Dictionary<string, ConnectionManager> innerConnections = new Dictionary<string, ConnectionManager>();

    int numCollisions = 0;
    bool origRotationSet = false;
    public Vector3 positionOnSphere = Vector3.zero;
    Quaternion origRotation;
    Quaternion snapRotation;
    Vector3 snapPosition;
    public Transform baseSphereTransform = null;
    GameObject stretchCurve = null;
    BezierBar bezBar;

    GameObject stretchLine = null;
    BezierLine bezLine;

    public GameObject curvePrefab;
    public GameObject linePrefab;

    bool activePull = false;
    bool snapBack = false;

    Vector3[] curveBasePoints = new Vector3[4];

    float _timeToSnapBack = 2.0f;
    float timeToSnapBack_inv = 0.5f;
    float snapTime;

    bool restrictDrawingOfEdges = false;

    public List<GameObject> outerEdgesNear = new List<GameObject>();
    public List<GameObject> outerEdgesFar = new List<GameObject>();

    public bool isSelected = false;

    public highlightState currHighlightState = highlightState.NONE;

    bool dynamicNodeColor = true;

    public void addOuterConnection(GameObject obj, bool near)
    {
        if (near) outerEdgesNear.Add(obj);
        else outerEdgesFar.Add(obj);
    }



    public void addInnerConnection(ConnectionManager conn)
    {
        if (innerConnections.ContainsKey(conn.name)) return;
        innerConnections.Add(conn.name, conn);
    }

    public void removeInnerConnection(ConnectionManager conn)
    {
        if (innerConnections.ContainsKey(conn.name)) innerConnections.Remove(conn.name);  
    }

    public void removeInnerConnections()
    {
        ConnectionManager conn;
        foreach (KeyValuePair<string, ConnectionManager> kv in innerConnections)
        {
            conn = kv.Value;
            if (conn.centerPoint != null) GameObject.Destroy(conn.centerPoint);
        }

        innerConnections.Clear();
    }

    public void hideAllInnerConnectionEdgeNodes()
    {
        foreach (KeyValuePair<string, ConnectionManager> kv in innerConnections)
        {
            kv.Value.hideEndSubNodes();
        }

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

    void createStretchObjects()
    {
        stretchCurve = (GameObject)Instantiate(curvePrefab);
        stretchCurve.name = nodeName + " [stretch curve";
        bezBar = stretchCurve.GetComponent<BezierBar>();
        bezBar.useSphericalInterpolation = false;


        stretchLine = (GameObject)Instantiate(linePrefab);
        stretchLine.name = nodeName + " [stretch line";
        bezLine = stretchLine.GetComponent<BezierLine>();
        bezLine.useSphericalInterpolation = false;
    }

    void destroyStretchObjects()
    {
        Destroy(stretchCurve);
        stretchCurve = null;
        Destroy(stretchLine);
        stretchLine = null;
    }

    public void beginPullEffect(float barRadius, bool restrictCurveRedraw)
    {
        restrictDrawingOfEdges = restrictCurveRedraw;

        if (stretchCurve == null || stretchLine == null)
        {
            createStretchObjects();
        }

        bezBar.radius = barRadius;
        bezLine.radius = barRadius*2f;
        activePull = true;

        if( restrictDrawingOfEdges )
        {
            stretchLine.SetActive(true);
            stretchCurve.SetActive(false);
        }
        else
        {
            stretchLine.SetActive(false);
            stretchCurve.SetActive(true);
        }
            
    }

    public void endPullEffect()
    {
        if (stretchCurve == null) return;

        activePull = false;

        snapRotation = gameObject.transform.localRotation;
        snapPosition = gameObject.transform.position;
        snapBack = true;
        snapTime = 0.0f;
    }

    void updatePullCurve()
    {

        if (stretchCurve == null) return;

        curveBasePoints[0] = baseSphereTransform.TransformPoint(positionOnSphere);
        curveBasePoints[3] = gameObject.transform.position;

        Vector3 centerVec = baseSphereTransform.position - curveBasePoints[0];
        centerVec.Normalize();
        Vector3 nodeVec = curveBasePoints[3] - curveBasePoints[0];
        float mag = nodeVec.magnitude;
        nodeVec *= 1.0f / mag;

        curveBasePoints[1] = curveBasePoints[0] + centerVec * mag * 0.5f;
        curveBasePoints[2] = curveBasePoints[0] + nodeVec * mag * 0.5f;

        if( restrictDrawingOfEdges ) bezLine.init(curveBasePoints, origColor, origColor);
        else bezBar.init(curveBasePoints, origColor, origColor);

        Material mat = stretchCurve.GetComponent<Renderer>().material;
        
        mat.SetFloat("_Highlight", nodeMaterial.GetFloat("_Highlight"));

        foreach (KeyValuePair<string, ConnectionManager> kv in innerConnections) kv.Value.recalculateEdge(restrictDrawingOfEdges);
        
    }

    void doSnapBack()
    {
        snapTime += Time.deltaTime;

        gameObject.transform.localRotation = Quaternion.Slerp(snapRotation, origRotation, snapTime * timeToSnapBack_inv);

        gameObject.transform.position = Vector3.Slerp(snapPosition, baseSphereTransform.TransformPoint(positionOnSphere), snapTime * timeToSnapBack_inv);

        if (snapTime >= timeToSnapBack)
        {
            stretchCurve.SetActive(false);
            snapBack = false;

            restrictDrawingOfEdges = false;
            foreach (KeyValuePair<string, ConnectionManager> kv in innerConnections) kv.Value.recalculateEdge(restrictDrawingOfEdges);

            destroyStretchObjects();
            

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

    /*
    void OnCollisionEnter(Collision collision)
    {
        numCollisions++;
        if (nodeMaterial == null) nodeMaterial = gameObject.GetComponent<Material>();
        if (nodeMaterial != null) adjustNodeColor(10f);
        //if (nodeMaterial != null) nodeMaterial.color = Color.white;
    }

    void OnCollisionExit(Collision collision)
    {
        numCollisions--;
        if (numCollisions <= 0)
        {
            numCollisions = 0;
            //if (nodeMaterial != null) nodeMaterial.color = origColor;
            if (nodeMaterial != null) adjustNodeColor(1f);
        }
    }
    */
    public void addCollision()
    {
        numCollisions++;
        if (nodeMaterial == null) nodeMaterial = gameObject.GetComponent<Material>();
        if (nodeMaterial != null) adjustNodeColor(10f);
    }

    public void subtractCollision()
    {
        numCollisions--;
        if (numCollisions <= 0)
        {
            numCollisions = 0;
            if (nodeMaterial != null) adjustNodeColor(1f);
        }
    }

    public int getNumCurvesAffectedByPulling()
    {
        return innerConnections.Count + 1;
    }

    
    public void setNearEdgeBrightness(float val)
    {
        Material mat;
        foreach (GameObject obj in outerEdgesNear)
        {
            mat = obj.GetComponent<Renderer>().material;
            mat.SetFloat("_Highlight", val);
        }

        if (outerEdgesNear.Count > 0 && dynamicNodeColor) adjustNodeColor(val);

    }

    public void setFarEdgeBrightness(float val)
    {
        Material mat;
        foreach (GameObject obj in outerEdgesFar)
        {
            mat = obj.GetComponent<Renderer>().material;
            mat.SetFloat("_Highlight", val);
        }
        
        if( outerEdgesFar.Count > 0 && dynamicNodeColor) adjustNodeColor(val);
     
    }

    public void adjustNodeColor(float val)
    {
        if (meshRend == null) meshRend = GetComponent<MeshRenderer>();
        nodeMaterial = meshRend.material;


        nodeMaterial = meshRend.material;
        nodeMaterial.SetFloat("_Highlight", val);
    }

    public void setMeshColors(Color color)
    {
        origColor = color;
        if (mesh == null) mesh = meshFilter.mesh;
        Color[] meshColors = new Color[mesh.vertexCount];

        for (int i = 0; i < meshColors.Length; i++) meshColors[i] = color;

        mesh.colors = meshColors;
    }
}
