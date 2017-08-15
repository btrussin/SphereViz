using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLoader : MonoBehaviour {

	public bool excludeIsolatedNodes = false;
	public bool performForceDirectedLayout = false;
    public int numForceIterations = 300;
    public float radius = 2.5f;

	public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject bezierPrefab;
    public GameObject bSplinePrefab;

    public GameObject projSphere;

    public bool useBezierBars = true;
    public bool useBSplineBars = true;
    public bool useSLERP = false;

    public float barRadiusScale = 1.0f;     // aesthetically appealing: 0.3
    public float pointScaleFactor = 1.0f;   // aesthetically appealing: 1.5

    // populated by the derived class
    protected Dictionary<string, NodeInfo> nodeMap = new Dictionary<string, NodeInfo>();
    protected List<EdgeInfo> edgeList = new List<EdgeInfo>();

    // populated by this class
	protected Dictionary<string, GroupInfo> groupMap = new Dictionary<string, GroupInfo>();
	protected Dictionary<string, Color> groupColorMap = new Dictionary<string, Color>();

    protected Dictionary<string, List<GameObject> > subElementObjectMap = new Dictionary<string, List<GameObject>>();
    protected List<GameObject> subElementConnectionList = new List<GameObject>();


    protected Dictionary<string, NodeManager> selectedNodeMap = new Dictionary<string, NodeManager>();
    protected Dictionary<string, int> selectedNodeNumConnections = new Dictionary<string, int>();

    protected Dictionary<string, Vector3> subNodePositionMap = new Dictionary<string, Vector3>();

    public int randomColorSeed = 8;

    float C1 = 0.1f;
    float C2 = 0.05f;
    float C3 = 0.05f;
    float C4 = 0.005f;

    public bool interpolateSpherical = true;

    public bool innerNodeHornLayout = true;

    // for best results, this value should be <= 0.04
    public float gravityAmt = 0.01f;

    public float splineGroupWeight = 0.95f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        
    }

    public void testMovement()
    {
        float rt = 0f, fo = 0f;
        if (Input.GetKeyDown(KeyCode.A)) rt -= 0.1f;
        if (Input.GetKeyDown(KeyCode.D)) rt += 0.1f;
        if (Input.GetKeyDown(KeyCode.W)) fo += 0.1f;
        if (Input.GetKeyDown(KeyCode.S)) fo -= 0.1f;

        if (rt != 0f || fo != 0f)
        {
            Vector3 pos = projSphere.transform.position;
            pos = pos + rt * projSphere.transform.right + fo * projSphere.transform.forward;
            projSphere.transform.position = pos;
        }
    }

	public void loadData()
	{
		// populate the nodes (derived class should do this and populate the 2D points)
		// populate the edges (derived class should do this and populate the 2D points)

		// cull isolated nodes (is applicable)
		if( excludeIsolatedNodes ) cullIsolatedNodes();


        // populate the groupMap
        populateGroupMap();


        clusterByGroups();
        //randomizeNodePoints();


        // do the force-directed layout
        if ( performForceDirectedLayout ) doForceDirLayout();




		
		// populate the groupColorMap
		populateColorMap();

		// normalize the points and project them onto 3D object
		normalizePointsForNodesAndGroups();
		projectPointsForNodesAndGroups();




		populatePts();
		populateEdges();
	}

    private void clusterByGroups()
    {
        //protected Dictionary<string, GroupInfo> groupMap = new Dictionary<string, GroupInfo>();
        int N = nodeMap.Count; // number of nodes
        int M = groupMap.Count; // number of groups

        GroupInfo[] tmpGrpInfo = new GroupInfo[M];

        int i = 0;
        foreach(GroupInfo grp in groupMap.Values) tmpGrpInfo[i++] = grp;

        SortBySize(tmpGrpInfo, 0, M - 1);

        float R = 1.0f;

        float[] portionVals = new float[M];
        float[] radiusVals = new float[M];
        float[] angleVals = new float[M];

        i = 0;
        GroupInfo currGrp;
        float A, B, C;
        float sumAngles = 0.0f;
        angleVals[0] = angleVals[1] = 0.0f;

        float hiVal = 20.0f;
        float loVal = 0.01f;
        float midVal = (hiVal + loVal) * 0.5f;


        for (i = 0; i < tmpGrpInfo.Length; i++)
        {
            currGrp = tmpGrpInfo[i];
            
            portionVals[i] = (float)currGrp.nodeList.Count / (float)N;
            radiusVals[i] = Mathf.Sqrt(portionVals[i]) * R;
        }

        for(int z = 0; z < 100; z++ )
        {

            for (i = 1; i < tmpGrpInfo.Length; i++)
            {
                currGrp = tmpGrpInfo[i];
                radiusVals[i] = Mathf.Sqrt(portionVals[i]) * R * midVal;
            }

            sumAngles = 0.0f;
            for (i = 2; i < tmpGrpInfo.Length; i++)
            {
                A = radiusVals[0] + radiusVals[i - 1];
                B = radiusVals[0] + radiusVals[i];
                C = radiusVals[i - 1] + radiusVals[i];

                angleVals[i] = Mathf.Acos((A * A + B * B - C * C) / (2.0f * A * B));
                sumAngles += angleVals[i];
            }

            if (sumAngles > 2.0f * Mathf.PI)
            {
                hiVal = midVal;
            }
            else if( sumAngles < 1.9f * Mathf.PI )
            {
                loVal = midVal;
            }
            else
            {
                break;
            }

            midVal = (hiVal + loVal) * 0.5f;
        }

        Debug.Log("Final Mid Val: " + midVal);
       

        tmpGrpInfo[0].center2 = Vector2.zero;

        float currAngle = 0.0f;
        Vector2 currVec;
        for (i = 0; i < tmpGrpInfo.Length; i++)
        {
            currGrp = tmpGrpInfo[i];

            currAngle += angleVals[i];

            if (i == 0) currGrp.center2 = Vector2.zero;
            else currGrp.center2 = new Vector2(Mathf.Cos(currAngle), Mathf.Sin(currAngle)) * (radiusVals[0] + radiusVals[i]);

            float partRadius = radiusVals[i] * 0.8f;
            float angleInc = 2f * Mathf.PI / (float)currGrp.nodeList.Count;
            float angle = 0f;
            for( int j = 0; j < currGrp.nodeList.Count; j++ )
            {
                currVec = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                //currGrp.nodeList[j].position2 = currGrp.center2 + currVec * partRadius * Random.value * 0.5f;
                currGrp.nodeList[j].position2 = currGrp.center2 + currVec * partRadius * (0.5f + Random.value * 0.5f);
                angle += angleInc;
            }

        }

    }

	private void cullIsolatedNodes()
	{
		HashSet<string> cullList = new HashSet<string>();

			foreach( string nodeName in nodeMap.Keys ) cullList.Add(nodeName);

			foreach( EdgeInfo edgeInfo in edgeList )
			{
				if( cullList.Contains(edgeInfo.startNode.name) ) cullList.Remove(edgeInfo.startNode.name);
				if( cullList.Contains(edgeInfo.endNode.name) ) cullList.Remove(edgeInfo.endNode.name);
			}

			foreach( string nodeName in cullList ) nodeMap.Remove(nodeName);
	}

	private void populateGroupMap()
	{
		NodeInfo currNode;
		GroupInfo currGroup;
		foreach( KeyValuePair<string, NodeInfo> kv in nodeMap )
		{
			currNode = kv.Value;
			
			if( !groupMap.TryGetValue(currNode.groupName, out currGroup ) )
			{
				currGroup = new GroupInfo();
				currGroup.name = currNode.groupName;
				currGroup.nodeList = new List<NodeInfo>();
                groupMap.Add(currGroup.name, currGroup);
			}

			currGroup.nodeList.Add(currNode);
		}
	}

	private void populateColorMap()
	{
		Color[] colors = ColorUtils.getColorPalette();
    	ColorUtils.randomizeColorPalette(colors, randomColorSeed);
    	Color currColor;
    	int idx = 0;
    	int maxIdx = colors.Length;

    	foreach(KeyValuePair<string, GroupInfo> kv in groupMap)
    	{
    		currColor = colors[idx % maxIdx];
    		groupColorMap.Add(kv.Key, currColor);
    		idx++;
    	}
	}

	private void normalizePointsForNodesAndGroups()
	{
		float xMin = 1000000000.0f;
        float xMax = -100000000.0f;
        float yMin = 1000000000.0f;
        float yMax = -100000000.0f;
        Vector2 v;
     	NodeInfo currNode;

        foreach( KeyValuePair<string, NodeInfo> kv in nodeMap )
        {
        	currNode = kv.Value;
        	v.x = currNode.position2.x;
            v.y = currNode.position2.y;

            if( v.x < xMin ) xMin = v.x;
            if( v.y < yMin ) yMin = v.y;
            if( v.x > xMax ) xMax = v.x;
            if( v.y > yMax ) yMax = v.y;
        }

        float xRange = xMax - xMin;
        float yRange = yMax - yMin;

        foreach( KeyValuePair<string, NodeInfo> kv in nodeMap )
        {
        	currNode = kv.Value;
            v.x = ((currNode.position2.x-xMin)/xRange)*2.0f - 1.0f;
            v.y = ((currNode.position2.y-yMin)/yRange)*2.0f - 1.0f;

            currNode.position2 = v;
        }

        GroupInfo currGroup;
        foreach( KeyValuePair<string, GroupInfo> kv in groupMap )
        {
        	currGroup = kv.Value;
        	Vector2 centerVec = Vector2.zero;

        	foreach(NodeInfo node in currGroup.nodeList)
        	{
        		centerVec += node.position2;
        	}

        	float listLenInv = 1.0f / (float)currGroup.nodeList.Count;
        	centerVec.x *= listLenInv;
        	centerVec.y *= listLenInv;

        	currGroup.center2 = centerVec;
        }


	}

	private void projectPointsForNodesAndGroups()
	{
		NodeInfo currNode;
        foreach( KeyValuePair<string, NodeInfo> kv in nodeMap )
        {
        	currNode = kv.Value;
        	currNode.position3 = getProjectedPoint(currNode.position2);
            currNode.sphereCoords = get3DPointProjectionSphereCoords(currNode.position2);
        }

        GroupInfo currGroup;
        foreach( KeyValuePair<string, GroupInfo> kv in groupMap )
        {
        	currGroup = kv.Value;
        	currGroup.center3 = getProjectedPoint(currGroup.center2);
            currGroup.sphereCoords = get3DPointProjectionSphereCoords(currGroup.center2);
        }


	}

	private Vector3 getProjectedPoint(Vector2 pt)
	{
		return get3DPointProjectionSphere(pt, radius);
	}

	private Vector3 get3DPointProjectionSphere(Vector2 v, float r)
    {
        Vector3 result = new Vector3(r, 0.0f, 0.0f);

        float horizontalAngle = v.x * 90.0f;
        float verticalAngle = v.y * 60.0f;

        Quaternion rotation = Quaternion.Euler(0.0f, horizontalAngle, verticalAngle);

        result = rotation * result;
        return result;
    }

    private Vector3 get3DPointProjectionSphereCoords(Vector3 v)
    {
        return get3DPointProjectionSphereCoords(v, radius);
    }

    private Vector3 get3DPointProjectionSphereCoords(Vector3 v, float r)
    {
        Vector3 result = new Vector3(v.x * 90.0f, v.y * 60.0f, r);
        return result;
    }

    Vector2 forceDirCenter = Vector2.zero;

    private float repelForce(float dist)
    {
        if (dist == 0.0f) return 10000.0f;
        return C3 / (dist * dist);
    }

    private float attractForce(float dist)
    {
        return C1 * Mathf.Log(dist / C2);
    }

    void recalcPositions()
    {

        NodeInfo outerInfo, innerInfo;
        Vector2 tVec;
        float dist;

        // calculate the repel forces for each node
        foreach (KeyValuePair<string, NodeInfo> outerEntry in nodeMap)
        {
            outerInfo = outerEntry.Value;
            outerInfo.dir = Vector3.zero;

            foreach (KeyValuePair<string, NodeInfo> innerEntry in nodeMap)
            {
                if (outerEntry.Key.Equals(innerEntry.Key)) continue;

                innerInfo = innerEntry.Value;

                tVec = outerInfo.position2 - innerInfo.position2;
                if (tVec.sqrMagnitude == 0.0f) tVec = new Vector2(1.0f, 1.0f);

                dist = tVec.magnitude;
                outerInfo.dir += tVec / dist * repelForce(dist);
            }
        }


        // calculate the attract forces for each node
        foreach (EdgeInfo link in edgeList)
        {
            outerInfo = link.startNode;
            innerInfo = link.endNode;

            // dir from inner to outer
            tVec = outerInfo.position2 - innerInfo.position2;

            if (tVec.sqrMagnitude == 0.0f) tVec = new Vector3(0.01f, 0.01f, 1.01f);

            dist = tVec.magnitude;

            tVec = tVec / dist * attractForce(dist) * link.forceValue;

            outerInfo.dir -= tVec;
            innerInfo.dir += tVec;
        }

        Vector2 tPos;
        Vector2 gravDir;
        foreach (KeyValuePair<string, NodeInfo> entry in nodeMap)
        {
            //gravDir = sphereCenter - entry.Value.pos3d;
            gravDir = forceDirCenter - entry.Value.position2;
            gravDir.Normalize();
            //tPos = entry.Value.pos3d + entry.Value.dir * C4 + gravDir * gravityAmt;
            tPos = entry.Value.position2 + entry.Value.dir * C4 + gravDir * gravityAmt;

            entry.Value.position2 = tPos;

            //gravDir = sphereCenter - entry.Value.pos3d;
            gravDir = forceDirCenter - tPos;
            dist = gravDir.magnitude;
        }
    }

    private void randomizeNodePoints()
    {
        Random.InitState(45);
        foreach (NodeInfo info in nodeMap.Values)
        {
            info.position2 = Random.insideUnitCircle;
        }
    }


    private void doForceDirLayout()
    { 
       
        for (int i = 0; i < numForceIterations; i++) recalcPositions();

    }

    private void populatePts()
    {
        Vector3 ptScale = Vector3.one * radius * 2f / 125f * pointScaleFactor;
        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            GameObject point = (GameObject)Instantiate(nodePrefab);
            point.name = "Node: " + kv.Value.name;
            point.transform.position = kv.Value.position3 + projSphere.transform.position;
            point.transform.localScale = ptScale;

            MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
            meshRend.material.color = groupColorMap[kv.Value.groupName];

            NodeManager manager = point.GetComponent<NodeManager>();
            manager.nodeName = kv.Value.name;
            manager.setSubNodeNames(kv.Value.subElements);
            manager.nodeInfo = kv.Value;

            point.transform.SetParent(projSphere.transform);
        }
    }


    public void populateSubNodes(NodeManager nodeManager)
    {
        if (nodeManager == null) return;

        NodeInfo nodeInfo = nodeManager.nodeInfo;

        if (nodeInfo == null) return;

        if (subElementObjectMap.ContainsKey(nodeInfo.name) ) return;

        Vector3 ptScale = Vector3.one * radius * 2f / 125f;

        Vector3 upDir = nodeManager.gameObject.transform.position - projSphere.transform.position;
        upDir.Normalize();

        Vector3 rightVec = new Vector3(1f, 0f, 0f);
        if( Vector3.Dot(upDir, rightVec) > 0.99f ) rightVec = new Vector3(0f, 0f, 1f);
        rightVec.Normalize();

        Vector3 forVec = Vector3.Cross(upDir, rightVec);
        forVec.Normalize();

        rightVec = Vector3.Cross(forVec, upDir);
        rightVec.Normalize();

        List<GameObject> gObjList = new List<GameObject>();

        float radAngle = 2f * Mathf.PI / (float)nodeInfo.subElements.Count;
        float currAngle = 0f;

        Vector3 center = nodeManager.gameObject.transform.position - upDir * radius * 0.15f;
        float c, s;

        float barRadius = radius / 125.0f * barRadiusScale;

        Vector3[] basePoints = new Vector3[4];

        basePoints[0] = nodeManager.gameObject.transform.position;
        if(innerNodeHornLayout) basePoints[1] = center * 0.25f + basePoints[0] * 0.75f;
        else basePoints[1] = (center + basePoints[0]) * 0.5f;

        foreach (string currName in nodeInfo.subElements)
            {
            c = Mathf.Cos(currAngle);
            s = Mathf.Sin(currAngle);

            GameObject point = (GameObject)Instantiate(nodePrefab);
            point.name = "Sub: " + currName;

            basePoints[3] = center + (rightVec * c + forVec * s) * 0.05f;
            if( innerNodeHornLayout ) basePoints[2] = center * 0.75f + basePoints[3] * 0.25f;
            else basePoints[2] = basePoints[3] + upDir * radius * 0.075f;

            point.transform.position = basePoints[3];
            point.transform.localScale = ptScale;

            Color color = groupColorMap[nodeInfo.groupName];

            MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
            meshRend.material.color = color;

            Destroy(point.GetComponent<NodeManager>());


            GameObject edgeObj = (GameObject)Instantiate(bezierPrefab);
            BezierBar bezBar = edgeObj.GetComponent<BezierBar>();
            bezBar.radius = barRadius;
            bezBar.sphereCoords = false;
            bezBar.init(basePoints, color, color);
            //edgeObj.transform.position = projSphere.transform.position;

            subNodePositionMap.Add(getSubNodeKey(nodeInfo, currName), basePoints[3]);

            gObjList.Add(point);
            gObjList.Add(edgeObj);

            currAngle += radAngle;

            point.transform.SetParent(projSphere.transform);
            edgeObj.transform.SetParent(projSphere.transform);
        }

        subElementObjectMap.Add(nodeInfo.name, gObjList);

    }

    public void populateSubNodeConnections(NodeManager nodeManagerA, NodeManager nodeManagerB)
    {
        
        if (nodeManagerA == null || nodeManagerB == null) return;
        NodeInfo infoA = nodeManagerA.nodeInfo;
        NodeInfo infoB = nodeManagerB.nodeInfo;
        if (infoA == null || infoB == null) return;

        foreach (GameObject obj in subElementConnectionList) GameObject.Destroy(obj);
        subElementConnectionList.Clear();

        Vector3[] ctrlPts = new Vector3[4];
        Vector3 tmpVec;
        string keyA, keyB;
        Vector3 sphereCenter = projSphere.transform.position;

        Color colorA = groupColorMap[infoA.groupName];
        Color colorB = groupColorMap[infoB.groupName];

        float barRadius = radius / 125.0f * barRadiusScale;

        for ( int i = 0; i < infoA.subElements.Count; i++ )
        {
            for (int j = 0; j < infoB.subElements.Count; j++)
            {
                if( infoA.subElements[i].Equals(infoB.subElements[j]) )
                {
                    keyA = getSubNodeKey(infoA, i);
                    keyB = getSubNodeKey(infoB, j);

                    if( subNodePositionMap.ContainsKey(keyA) && subNodePositionMap.ContainsKey(keyB) )
                    {
                        ctrlPts[0] = subNodePositionMap[keyA];
                        ctrlPts[3] = subNodePositionMap[keyB];

                        tmpVec = sphereCenter - ctrlPts[0];
                        ctrlPts[1] = ctrlPts[0] + tmpVec * radius * 0.3f;
                        tmpVec = sphereCenter - ctrlPts[3];
                        ctrlPts[2] = ctrlPts[3] + tmpVec * radius * 0.3f;


                        
                        GameObject edgeObj = (GameObject)Instantiate(bezierPrefab);
                        BezierBar bezBar = edgeObj.GetComponent<BezierBar>();
                        bezBar.radius = barRadius;
                        bezBar.sphereCoords = false;
                        bezBar.init(ctrlPts, colorA, colorB);

                        subElementConnectionList.Add(edgeObj);

                        edgeObj.transform.SetParent(projSphere.transform);

                    }
                         
                    j += infoB.subElements.Count;
                }
            }
        }
    }

    public void removeSubNodes(NodeManager nodeManager)
    {
        if (nodeManager == null) return;
        NodeInfo nodeInfo = nodeManager.nodeInfo;
        List<GameObject> gObjList;

        string tmpKey;
        foreach (string currName in nodeInfo.subElements)
        {
            tmpKey = getSubNodeKey(nodeInfo, currName);
            if (subNodePositionMap.ContainsKey(tmpKey)) subNodePositionMap.Remove(tmpKey);
        }

        if ( subElementObjectMap.TryGetValue(nodeInfo.name, out gObjList))
        {
            foreach (GameObject go in gObjList) GameObject.Destroy(go);

            gObjList.Clear();

            subElementObjectMap.Remove(nodeInfo.name);
        }
    }

    public string getSubNodeKey(NodeInfo nodeInfo, string subName)
    {
        return nodeInfo.name + "|" + subName;
    }

    public string getSubNodeKey(NodeInfo nodeInfo, int idx)
    {
        if (idx >= nodeInfo.subElements.Count) return "";
        return getSubNodeKey(nodeInfo, nodeInfo.subElements[idx]);
    }

    private void populateEdges()
    {
        Vector3[] basePts = new Vector3[4];
        
        Vector3[] pts;
        Vector3 tVec1, tVec2;

        Color c0;
        Color c1;

        float barRadius = radius / 125.0f * barRadiusScale;

        GroupInfo fromGroup;
        GroupInfo toGroup;

        float groupWeight = splineGroupWeight;
        float nodeWeight = 1.0f - groupWeight;

        foreach (EdgeInfo edge in edgeList)
        {

            c0 = groupColorMap[edge.startNode.groupName];
            c1 = groupColorMap[edge.endNode.groupName];

            if ( !useBSplineBars )
            {
            	basePts[0] = edge.startNode.position3;
            	basePts[basePts.Length-1] = edge.endNode.position3;

            	basePts[1] = basePts[0] * 2.0f;
            	basePts[basePts.Length - 2] = basePts[basePts.Length - 1] * 2.0f;
            }

            if( useBSplineBars )
            {

            	if(edge.isSameGroup())
            	{
            		basePts = new Vector3[4];

                    if( interpolateSpherical )
                    {
                        basePts[0] = edge.startNode.sphereCoords;
                        basePts[1] = basePts[0];
                        basePts[1].z *= 1.1f;
                        
                        basePts[3] = basePts[2] = edge.endNode.sphereCoords;
                        basePts[2].z *= 1.1f;
                    }
                    else
                    {
                        basePts[0] = edge.startNode.position3;
                        basePts[1] = basePts[0] * 1.1f;
                        
                        basePts[3] = edge.endNode.position3;
                        basePts[2] = basePts[3] * 1.1f;

                        
                    }

                    GameObject edgeObj = (GameObject)Instantiate(bezierPrefab);
                    BezierBar bezBar = edgeObj.GetComponent<BezierBar>();
                    bezBar.radius = barRadius;
                    bezBar.sphereCoords = interpolateSpherical;
                    bezBar.init(basePts, c0, c1);
                    edgeObj.transform.position = projSphere.transform.position;

                    edgeObj.transform.SetParent(projSphere.transform);
                }
				else
				{
					fromGroup = groupMap[edge.startNode.groupName];
					toGroup = groupMap[edge.endNode.groupName];

                    if( interpolateSpherical )
                    {
                        tVec1 = fromGroup.sphereCoords;
                        tVec2 = toGroup.sphereCoords;

                        basePts = new Vector3[7];

                        basePts[0] = edge.startNode.sphereCoords;  // start point
                        basePts[6] = edge.endNode.sphereCoords; // end point

                        //basePts[1] = basePts[2] = tVec1;
                        basePts[1] = basePts[2] = tVec1 * groupWeight + basePts[0] * nodeWeight;
                        basePts[1].z *= 1.1f;  // group1 node radius 1.1*radius
                        basePts[2].z *= 1.4f;  // group1 node radius 1.4*radius

                        //basePts[4] = basePts[5] = tVec2;
                        basePts[4] = basePts[5] = tVec2 * groupWeight + basePts[6] * nodeWeight;
                        basePts[4].z *= 1.4f;  // group2 node radius 1.4*radius
                        basePts[5].z *= 1.1f;  // group2 node radius 1.1*radius

                        basePts[3] = (basePts[4] + basePts[2]) * 0.5f;
                    }
                    else
                    {
                        tVec1 = fromGroup.center3;
                        tVec2 = toGroup.center3;

                        basePts = new Vector3[7];

                        basePts[0] = edge.startNode.position3;
                        basePts[6] = edge.endNode.position3;

                        basePts[1] = tVec1 * 1.1f;
                        basePts[2] = tVec1 * 1.4f;

                        basePts[4] = tVec2 * 1.4f;
                        basePts[5] = tVec2 * 1.1f;

                        basePts[3] = (basePts[4] + basePts[2]) * 0.5f;

                    }

					


					GameObject edgeObj = (GameObject)Instantiate(bSplinePrefab);
                	BasisSpline bspline = edgeObj.GetComponent<BasisSpline>();
                    bspline.radius = barRadius;
                    bspline.useSphericalInterpolation = interpolateSpherical;
                	bspline.init(basePts, c0, c1);
                    edgeObj.transform.position = projSphere.transform.position;

                    edgeObj.transform.SetParent(projSphere.transform);
                }                
            }
            else if(useBezierBars)
            {
                GameObject edgeObj = (GameObject)Instantiate(bezierPrefab);
                BezierBar bezBar = edgeObj.GetComponent<BezierBar>();
                bezBar.radius = barRadius;
                bezBar.sphereCoords = false;
                bezBar.init(basePts, c0, c1);
                edgeObj.transform.position = projSphere.transform.position;

                edgeObj.transform.SetParent(projSphere.transform);
            }
            else
            {
                GameObject edgeObj = (GameObject)Instantiate(edgePrefab);
                LineRenderer rend = edgeObj.GetComponent<LineRenderer>();

                pts = Utils.getBezierPoints(basePts, 100);
                rend.SetPositions(pts);
                rend.startColor = c0;
                rend.endColor = c1;
                edgeObj.transform.position = projSphere.transform.position;

                edgeObj.transform.SetParent(projSphere.transform);
            }

        }

    }

    protected string getEdgeName(NodeInfo info1, NodeInfo info2)
    {
        if (info1.name.CompareTo(info2.name) < 0) return info1.name + " - " + info2.name;
        else return info2.name + " - " + info1.name;
    }

    void swapElements(GroupInfo[] groups, int i, int j)
    {
        GroupInfo tGroup = groups[i];

        groups[i] = groups[j];

        groups[j] = tGroup;
        return;
    }

    void SortBySize(GroupInfo[] groups, int beginIdx, int endIdx)
    {
        int idxDist = endIdx - beginIdx;
        if (idxDist < 1) return;
        else if (idxDist == 1)
        {
            if (groups[beginIdx].nodeList.Count < groups[endIdx].nodeList.Count) swapElements(groups, beginIdx, endIdx);
            return;
        }

        int midIdx = (beginIdx + endIdx) / 2;
        int countVal = groups[midIdx].nodeList.Count;

        swapElements(groups, midIdx, endIdx);


        int s = beginIdx;
        int e = endIdx - 1;

        while (s < e)
        {
            if (groups[s].nodeList.Count <= countVal)
            {
                swapElements(groups, s, e);
                e--;
            }
            else s++;
        }

        int divider = midIdx;

        for (int i = beginIdx; i < endIdx; i++)
        {
            if (groups[i].nodeList.Count <= countVal)
            {
                divider = i;
                swapElements(groups, divider, endIdx);
                break;
            }
        }

        SortBySize(groups, beginIdx, divider - 1);
        SortBySize(groups, divider + 1, endIdx);
    }




}


