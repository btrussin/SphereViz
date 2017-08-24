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

    int numCollisions = 0;

    public Vector3 positionOnSphere = Vector3.zero;
    public Transform baseSphereTransform = null;
    GameObject stretchEdge = null;
    BezierBar bezBar;

    public GameObject curvePrefab;

    bool activePull = false;

    Vector3[] curveBasePoints = new Vector3[4];

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

    }
	
	// Update is called once per frame
	void Update () {
        if (activePull)
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

        }
	}

    public void beginPullEffect(float barRadius)
    {
        if(stretchEdge == null)
        {
            stretchEdge = (GameObject)Instantiate(curvePrefab);
            bezBar = stretchEdge.GetComponent<BezierBar>();
            bezBar.sphereCoords = false;
        }

        bezBar.radius = barRadius;
        activePull = true;
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