public class NodeInfo
{
    public string name = "";
    public string groupName = "";
    public Vector2 position2 = Vector2.zero;
    public Vector3 position3 = Vector3.zero;
    public Vector3 sphereCoords = Vector3.zero;

    public Vector2 dir = Vector2.zero;

	public List<string> subElements = new List<string>();
}


public class GroupInfo
{
    public string name = "";
    public List<NodeInfo> nodeList = null;
    public Vector2 center2 = Vector2.zero;
    public Vector3 center3 = Vector3.zero;
    public Vector3 sphereCoords = Vector3.zero;
}

public class EdgeInfo
{
	public NodeInfo startNode = null;
	public NodeInfo endNode = null;
    public float forceValue = 0.0f;

	private bool __isSameGroup = false;
	private bool __sameGrpSet = false;
	public bool isSameGroup()
	{
		if(__sameGrpSet) return __isSameGroup;
		else
		{
			__sameGrpSet = true;
			__isSameGroup = startNode.groupName.Equals(endNode.groupName);
			return isSameGroup();
		}
	}

	public bool Equals(EdgeInfo otherInfo)
	{
		return (startNode.name.Equals(otherInfo.startNode.name) && endNode.name.Equals(otherInfo.endNode.name)) || 
			(startNode.name.Equals(otherInfo.endNode.name) && endNode.name.Equals(otherInfo.startNode.name));
	}
}