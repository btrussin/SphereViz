using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum highlightState
{
    NONE,
    ONE_HOP,
    NEAR,
    FAR
}

public enum SubNodeDisplayType
{
    BLOOM,
    TABLE,
    INNER_SPHERE
}

public enum NodeClusterMode
{
    RANDOM,
    SQUARE_CLUSTER,
    CIRCLE_CLUSTER,
    FORCE_DIRECTED
}

public class DataObjectManager : MonoBehaviour
{
    public NodeClusterMode nodeClusterMode;
    public DataLoader dataLoader;

    public bool excludeIsolatedNodes = false;
    public int numForceIterations = 300;
    public float mainObjRadius = 2.5f;

    public float timeToSnapBack = 2.0f;

    public GameObject nodePrefab;
    public GameObject groupNodePrefab;
    public GameObject innerNodePrefab;
    public GameObject edgePrefab;
    public GameObject bezierPrefab;
    public GameObject bSplinePrefab;

    public GameObject bezierCollPrefab;

    public GameObject popupTextPrefab;

    public GameObject projSphere;
    public Renderer projSphereRenderer;
    public GameObject innerSphere;
    public GameObject projSphereCenterReference;

    //public bool useBezierBars = true;
    //public bool useBSplineBars = true;
    public bool useSLERP = false;

    public float barRadiusScale   = 1.0f; // aesthetically appealing: 0.3
    public float pointScaleFactor = 1.0f; // aesthetically appealing: 1.5

    public SliderManager slider_barRadius;
    public SliderManager slider_nodeScale;
    public SliderManager slider_innerConnDist;
    public SliderManager slider_outerConnDist;
    public SliderManager slider_gazeAngle;
    public SliderManager slider_collSize;
    public SliderManager slider_sphereViz;
    public SliderManager slider_edgeThin;

    public ViveController rightController;
    public ViveController leftController;

    public float edgeDist = 0.4f; // original: 0.4f
    public float innerGroupEdgeDist = 0.1f; // original: 0.1f

    public highlightState currHighlightState = highlightState.ONE_HOP;

    // populated by the derived class
    protected Dictionary<string, NodeInfo> nodeMap = new Dictionary<string, NodeInfo>();
    protected List<EdgeInfo> edgeList = new List<EdgeInfo>();

    // populated by this class
    protected Dictionary<string, GroupInfo> groupMap = new Dictionary<string, GroupInfo>();
    protected Dictionary<string, Color> groupColorMap = new Dictionary<string, Color>();

    protected Dictionary<string, List<GameObject>> subElementObjectMap = new Dictionary<string, List<GameObject>>();
    protected Dictionary<string, GameObject> subNodePositionMap = new Dictionary<string, GameObject>();


    protected Dictionary<string, NodeManager> selectedNodeMap = new Dictionary<string, NodeManager>();
    protected Dictionary<string, NodeManager> nodeManagerMap = new Dictionary<string, NodeManager>();

    protected List<GameObject> outerEdgeList = new List<GameObject>();
    protected List<GameObject> nodeList = new List<GameObject>();

    public int randomColorSeed = 8;

    float C1 = 0.1f;
    float C2 = 0.05f;
    float C3 = 0.05f;
    float C4 = 0.005f;

    public bool interpolateSpherical = true;

    public bool innerNodeHornLayout = true;

    public bool innerConnectionsStraightLines = false;

    // for best results, this value should be <= 0.04
    public float gravityAmt = 0.01f;

    public float splineGroupWeight = 0.95f;

    public int maxCurvesUpdatedPerFrame = 10;
    bool activesUpdateEdges = false;

    float highlightBrightness = 1f;
    float dimmedBrightness = 0.1f;

    public float gazeAngle = 8f;
    public float contrColliderScale = 1f;
    public float sphereVisibility = 0.085f;
    public float edgeThinAmount = 0.1f;

    public SubNodeDisplayType subNodeDisplayType = SubNodeDisplayType.BLOOM;

    public bool invertFarEdgeGradient = true;

    public bool useGroupNodes = true;

    [Range(60f, 170f)]
    public float projHorizontalAngle = 90.0f;
    [Range(20f, 80f)]
    public float projVerticalAngle = 60.0f;

    public bool doHighlightEdgeThinning = false;

    // Use this for initialization
    void Start()
    {
        projSphere.transform.localScale = Vector3.one * mainObjRadius;

        loadData();


        updateHighlightState(highlightState.ONE_HOP);




        //testing();
    }

    void testing()
    {
        Vector3 v1 = Vector3.one;

        Quaternion q = Quaternion.Euler(30f, 40f, 50f);

        Vector3 w = q * v1;

        Debug.Log("Target: " + w.x + "," + w.y + "," + w.z);

        Quaternion qx = Quaternion.Euler(30f, 0f, 0f); // #2
        Quaternion qy = Quaternion.Euler(0f, 40f, 0f); // #3
        Quaternion qz = Quaternion.Euler(0f, 0f, 50f); // #1

        w = qy * qx * qz * v1;

        Debug.Log("Result: " + w.x + "," + w.y + "," + w.z);
    }

    float currVis = 0.2f;

    // Update is called once per frame
    void Update()
    {

        if (doFirstPosition)
        {
            recenterProjectionSphere();
            doFirstPosition = false;
        }

        if (activesUpdateEdges) recalcAllEdges();

        if (recenterSphereAnim) recenterAnimation();
        
    }

    protected void setDefaultParameterValues()
    {
        slider_innerConnDist.suggestValue(innerGroupEdgeDist);
        slider_outerConnDist.suggestValue(edgeDist);

        slider_nodeScale.suggestValue(pointScaleFactor);
        slider_barRadius.suggestValue(barRadiusScale);

        slider_gazeAngle.suggestValue(gazeAngle);

        slider_collSize.suggestValue(contrColliderScale);

        slider_sphereViz.suggestValue(sphereVisibility);

        slider_edgeThin.suggestValue(edgeThinAmount);

        updateParameterValues();
    }

    protected void updateParameterValues()
    {
        innerGroupEdgeDist = slider_innerConnDist.getValue();
        edgeDist = slider_outerConnDist.getValue();
        pointScaleFactor = slider_nodeScale.getValue();
        barRadiusScale = slider_barRadius.getValue();
        gazeAngle = slider_gazeAngle.getValue();
        contrColliderScale = slider_collSize.getValue();
        sphereVisibility = slider_sphereViz.getValue();
        edgeThinAmount = slider_edgeThin.getValue();
    }

    public void updateGazeFactors()
    {
        updateParameterValues();

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();
        if (gazeScript != null) gazeScript.gazeFactor = Mathf.Cos(gazeAngle / 180f * Mathf.PI);

    }

    public void updateControllerColliderScale()
    {
        updateParameterValues();

        float colliderScale = 0.027f * contrColliderScale + 0.003f;
        float meshScale = 0.063f * contrColliderScale + 0.007f;

        //Debug.Log("Controller Collider Scale: " + contrColliderScale);

        leftController.setColliderValues(colliderScale, meshScale);
        rightController.setColliderValues(colliderScale, meshScale);

    }



    // new variables
    bool doFirstPosition = true;

    bool recenterSphereAnim = false;

    Quaternion prevSphereRotation;
    Quaternion targetSphereRotation;

    Vector3 prevSpherePosition;
    Vector3 targetSpherePosition;

    public int numFramesRecenter = 50;
    int currAnimationFrame = 0;
 
    float[] smoothVals = new float[1];

 
    void recenterAnimation()
    {
        // Recenter so that the sphere is a little farther from the user
        projSphere.transform.rotation = Quaternion.Lerp(prevSphereRotation, targetSphereRotation, smoothVals[currAnimationFrame]);
        projSphere.transform.position = Vector3.Lerp(prevSpherePosition, targetSpherePosition, smoothVals[currAnimationFrame]);

        currAnimationFrame++;

        if(currAnimationFrame >= numFramesRecenter - 1)
        {
            recenterSphereAnim = false;
        }

    }


    public GameObject dummyCameraObject;

    public void recenterProjectionSphere()
    {

        if (smoothVals.Length != numFramesRecenter)
        {
            float[] baseVals = { 0f, 0.05f, 0.95f, 1f };
            smoothVals = Utils.getBezierFloats(baseVals, numFramesRecenter);
        }


        Vector3 tForward = Camera.main.transform.forward;
        tForward.y = 0f;
        tForward.Normalize();

        dummyCameraObject.transform.right = tForward;


        prevSphereRotation = projSphere.transform.rotation;
        targetSphereRotation = dummyCameraObject.transform.rotation;

        dummyCameraObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

       
        prevSpherePosition = projSphere.transform.position;
        targetSpherePosition = Camera.main.transform.position + tForward * 0.3f;

        currAnimationFrame = 0;
        recenterSphereAnim = true;
        
    }

    void updateHighlightNodesEdges()
    {
        NodeManager currNodeManager;
        BaseCurve baseCurve;
        if (currHighlightState == highlightState.NONE || (currHighlightState == highlightState.ONE_HOP && selectedNodeMap.Count == 0) )
        {
            // highlight all nodes
            foreach (KeyValuePair<string, NodeManager> kv in nodeManagerMap) kv.Value.adjustNodeColor(highlightBrightness);

            // update all edges
            foreach (GameObject obj in outerEdgeList)
            {
                baseCurve = obj.GetComponent<BaseCurve>();
                if (baseCurve != null) baseCurve.updateHighlightState(highlightState.NONE);
            }

        }
        else if (currHighlightState == highlightState.ONE_HOP)
        {
            // highlight or dim all nodes
            foreach (KeyValuePair<string, NodeManager> kv in nodeManagerMap)
            {
                currNodeManager = kv.Value;
                if (currNodeManager.isSelected) kv.Value.adjustNodeColor(highlightBrightness);
                else kv.Value.adjustNodeColor(dimmedBrightness);
            }

            // update all edges
            foreach (GameObject obj in outerEdgeList)
            {
                baseCurve = obj.GetComponent<BaseCurve>();
                if (baseCurve != null) baseCurve.updateHighlightState();
            }
        }

        else if (currHighlightState == highlightState.NEAR || currHighlightState == highlightState.FAR)
        {
            // dim all nodes
            foreach (KeyValuePair<string, NodeManager> kv in nodeManagerMap)
            {
                currNodeManager = kv.Value;
                if (currNodeManager.isSelected) kv.Value.adjustNodeColor(highlightBrightness);
                else kv.Value.adjustNodeColor(dimmedBrightness);
            }

            // update all edges, highlighting the nodes attached to near edges
            foreach (GameObject obj in outerEdgeList)
            {
                baseCurve = obj.GetComponent<BaseCurve>();
                if (baseCurve != null) baseCurve.updateHighlightState();
            }
        }


        if(doHighlightEdgeThinning)
        {

            if (currHighlightState == highlightState.NONE || (currHighlightState == highlightState.ONE_HOP && selectedNodeMap.Count == 0))
            {
                // update all edges
                foreach (GameObject obj in outerEdgeList)
                {
                    baseCurve = obj.GetComponent<BaseCurve>();
                    if (baseCurve != null) baseCurve.updateRadiusBasedOnHighlightState(highlightState.NONE);
                }

            }
            else if (currHighlightState == highlightState.ONE_HOP)
            {
                // update all edges
                foreach (GameObject obj in outerEdgeList)
                {
                    baseCurve = obj.GetComponent<BaseCurve>();
                    if (baseCurve != null) baseCurve.updateRadiusBasedOnHighlightState();
                }
            }

            else if (currHighlightState == highlightState.NEAR || currHighlightState == highlightState.FAR)
            {
                foreach (GameObject obj in outerEdgeList)
                {
                    baseCurve = obj.GetComponent<BaseCurve>();
                    if (baseCurve != null) baseCurve.updateRadiusBasedOnHighlightState();
                }
            }
        }

    }

    public void updateHighlightState(highlightState type)
    {

        currHighlightState = type;

        foreach ( KeyValuePair<string, NodeManager> kv in nodeManagerMap) kv.Value.currHighlightState = currHighlightState;

        BaseCurve curve;
    
        // update all edges
        foreach (GameObject obj in outerEdgeList)
        {
            curve = obj.GetComponent<BaseCurve>();
            if (curve != null) curve.currHighlightState = currHighlightState;

        }

        updateHighlightNodesEdges();
    }

    public void loadData()
    {
        dataLoader.loadData();
        nodeMap = dataLoader.nodeMap;
        edgeList = dataLoader.edgeList;

        setDefaultParameterValues();

        // populate the nodes (derived class should do this and populate the 2D points)
        // populate the edges (derived class should do this and populate the 2D points)

        // cull isolated nodes (is applicable)
        if (excludeIsolatedNodes) cullIsolatedNodes();

        // populate the groupMap
        populateGroupMap();

        switch(nodeClusterMode)
        {
            case NodeClusterMode.RANDOM:
                randomizeNodePoints();
                break;
            case NodeClusterMode.SQUARE_CLUSTER:
                clusterByGroupsInSquares();
                break;
            case NodeClusterMode.CIRCLE_CLUSTER:
                clusterByGroupsInCircles();
                break;
            case NodeClusterMode.FORCE_DIRECTED:
                doForceDirLayout();
                break;
        }

        // populate the groupColorMap
        populateColorMap();

        // normalize the points and project them onto 3D object
        normalizePointsForNodesAndGroups();
        projectPointsForNodesAndGroups();


        populatePts();
        populateEdges();

        if( useGroupNodes ) populateGroupNodes();
    }

    private void clusterByGroupsInSquares()
    {
        int N = nodeMap.Count; // number of nodes
        int M = groupMap.Count; // number of groups

        GroupInfo[] tmpGrpInfo = new GroupInfo[M];

        int i = 0;
        foreach (GroupInfo grp in groupMap.Values) tmpGrpInfo[i++] = grp;

        SortBySize(tmpGrpInfo, 0, M - 1);

        int w, h;

        int[][] assignedSpots = new int[N][];
        for( i = 0; i < assignedSpots.Length; i++ )
        {
            assignedSpots[i] = new int[N];
        }

        for (i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                assignedSpots[i][j] = 0;
            }
        }

        int usedWidth = 0;
        int usedHeight = 0;

        List<KeyValuePair<int, int>> startingPositions = new List<KeyValuePair<int, int>>();
        startingPositions.Add(new KeyValuePair<int, int>(0, 0));

        int minExpansionArea, minExpansionIdx;
        int currArea, proposedW, proposedH;
        KeyValuePair<int, int> currPair;
        int baseX, baseY, nodeIdx;
        float randX, randY;

        foreach (GroupInfo grp in tmpGrpInfo)
        {
           
            minExpansionArea = N * (N+1);
            minExpansionIdx = 0;
            getDimensions(grp.nodeList.Count, out w, out h);

            for (i = 0; i < startingPositions.Count; i++)
            {
                currPair = startingPositions[i];

                proposedW = currPair.Key + w;
                proposedH = currPair.Value + h;

                currArea = proposedW * proposedH;

                if ((proposedW <= usedWidth && proposedH <= usedHeight)
                    || currArea < minExpansionArea )
                {
                    bool good = true;

                    for (int x = proposedW - w; x <= proposedW; x++)
                    {
                        for (int y = proposedH - h; y <= proposedH; y++)
                        {
                            if(assignedSpots[x][y] != 0 )
                            {
                                good = false;
                                x += w;
                                y += h;
                                break;
                            }
                        }
                    }

                    if( good )
                    {
                        minExpansionArea = currArea;
                        minExpansionIdx = i;

                        if (proposedW <= usedWidth && proposedH <= usedHeight)
                        {
                            i += startingPositions.Count;
                            break;
                        }
                    
                    }

                }
            }

            currPair = startingPositions[minExpansionIdx];
            baseX = currPair.Key;
            baseY = currPair.Value;

            nodeIdx = 0;
            for (int x = baseX; x < baseX + w; x++)
            {
                for (int y = baseY; y < baseY + h; y++)
                {
                    assignedSpots[x][y] = 1;

                    if( nodeIdx < grp.nodeList.Count )
                    {
                        randX = (Random.value - 0.5f) * 0.8f;
                        randY = (Random.value - 0.5f) * 0.8f;
                        NodeInfo info = grp.nodeList[nodeIdx];
                        info.position2.x = (float)x + randX;
                        info.position2.y = (float)(N-y) + randY;
                    }
                    nodeIdx++;
                }
            }

            for (int x = baseX; x <= baseX + w; x++)
            {
                assignedSpots[x][baseY + h] = -1;
            }

            for (int y = baseY; y <= baseY + h; y++)
            {
                assignedSpots[baseX + w][y] = -1;
            }

            usedWidth = (baseX + w) > usedWidth ? (baseX + w) : usedWidth;
            usedHeight = (baseY + h) > usedHeight ? (baseY + h) : usedHeight;

            startingPositions.Add(new KeyValuePair<int, int>(baseX + w + 1, baseY));
            startingPositions.Add(new KeyValuePair<int, int>(baseX, baseY + h + 1));

            startingPositions.Remove(currPair);

        }


    }

    private void getDimensions(int count, out int w, out int h)
    {
        float fCount = (float)count;
        int minDim = (int)Mathf.Ceil(Mathf.Sqrt(fCount));
        int maxDim = (count+1) / 2;

        w = minDim;
        h = minDim;

        bool searchForMinDimensions = false;

        if( searchForMinDimensions )
        {
            int minArea = minDim * minDim + 1;
            int area;

            w = minDim;
            h = minDim;

            for (int mainDim = minDim; mainDim <= maxDim; mainDim++)
            {
                int altDim = (int)Mathf.Ceil(fCount / (float)mainDim);
                area = mainDim * altDim;
                if (area < minArea)
                {
                    minArea = area;
                    w = mainDim;
                    h = altDim;
                }
            }
        }
        
    }

    private void clusterByGroupsInCircles()
    {
        //protected Dictionary<string, GroupInfo> groupMap = new Dictionary<string, GroupInfo>();
        int N = nodeMap.Count; // number of nodes
        int M = groupMap.Count; // number of groups

        GroupInfo[] tmpGrpInfo = new GroupInfo[M];

        int i = 0;
        foreach (GroupInfo grp in groupMap.Values) tmpGrpInfo[i++] = grp;

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

        float hiVal = 100.0f;
        float loVal = 0.01f;
        float midVal = (hiVal + loVal) * 0.5f;


        for (i = 0; i < tmpGrpInfo.Length; i++)
        {
            currGrp = tmpGrpInfo[i];
            portionVals[i] = (float)currGrp.nodeList.Count / (float)N;
        }

        // testing new layout
        bool doNewTest = false;
        if( doNewTest )
        {
            float vecLength = 0f;
            for (i = 0; i < portionVals.Length; i++)
            {
                Debug.Log("[" + i + "]: " + portionVals[i]);

                //portionVals[i] *= portionVals[i];
                portionVals[i] *= 10f;
                vecLength += portionVals[i] * portionVals[i];
            }

            vecLength = Mathf.Sqrt(vecLength);

            for (i = 0; i < portionVals.Length; i++)
            {
                portionVals[i] /= vecLength;

                Debug.Log("[" + i + "]: " + portionVals[i]);
            }
        }


        for (i = 0; i < tmpGrpInfo.Length; i++)
        {
            radiusVals[i] = Mathf.Sqrt(portionVals[i]) * R;
        }

        for (int z = 0; z < 100; z++)
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
            else if (sumAngles < 1.9f * Mathf.PI)
            {
                loVal = midVal;
            }
            else
            {
                break;
            }

            midVal = (hiVal + loVal) * 0.5f;
        }


        tmpGrpInfo[0].center2 = Vector2.zero;

        float currAngle = 0.0f;

        Vector2 currVec;

        float randomPart = 0.9f;
        float fixedPart = 1f - randomPart;
        float prevRandVal = 0f;
        float currRandVal;
        float minRandOffeset = 0.3f;
        for (i = 0; i < tmpGrpInfo.Length; i++)
        {
            currGrp = tmpGrpInfo[i];

            currAngle += angleVals[i];

            if (i == 0) currGrp.center2 = Vector2.zero;
            else currGrp.center2 = new Vector2(Mathf.Cos(currAngle), Mathf.Sin(currAngle)) * (radiusVals[0] + radiusVals[i]);

            float partRadius = radiusVals[i] * 9f/10f;
            float minRadius = radiusVals[i] * 1f/10f;
            float angleInc = 2f * Mathf.PI / (float)currGrp.nodeList.Count;
            float angle = 0f;
            
            for (int j = 0; j < currGrp.nodeList.Count; j++)
            {
                currVec = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                //currGrp.nodeList[j].position2 = currGrp.center2 + currVec * partRadius * Random.value * 0.5f;
                currRandVal = Random.value;
                if( Mathf.Abs(currRandVal-prevRandVal) < minRandOffeset)
                {
                    currRandVal += minRandOffeset;
                    if (currRandVal > 1f) currRandVal -= 1f;
                }
                //currGrp.nodeList[j].position2 = currGrp.center2 + currVec * partRadius * (fixedPart + currRandVal * randomPart);
                currGrp.nodeList[j].position2 = currGrp.center2 + currVec * (minRadius + currRandVal * partRadius);
                angle += angleInc;
                prevRandVal = currRandVal;
            }

        }

    }

    private void cullIsolatedNodes()
    {
        HashSet<string> cullList = new HashSet<string>();

        foreach (string nodeName in nodeMap.Keys) cullList.Add(nodeName);

        foreach (EdgeInfo edgeInfo in edgeList)
        {
            if (cullList.Contains(edgeInfo.startNode.name)) cullList.Remove(edgeInfo.startNode.name);
            if (cullList.Contains(edgeInfo.endNode.name)) cullList.Remove(edgeInfo.endNode.name);
        }

        foreach (string nodeName in cullList) nodeMap.Remove(nodeName);
    }

    private void populateGroupMap()
    {
        NodeInfo currNode;
        GroupInfo currGroup;
        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            currNode = kv.Value;

            if (!groupMap.TryGetValue(currNode.groupName, out currGroup))
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

        foreach (KeyValuePair<string, GroupInfo> kv in groupMap)
        {
            currColor = colors[idx % maxIdx];
            groupColorMap.Add(kv.Key, currColor);
            idx++;
        }
    }

    private void normalizePointsForNodesAndGroups()
    {
        float xMin = float.MaxValue;
        float xMax = float.MinValue;
        float yMin = float.MaxValue;
        float yMax = float.MinValue;

        Vector2 v;
        NodeInfo currNode;

        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            currNode = kv.Value;
            v.x = currNode.position2.x;
            v.y = currNode.position2.y;

            if (v.x < xMin) xMin = v.x;
            if (v.y < yMin) yMin = v.y;
            if (v.x > xMax) xMax = v.x;
            if (v.y > yMax) yMax = v.y;
        }

        float xRange = xMax - xMin;
        float yRange = yMax - yMin;

        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            currNode = kv.Value;
            v.x = ((currNode.position2.x - xMin) / xRange) * 2.0f - 1.0f;
            v.y = ((currNode.position2.y - yMin) / yRange) * 2.0f - 1.0f;

            currNode.position2 = v;
        }

        GroupInfo currGroup;
        foreach (KeyValuePair<string, GroupInfo> kv in groupMap)
        {
            currGroup = kv.Value;
            Vector2 centerVec = Vector2.zero;

            foreach (NodeInfo node in currGroup.nodeList)
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
        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            updateProjectedPointsForNodeInfo(kv.Value);
        }

        foreach (KeyValuePair<string, GroupInfo> kv in groupMap)
        {
            updateProjectedPointsForGroupInfo(kv.Value);
        }


    }

    public void updateProjectedPointsForNodeInfo(NodeInfo currNode)
    {
        currNode.position3 = getProjectedPoint(currNode.position2);
        currNode.sphereCoords = get3DPointProjectionSphereCoords(currNode.position2);
    }

    public void updateProjectedPointsForGroupInfo(GroupInfo currGroup)
    {
        currGroup.center3 = getProjectedPoint(currGroup.center2);
        currGroup.sphereCoords = get3DPointProjectionSphereCoords(currGroup.center2);
    }

    private Vector3 getProjectedPoint(Vector2 pt)
    {
        return get3DPointProjectionSphere(pt, mainObjRadius);
    }

    private Vector3 get3DPointProjectionSphere(Vector2 v, float r)
    {
        
        Vector3 result = new Vector3(r, 0.0f, 0.0f);
        
        float horizontalAngle = v.x * projHorizontalAngle;
        float verticalAngle = v.y * projVerticalAngle;

        Quaternion rotation = Quaternion.Euler(0.0f, horizontalAngle, verticalAngle);

        return rotation * result;
    }

    private Vector3 get3DPointProjectionSphereCoords(Vector2 v)
    {
        return get3DPointProjectionSphereCoords(v, mainObjRadius);
    }

    private Vector3 get3DPointProjectionSphereCoords(Vector2 v, float r)
    {
        return new Vector3(v.x * projHorizontalAngle, v.y * projVerticalAngle, r);
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

    private void populateGroupNodes()
    {

        foreach ( KeyValuePair<string, GroupInfo> kv in groupMap )
        {
            GroupInfo currGrpInfo = kv.Value;
            GameObject node = (GameObject)Instantiate(groupNodePrefab);
            GroupManager grpManager = node.GetComponent<GroupManager>();
            Vector3 centerToPt = projSphere.transform.TransformPoint(currGrpInfo.center3) - projSphere.transform.position;
            centerToPt.Normalize();

            grpManager.dataObjManager = this;
            grpManager.groupInfo = currGrpInfo;
            grpManager.sphereCenterReference = projSphereCenterReference;

            //node.transform.position = projSphere.transform.position + centerToPt * projSphere.transform.localScale.x * 0.08f;
            node.transform.position = projSphere.transform.position + centerToPt * projSphere.transform.localScale.x * 0.8f;

            node.transform.SetParent(projSphere.transform);

            grpManager.init(currGrpInfo.name);
            Renderer rend = node.GetComponent<Renderer>();
            Color targetColor = groupColorMap[kv.Key];
            targetColor.a = 0.25f;
            rend.material.color = targetColor;

            foreach( NodeInfo ni in currGrpInfo.nodeList)
            {
                NodeManager nm = nodeManagerMap[ni.name];
                grpManager.addNode(ni, nm.gameObject);
            }
        }
    }


    private void populatePts()
    {

        //Vector3 ptScale = Vector3.one * tmpRadius * 2f / 125f * pointScaleFactor;
        Vector3 ptScale = getCurrentPointSize();

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();

        if (gazeScript == null)
        {
            Debug.Log("No Gaze stuff");
        }

        NodeInfo currNodeInfo;

        foreach (KeyValuePair<string, NodeInfo> kv in nodeMap)
        {
            currNodeInfo = kv.Value;
            GameObject point = (GameObject)Instantiate(nodePrefab);
            point.name = "Node: " + currNodeInfo.name;
            //point.transform.position = projSphere.transform.TransformPoint(currNodeInfo.position3);
            point.transform.position = transform.TransformPoint(currNodeInfo.position3);
            point.transform.localScale = ptScale;

            MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
            meshRend.material.color = groupColorMap[currNodeInfo.groupName];

            NodeManager manager = point.GetComponent<NodeManager>();
            manager.nodeName = currNodeInfo.name;
            manager.setSubNodeNames(currNodeInfo.subElements);
            manager.nodeInfo = currNodeInfo;
            //manager.positionOnSphere = currNodeInfo.position3;
            manager.baseSphereTransform = projSphere.transform;
            manager.timeToSnapBack = timeToSnapBack;

            manager.setMeshColors(groupColorMap[currNodeInfo.groupName]);

            point.transform.SetParent(projSphere.transform);

            GameObject popupText = (GameObject)Instantiate(popupTextPrefab);
            popupText.transform.position = point.transform.position;
            popupText.transform.SetParent(point.transform);

            PopupTextFade popupTextFade = popupText.GetComponent<PopupTextFade>();
            TextMesh tMesh = popupTextFade.GetComponent<TextMesh>();
            tMesh.text = currNodeInfo.name;

            popupTextFade.parentObject = point;

            if (popupTextFade != null) gazeScript.addTextObject(popupTextFade);
            else Debug.Log("No Popup-text stuff");

            nodeList.Add(point);
            if (!nodeManagerMap.ContainsKey(currNodeInfo.name)) nodeManagerMap.Add(currNodeInfo.name, manager);
            else nodeManagerMap[currNodeInfo.name] = manager;
        }
    }

    public void deselectAllNodes()
    {
        NodeManager[] tmpNodeManagers = new NodeManager[selectedNodeMap.Count];
        selectedNodeMap.Values.CopyTo(tmpNodeManagers, 0);

        toggleSubNodes(tmpNodeManagers);
    }

    public void unselectNode(NodeManager nm, GazeActivate gazeScript)
    {
        NodeInfo nodeInfo;
        SubNodeManager subNodeManager;
        ConnectionManager connManager;

        nm.isSelected = false;
        nodeInfo = nm.nodeInfo;
        string str = nodeInfo.name;
        List<GameObject> objList = subElementObjectMap[str];
        foreach (GameObject obj in objList)
        {
            subNodeManager = obj.GetComponent<SubNodeManager>();
            if (subNodeManager != null)
            {
                Dictionary<string, GameObject> subNodeConnections = subNodeManager.getInnerConnectionMap();
                foreach (KeyValuePair<string, GameObject> kv in subNodeConnections)
                {
                    connManager = kv.Value.GetComponent<ConnectionManager>();
                    if (connManager == null) continue;
                    gazeScript.removeTextObject(connManager.name);
                    connManager.showEndSubNodes();
                    SubNodeManager otherSubNodeManager = connManager.subPointA.GetComponent<SubNodeManager>();
                    if (otherSubNodeManager == subNodeManager) otherSubNodeManager = connManager.subPointB.GetComponent<SubNodeManager>();
                    otherSubNodeManager.removeInnerConnection(connManager);
                    connManager.removeFromNodeManagers();
                }
                subNodeManager.destroyAndRemoveAllInnerConnections();
            }

            Destroy(obj);
        }
        objList.Clear();
        subElementObjectMap.Remove(str);
        selectedNodeMap.Remove(str);

        if (subNodeDisplayType == SubNodeDisplayType.INNER_SPHERE)
        {
            GameObject targetObj = nm.gameObject;
            targetObj.transform.position = projSphere.transform.TransformPoint(nodeInfo.position3);

            // TODO: move the edges
            //nm.removeInnerSphereExtention();

            List<ConnectionManager> connManList = new List<ConnectionManager>();
            nm.getInnerConnections(connManList);

            List<GameObject> connObjects = new List<GameObject>();

            foreach (ConnectionManager cm in connManList)
            {
                gazeScript.removeTextObject(cm.name);
                if (cm.centerPoint != null) GameObject.Destroy(cm.centerPoint);
                connObjects.Add(cm.gameObject);

                cm.nodePointA.GetComponent<NodeManager>().removeInnerConnection(cm);
                cm.nodePointB.GetComponent<NodeManager>().removeInnerConnection(cm);
            }

            foreach (GameObject obj in connObjects)
            { 
                Destroy(obj);
            }

        }
        else
        {
            foreach (string currName in nodeInfo.subElements)
            {
                gazeScript.removeTextObject(getSubNodeKey(nodeInfo, currName));
            }

            nm.removeInnerConnections();
        }

    }

    public void toggleSubNodes(NodeManager[] nodeManagers)
    {
        List<NodeManager> newNodes = new List<NodeManager>();
        List<NodeManager> oldNodes = new List<NodeManager>();

        NodeInfo nodeInfo;

        if ( nodeManagers != null )
        {
            foreach (NodeManager nm in nodeManagers)
            {
                nodeInfo = nm.nodeInfo;
                if (subElementObjectMap.ContainsKey(nodeInfo.name)) oldNodes.Add(nm);
                else newNodes.Add(nm);
            }
        }

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();
        foreach (NodeManager nm in oldNodes)
        {
            unselectNode(nm, gazeScript);
        }


        NodeManager[] newNodeManagers = new NodeManager[newNodes.Count];
        newNodes.CopyTo(newNodeManagers, 0);

        for (int i = 0; i < newNodeManagers.Length; i++)
        {
            switch(subNodeDisplayType)
            {
                case SubNodeDisplayType.BLOOM:
                    populateSubNodes_bloom(newNodeManagers[i]);
                    break;
                case SubNodeDisplayType.TABLE:
                    populateSubNodes_table(newNodeManagers[i]);
                    break;
                case SubNodeDisplayType.INNER_SPHERE:
                    populateSubNodes_innerSphere(newNodeManagers[i]);
                    break;
            }

            if(subNodeDisplayType!= SubNodeDisplayType.INNER_SPHERE)
            {
                foreach (KeyValuePair<string, NodeManager> kv in selectedNodeMap)
                {
                    populateSubNodeConnections(newNodeManagers[i], kv.Value);
                }

                for (int j = 0; j < i; j++)
                {
                    populateSubNodeConnections(newNodeManagers[i], newNodeManagers[j]);
                }
            }

            else
            {
                foreach (KeyValuePair<string, NodeManager> kv in selectedNodeMap)
                {
                    populateInnerNodeConnections(newNodeManagers[i], kv.Value);
                }

                for (int j = 0; j < i; j++)
                {
                    populateInnerNodeConnections(newNodeManagers[i], newNodeManagers[j]);
                }
            }
        }

        for (int i = 0; i < newNodeManagers.Length; i++)
        {
            newNodeManagers[i].isSelected = true;
            selectedNodeMap.Add(newNodeManagers[i].nodeInfo.name, newNodeManagers[i]);
        }

        updateHighlightNodesEdges();
        
        foreach (KeyValuePair<string, NodeManager> kv in selectedNodeMap) kv.Value.hideAllInnerConnectionEdgeNodes();

    }

    public float getCurrBarRadius()
    {
        return barRadiusScale / 125.0f;
    }

    private Vector3 getCurrentPointSize()
    {
        Vector3 ptScale = Vector3.one * projSphere.transform.localScale.x * 2f / 125f * pointScaleFactor;
        return ptScale;
    }

    void populateSubNodes_innerSphere(NodeManager nodeManager)
    {
        if (nodeManager == null) return;

        NodeInfo nodeInfo = nodeManager.nodeInfo;

        if (nodeInfo == null) return;

        if (subElementObjectMap.ContainsKey(nodeInfo.name)) return;

        GameObject targetObj = nodeManager.gameObject;

        Vector3 initPos = targetObj.transform.position;
        Vector3 destPos = innerSphere.transform.TransformPoint(nodeInfo.position3);
        //Vector3 destPos = projSphere.transform.TransformPoint(nodeInfo.position3);        

        targetObj.transform.position = destPos;

        List<GameObject> gObjList = new List<GameObject>();

        string subKey;
        foreach (string currName in nodeInfo.subElements)
        {
            subKey = getSubNodeKey(nodeInfo, currName);
            if (subNodePositionMap.ContainsKey(subKey)) subNodePositionMap[subKey] = targetObj;
            else subNodePositionMap.Add(subKey, targetObj);
        }

        subElementObjectMap.Add(nodeInfo.name, gObjList);

        // TODO: move the edges

        //nodeManager.setupInnerSphereExtension(bezierPrefab, initPos, destPos, getCurrBarRadius());
    }

    void populateSubNodes_bloom(NodeManager nodeManager)
    {
        if (nodeManager == null) return;

        NodeInfo nodeInfo = nodeManager.nodeInfo;

        if (nodeInfo == null) return;

        if (subElementObjectMap.ContainsKey(nodeInfo.name)) return;

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();

        float tmpScale = projSphere.transform.localScale.x;
        float tmpRadius = tmpScale;
        Vector3 ptScale = getCurrentPointSize();

        Vector3 upDir = projSphere.transform.TransformPoint(nodeManager.nodeInfo.position3) - projSphere.transform.position;
        upDir.Normalize();


        Vector3 rightVec = new Vector3(1f, 0f, 0f);
        if (Vector3.Dot(upDir, rightVec) > 0.99f) rightVec = new Vector3(0f, 0f, 1f);
        rightVec.Normalize();

        Vector3 forVec = Vector3.Cross(upDir, rightVec);
        forVec.Normalize();

        rightVec = Vector3.Cross(forVec, upDir);
        rightVec.Normalize();

        List<GameObject> gObjList = new List<GameObject>();

        float radAngle = 2f * Mathf.PI / (float)nodeInfo.subElements.Count;
        float currAngle = 0f;

        Vector3 center = nodeManager.gameObject.transform.position - upDir * tmpRadius * 0.15f;
        float c, s;

        //float barRadius = tmpRadius / 125.0f * barRadiusScale;

        float barRadius = getCurrBarRadius();

        Vector3[] basePoints = new Vector3[4];

        basePoints[0] = nodeManager.gameObject.transform.position;
        if (innerNodeHornLayout) basePoints[1] = center * 0.25f + basePoints[0] * 0.75f;
        else basePoints[1] = (center + basePoints[0]) * 0.5f;

        string subKey;

        nodeManager.gameObject.transform.localRotation = Quaternion.identity;

        foreach (string currName in nodeInfo.subElements)
        {
            c = Mathf.Cos(currAngle);
            s = Mathf.Sin(currAngle);

            GameObject point = (GameObject)Instantiate(innerNodePrefab);
            point.name = "Sub: " + currName;

            basePoints[3] = center + (rightVec * c + forVec * s) * 0.05f * tmpScale;
            if (innerNodeHornLayout) basePoints[2] = center * 0.75f + basePoints[3] * 0.25f;
            else basePoints[2] = basePoints[3] + upDir * tmpRadius * 0.075f;


            point.transform.position = basePoints[3];
            point.transform.localScale = ptScale;

            Color color = groupColorMap[nodeInfo.groupName];

            MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
            meshRend.material.color = color;


            GameObject edgeObj = (GameObject)Instantiate(bezierPrefab);
            BezierBar bezBar = edgeObj.GetComponent<BezierBar>();
            bezBar.radius = barRadius;
            bezBar.useSphericalInterpolation = false;
            bezBar.init(basePoints, color, color);
            //edgeObj.transform.position = projSphere.transform.position;

            SubNodeManager subNM = point.GetComponent<SubNodeManager>();
            subNM.setMeshColors(color);

            subKey = getSubNodeKey(nodeInfo, currName);
            if (subNodePositionMap.ContainsKey(subKey)) subNodePositionMap[subKey] = point;
            else subNodePositionMap.Add(subKey, point);

            gObjList.Add(point);
            gObjList.Add(edgeObj);


            GameObject popupText = (GameObject)Instantiate(popupTextPrefab);
            popupText.name = subKey;
            popupText.transform.position = point.transform.position;
            popupText.transform.localScale = Vector3.one * 0.5f * projSphere.transform.localScale.x;
            popupText.transform.SetParent(point.transform);

            PopupTextFade popupTextObject = popupText.GetComponent<PopupTextFade>();

            CameraOriented camOriented = popupText.GetComponent<CameraOriented>();
            Destroy(camOriented);

            TextMesh tMesh = popupTextObject.GetComponent<TextMesh>();
            tMesh.text = currName;
            popupTextObject.parentObject = point;

            if (popupTextObject != null) gazeScript.addTextObject(subKey, popupTextObject);
            else Debug.Log("No Popup-text stuff");



            currAngle += radAngle;



            point.transform.SetParent(nodeManager.gameObject.transform);
            edgeObj.transform.SetParent(nodeManager.gameObject.transform);


        }

        subElementObjectMap.Add(nodeInfo.name, gObjList);

    }

    void populateSubNodes_table(NodeManager nodeManager)
    {
        if (nodeManager == null) return;

        NodeInfo nodeInfo = nodeManager.nodeInfo;

        if (nodeInfo == null) return;

        if (subElementObjectMap.ContainsKey(nodeInfo.name)) return;

      

        Vector3 ptScale = getCurrentPointSize();

        Vector3 upDir = projSphere.transform.TransformPoint(nodeManager.nodeInfo.position3) - projSphere.transform.position;
        upDir.Normalize();


        List<GameObject> gObjList = new List<GameObject>();

         
        string subKey;

        nodeManager.gameObject.transform.localRotation = Quaternion.identity;

        int numAdded = 0;


        float barRadius = getCurrBarRadius();

        Vector3[] basePoints = new Vector3[4];

        

        foreach (string currName in nodeInfo.subElements)
        {
           
            GameObject point = (GameObject)Instantiate(innerNodePrefab);
            point.name = "Sub: " + currName;

            point.transform.position = nodeManager.gameObject.transform.position - upDir * numAdded*0.02f;
            point.transform.localScale = ptScale;

            Color color = groupColorMap[nodeInfo.groupName];
            MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
            meshRend.material.color = color;

            meshRend.enabled = false;


            subKey = getSubNodeKey(nodeInfo, currName);
            if (subNodePositionMap.ContainsKey(subKey)) subNodePositionMap[subKey] = point;
            else subNodePositionMap.Add(subKey, point);

            gObjList.Add(point);


            GameObject textLabel = (GameObject)Instantiate(popupTextPrefab);
            textLabel.name = subKey;
            textLabel.transform.position = point.transform.position;
            textLabel.transform.localScale = Vector3.one * 0.5f * projSphere.transform.localScale.x;
            textLabel.transform.SetParent(point.transform);

            PopupTextFade popupTextObject = textLabel.GetComponent<PopupTextFade>();
            Destroy(popupTextObject);
            TextMesh tMesh = textLabel.GetComponent<TextMesh>();
            tMesh.text = currName;

            tMesh.anchor = TextAnchor.MiddleLeft;

            point.transform.SetParent(nodeManager.gameObject.transform);

            numAdded++;
        }

        subElementObjectMap.Add(nodeInfo.name, gObjList);

    }

    void populateInnerNodeConnections(NodeManager nodeManagerA, NodeManager nodeManagerB)
    {
        if (nodeManagerA == null || nodeManagerB == null) return;
        NodeInfo infoA = nodeManagerA.nodeInfo;
        NodeInfo infoB = nodeManagerB.nodeInfo;
        if (infoA == null || infoB == null) return;

        Vector3[] ctrlPts = new Vector3[4];
        string connectionKey;
        Vector3 sphereCenter = projSphere.transform.position;

        Color colorA = groupColorMap[infoA.groupName];
        Color colorB = groupColorMap[infoB.groupName];

        float barRadius = getCurrBarRadius();

        Vector3 tmpVecA = Vector3.zero;
        Vector3 tmpVecB = Vector3.zero;

        Vector3 ptScale = getCurrentPointSize();

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();

        List<string> subsInCommon = new List<string>();

        for (int i = 0; i < infoA.subElements.Count; i++)
        {
            for (int j = 0; j < infoB.subElements.Count; j++)
            {
                if (infoA.subElements[i].Equals(infoB.subElements[j]))
                {
                    subsInCommon.Add(infoA.subElements[i]);
                }
            }
        }

        if (subsInCommon.Count < 1) return;


        // start the connection


        connectionKey =  "[]: " + infoA.name + "|" + infoB.name;

        ctrlPts[0] = nodeManagerA.gameObject.transform.position;
        ctrlPts[3] = nodeManagerB.gameObject.transform.position;

        tmpVecA = sphereCenter - nodeManagerA.gameObject.transform.position;
        tmpVecB = sphereCenter - nodeManagerB.gameObject.transform.position;

        if (innerConnectionsStraightLines)
        {
            
            ctrlPts[1] = ctrlPts[0] + tmpVecA * 0.001f;
            ctrlPts[2] = ctrlPts[3] + tmpVecB * 0.001f;
        }
        else
        {
            ctrlPts[1] = ctrlPts[0] + tmpVecA * 0.5f;
            ctrlPts[2] = ctrlPts[3] + tmpVecB * 0.5f;
        }


        GameObject edgeObj = (GameObject)Instantiate(bezierCollPrefab);
        ConnectionManager connMan = edgeObj.GetComponent<ConnectionManager>();
        GameObject mainCurve = connMan.mainCurve;


        BezierBar bezBar = mainCurve.GetComponent<BezierBar>();
        bezBar.radius = barRadius;
        bezBar.useSphericalInterpolation = false;
        bezBar.init(ctrlPts, colorA, colorB);


        //var collider = mainCurve.GetComponent<MeshCollider>();
        //collider.sharedMesh = bezBar.mesh;

        string textForPopup = "";

        foreach(string s in subsInCommon)
        {
            if(textForPopup.Length > 0) textForPopup += "\n" + s;
            else textForPopup += s;
        }

        connMan.setupText(textForPopup);

        connMan.colorA = colorA;
        connMan.colorB = colorB;
        connMan.innerConnStraightLine = innerConnectionsStraightLines;
        connMan.projSphereTransform = projSphere.transform;
        connMan.subPointA = nodeManagerA.gameObject;
        connMan.subPointB = nodeManagerB.gameObject;
        connMan.nodePointA = nodeManagerA.gameObject;
        connMan.nodePointB = nodeManagerB.gameObject;
        connMan.bezBar = bezBar;
        connMan.dataManager = this;

        connMan.dontToggleMeshRenderes = true;

        //if(subNodeDisplayType == SubNodeDisplayType.TABLE) connMan.neverShowMeshRenderers = true;



        BezierLine bezLine = connMan.altLineCurve.GetComponent<BezierLine>();
        connMan.bezLine = bezLine;
        connMan.altLineCurve.SetActive(false);

        if (nodeManagerA.nodeName.CompareTo(nodeManagerB.nodeName) < 0) connMan.name = nodeManagerA.nodeName + " |conn| " + nodeManagerB.nodeName;
        else connMan.name = nodeManagerB.nodeName + " |conn| " + nodeManagerA.nodeName;



        nodeManagerA.addInnerConnection(connMan);
        nodeManagerB.addInnerConnection(connMan);

        edgeObj.transform.SetParent(projSphere.transform);




        connMan.assignMeshRenderers();
        connMan.hideEndSubNodes();

        GameObject point = (GameObject)Instantiate(innerNodePrefab);
        point.name = "MidPt: " + connMan.name;

        Destroy(point.GetComponent<NodeManager>());


        Vector3 centerPt = 0.125f * (ctrlPts[0] + 3f * (ctrlPts[1] + ctrlPts[2]) + ctrlPts[3]);

        point.transform.position = centerPt;
        point.transform.localScale = ptScale;

        connMan.centerPoint = point;

        point.transform.SetParent(projSphere.transform);

        SubNodeManager subNM = point.GetComponent<SubNodeManager>();
        subNM.setMeshColors(colorA * 0.5f + colorB * 0.5f);



        GameObject popupText = (GameObject)Instantiate(popupTextPrefab);
        popupText.name = "Text: " + connMan.name;
        popupText.transform.position = point.transform.position;
        popupText.transform.localScale = Vector3.one * 0.5f;
        popupText.transform.SetParent(point.transform);

        PopupTextFade popupTextObject = popupText.GetComponent<PopupTextFade>();
        TextMesh tMesh = popupTextObject.GetComponent<TextMesh>();
        tMesh.text = textForPopup;
        popupTextObject.parentObject = point;

        if (popupTextObject != null) gazeScript.addTextObject(connMan.name, popupTextObject);
        else Debug.Log("No Popup-text stuff");

        /*
        SubNodeManager subNodeManA = connMan.subPointA.GetComponent<SubNodeManager>();
        subNodeManA.addInnerConnection(connMan, edgeObj);
        SubNodeManager subNodeManB = connMan.subPointB.GetComponent<SubNodeManager>();
        subNodeManB.addInnerConnection(connMan, edgeObj);
        */
        // end the connections



    }

    void populateSubNodeConnections(NodeManager nodeManagerA, NodeManager nodeManagerB)
    {
        //float tmpRadius = radius * projSphere.transform.localScale.x;

        if (nodeManagerA == null || nodeManagerB == null) return;
        NodeInfo infoA = nodeManagerA.nodeInfo;
        NodeInfo infoB = nodeManagerB.nodeInfo;
        if (infoA == null || infoB == null) return;

        //List<GameObject> objListA = subElementObjectMap[infoA.name];
        //List<GameObject> objListB = subElementObjectMap[infoB.name];

        Vector3[] ctrlPts = new Vector3[4];
        string keyA, keyB, connectionKey;
        Vector3 sphereCenter = projSphere.transform.position;

        Color colorA = groupColorMap[infoA.groupName];
        Color colorB = groupColorMap[infoB.groupName];

        //float barRadius = tmpRadius / 125.0f * barRadiusScale;

        float barRadius = getCurrBarRadius();

        bool tmpVecASet = false;
        bool tmpVecBSet = false;
        Vector3 tmpVecA = Vector3.zero;
        Vector3 tmpVecB = Vector3.zero;

        Vector3 ptScale = getCurrentPointSize();

        GazeActivate gazeScript = Camera.main.GetComponent<GazeActivate>();

        for (int i = 0; i < infoA.subElements.Count; i++)
        {
            for (int j = 0; j < infoB.subElements.Count; j++)
            {
                if (infoA.subElements[i].Equals(infoB.subElements[j]))
                {

                    keyA = getSubNodeKey(infoA, i);
                    keyB = getSubNodeKey(infoB, j);
                    connectionKey = getInnerNodeKey(infoA, infoB, i);

                    ctrlPts[0] = subNodePositionMap[keyA].transform.position;
                    ctrlPts[3] = subNodePositionMap[keyB].transform.position;

                    if (!tmpVecASet)
                    {
                        tmpVecA = sphereCenter - nodeManagerA.gameObject.transform.position;
                        tmpVecA.Normalize();
                        tmpVecA *= Vector3.Dot(tmpVecA, ctrlPts[0] - nodeManagerA.gameObject.transform.position);
                        tmpVecASet = true;
                    }

                    if (!tmpVecBSet)
                    {
                        tmpVecB = sphereCenter - nodeManagerB.gameObject.transform.position;
                        tmpVecB.Normalize();
                        tmpVecB *= Vector3.Dot(tmpVecB, ctrlPts[3] - nodeManagerB.gameObject.transform.position);
                        tmpVecBSet = true;
                    }


                    if (innerConnectionsStraightLines)
                    {
                        //ctrlPts[1] = ctrlPts[0] * 0.67f + ctrlPts[3] * 0.33f;
                        //ctrlPts[2] = ctrlPts[3] * 0.67f + ctrlPts[0] * 0.33f;

                        ctrlPts[1] = ctrlPts[0] + tmpVecA * 0.001f;
                        ctrlPts[2] = ctrlPts[3] + tmpVecB * 0.001f;
                    }
                    else
                    {
                        ctrlPts[1] = ctrlPts[0] + tmpVecA;
                        ctrlPts[2] = ctrlPts[3] + tmpVecB;
                    }


                    GameObject edgeObj = (GameObject)Instantiate(bezierCollPrefab);
                    ConnectionManager connMan = edgeObj.GetComponent<ConnectionManager>();
                    GameObject mainCurve = connMan.mainCurve;


                    BezierBar bezBar = mainCurve.GetComponent<BezierBar>();
                    bezBar.radius = barRadius;
                    bezBar.useSphericalInterpolation = false;
                    bezBar.init(ctrlPts, colorA, colorB);
                    

                    //var collider = mainCurve.GetComponent<MeshCollider>();
                    //collider.sharedMesh = bezBar.mesh;

                    connMan.setupText(infoA.subElements[i]);

                    connMan.colorA = colorA;
                    connMan.colorB = colorB;
                    connMan.innerConnStraightLine = innerConnectionsStraightLines;
                    connMan.projSphereTransform = projSphere.transform;
                    connMan.subPointA = subNodePositionMap[keyA];
                    connMan.subPointB = subNodePositionMap[keyB];
                    connMan.nodePointA = nodeManagerA.gameObject;
                    connMan.nodePointB = nodeManagerB.gameObject;
                    connMan.bezBar = bezBar;
                    connMan.dataManager = this;

                    //if(subNodeDisplayType == SubNodeDisplayType.TABLE) connMan.neverShowMeshRenderers = true;



                    BezierLine bezLine = connMan.altLineCurve.GetComponent<BezierLine>();
                    connMan.bezLine = bezLine;
                    connMan.altLineCurve.SetActive(false);

                    if (nodeManagerA.nodeName.CompareTo(nodeManagerB.nodeName) < 0) connMan.name = keyA + " |conn| " + keyB;
                    else connMan.name = connectionKey;



                    nodeManagerA.addInnerConnection(connMan);
                    nodeManagerB.addInnerConnection(connMan);

                    edgeObj.transform.SetParent(projSphere.transform);


                    

                    connMan.assignMeshRenderers();
                    connMan.hideEndSubNodes();

                    GameObject point = (GameObject)Instantiate(innerNodePrefab);
                    point.name = "MidPt: " + connMan.name;

                    Destroy(point.GetComponent<NodeManager>());


                    Vector3 centerPt = 0.125f * (ctrlPts[0] + 3f * (ctrlPts[1] + ctrlPts[2]) + ctrlPts[3]);

                    point.transform.position = centerPt;
                    point.transform.localScale = ptScale;


                    //MeshRenderer meshRend = point.GetComponent<MeshRenderer>();
                    //meshRend.material.color = colorA*0.5f + colorB*0.5f;

                    connMan.centerPoint = point;

                    point.transform.SetParent(projSphere.transform);

                    SubNodeManager subNM = point.GetComponent<SubNodeManager>();
                    subNM.setMeshColors(colorA*0.5f + colorB*0.5f);



                    GameObject popupText = (GameObject)Instantiate(popupTextPrefab);
                    popupText.name = "Text: " + connMan.name;
                    popupText.transform.position = point.transform.position;
                    popupText.transform.localScale = Vector3.one * 0.5f;
                    popupText.transform.SetParent(point.transform);

                    PopupTextFade popupTextObject = popupText.GetComponent<PopupTextFade>();
                    TextMesh tMesh = popupTextObject.GetComponent<TextMesh>();
                    tMesh.text = infoA.subElements[i];
                    popupTextObject.parentObject = point;

                    if (popupTextObject != null) gazeScript.addTextObject(connMan.name, popupTextObject);
                    else Debug.Log("No Popup-text stuff");

                    SubNodeManager subNodeManA = connMan.subPointA.GetComponent<SubNodeManager>();
                    subNodeManA.addInnerConnection(connMan, edgeObj);
                    SubNodeManager subNodeManB = connMan.subPointB.GetComponent<SubNodeManager>();
                    subNodeManB.addInnerConnection(connMan, edgeObj);


                    j += infoB.subElements.Count;
                }
            }
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

    public string getInnerNodeKey(NodeInfo nodeInfo1, NodeInfo nodeInfo2, string subName)
    {
        return subName + ": " + nodeInfo1.name + "|" + nodeInfo2.name;
    }

    public string getInnerNodeKey(NodeInfo nodeInfo1, NodeInfo nodeInfo2, int idx1)
    {
        if (idx1 >= nodeInfo1.subElements.Count) return "";
        return getInnerNodeKey(nodeInfo1, nodeInfo2, nodeInfo1.subElements[idx1]);
    }

    public void repopulateEdges()
    {
        repopulateEdges(edgeList);
    }

    public void repopulateEdges(List<EdgeInfo> list)
    {
        updateParameterValues();

        foreach (EdgeInfo edge in list) edge.updateNextFrame = true;

        activesUpdateEdges = true;
    }

    private void recalcAllEdges()
    {
        Vector3[] basePts;

        Vector3 tVec1, tVec2;

        float barRadius = getCurrBarRadius();

        GroupInfo fromGroup;
        GroupInfo toGroup;

        float groupWeight = splineGroupWeight;
        float nodeWeight = 1.0f - groupWeight;

        float splineEdgeDistOuter = (1.0f + edgeDist);
        float splineEdgeDistInner = (1.0f + edgeDist * 0.25f);

        float bezEdgeDist = 1.0f + innerGroupEdgeDist;

        BezierBar bezBarScript;
        BasisSpline splineScript;
        GameObject edgeObj;

        int numUpdatedThisFrame = 0;

        foreach (EdgeInfo edge in edgeList)
        {
            if (!edge.updateNextFrame) continue;
          
            if (numUpdatedThisFrame >= maxCurvesUpdatedPerFrame) break;

            numUpdatedThisFrame++;

            edgeObj = edge.edgeGameObject;
            edge.updateNextFrame = false;

            bezBarScript = edgeObj.GetComponent<BezierBar>();
            splineScript = edgeObj.GetComponent<BasisSpline>();

            if (edge.isSameGroup())
            {
                basePts = bezBarScript.controlPoints;

                if (interpolateSpherical)
                {
                    basePts[0] = edge.startNode.sphereCoords;
                    basePts[1] = basePts[0];
                    basePts[1].z *= bezEdgeDist; // the radius from the sphere center

                    basePts[3] = edge.endNode.sphereCoords;
                    basePts[2] = basePts[3];
                    basePts[2].z *= bezEdgeDist;
                }
                else
                {
                    basePts[0] = edge.startNode.position3;
                    basePts[1] = basePts[0] * bezEdgeDist;

                    basePts[3] = edge.endNode.position3;
                    basePts[2] = basePts[3] * bezEdgeDist;
                }

                //edgeObj.transform.SetParent(null);

                bezBarScript.radius = barRadius;
                bezBarScript.useSphericalInterpolation = interpolateSpherical;
                //bezBarScript.init(basePts, projSphere.transform);
                bezBarScript.init(basePts);

                //edgeObj.transform.SetParent(projSphere.transform);
            }
            else
            {
                fromGroup = groupMap[edge.startNode.groupName];
                toGroup = groupMap[edge.endNode.groupName];

                if (interpolateSpherical)
                {
                    tVec1 = fromGroup.sphereCoords;
                    tVec2 = toGroup.sphereCoords;

                    basePts = splineScript.controlPoints;

                    basePts[0] = edge.startNode.sphereCoords;  // start point
                    basePts[6] = edge.endNode.sphereCoords; // end point

                    basePts[1] = basePts[2] = tVec1 * groupWeight + basePts[0] * nodeWeight;
                    basePts[1].z *= splineEdgeDistInner;  // group1 node radius 1.1*radius
                    basePts[2].z *= splineEdgeDistOuter;  // group1 node radius 1.4*radius

                    basePts[4] = basePts[5] = tVec2 * groupWeight + basePts[6] * nodeWeight;
                    basePts[4].z *= splineEdgeDistOuter;  // group2 node radius 1.4*radius
                    basePts[5].z *= splineEdgeDistInner;  // group2 node radius 1.1*radius

                    basePts[3] = (basePts[4] + basePts[2]) * 0.5f;
                }
                else
                {
                    tVec1 = fromGroup.center3;
                    tVec2 = toGroup.center3;

                    basePts = new Vector3[7];


                    basePts[0] = edge.startNode.position3;
                    basePts[6] = edge.endNode.position3;

                    basePts[1] = tVec1 * splineEdgeDistInner;
                    basePts[2] = tVec1 * splineEdgeDistOuter;

                    basePts[4] = tVec2 * splineEdgeDistOuter;
                    basePts[5] = tVec2 * splineEdgeDistInner;

                    basePts[3] = (basePts[4] + basePts[2]) * 0.5f;

                }

                //edgeObj.transform.SetParent(null);

                splineScript.radius = barRadius;
                splineScript.useSphericalInterpolation = interpolateSpherical;
                splineScript.init(basePts);

                //edgeObj.transform.SetParent(projSphere.transform);
            }

        }

        if (numUpdatedThisFrame == 0) activesUpdateEdges = false;

    }

    public void recalculateNodeSizes()
    {
        updateParameterValues();

        Vector3 ptLocalScale = getCurrentPointSize();
        ptLocalScale /= projSphere.transform.localScale.x;

        foreach (GameObject obj in nodeList)
        {
            if(obj.transform.childCount > 0)
            {
                List<Transform> tList = new List<Transform>();
                for (int i = 0; i < obj.transform.childCount; i++) tList.Add(obj.transform.GetChild(i));
                obj.transform.DetachChildren();

                obj.transform.localScale = ptLocalScale;

                foreach (Transform t in tList) t.SetParent(obj.transform);
            }

            else obj.transform.localScale = ptLocalScale;
        }

        // sub nodes
        ptLocalScale = getCurrentPointSize();

        BezierBar bezScript;

        foreach (KeyValuePair<string, List<GameObject>> kv in subElementObjectMap)
        {
            foreach (GameObject obj in kv.Value)
            {
                bezScript = obj.GetComponent<BezierBar>();
                if (bezScript == null)
                {
                    List<Transform> tList = new List<Transform>();
                    for (int i = 0; i < obj.transform.childCount; i++) tList.Add(obj.transform.GetChild(i));
                    obj.transform.DetachChildren();


                    Transform parentTransform = obj.transform.parent;
                    obj.transform.SetParent(null);
                    obj.transform.localScale = ptLocalScale;
                    obj.transform.SetParent(parentTransform);

                    foreach (Transform t in tList) t.SetParent(obj.transform);

                }
            }
        }

        // update all sub node connections
        List<GameObject> subObjectList;
        SubNodeManager snManager;

        ptLocalScale = getCurrentPointSize();
        foreach (KeyValuePair<string, List<GameObject>> kv in subElementObjectMap)
        {
            subObjectList = kv.Value;
            foreach (GameObject obj in subObjectList)
            {
                
                snManager = obj.GetComponent<SubNodeManager>();
                if (snManager != null)
                {
                    snManager.updateConnectionCenterNodeScale(ptLocalScale);
                }
            }
        }


    }

    public void recalculateEdgeRadii()
    {
        updateParameterValues();

        float barRadius = getCurrBarRadius();

        BaseCurve curve;

        foreach (GameObject obj in outerEdgeList)
        {
            curve = obj.GetComponent<BaseCurve>();

            curve.updateBarRadius(barRadius);
        }

        // subnode edges
        foreach (KeyValuePair<string, List<GameObject>> kv in subElementObjectMap)
        {
            foreach (GameObject obj in kv.Value)
            {
                curve = obj.GetComponent<BaseCurve>();
                if (curve != null)
                {
                    Transform parentTransform = obj.transform.parent;
                    obj.transform.SetParent(null);
                    curve.radius = barRadius;
                    curve.refreshVertices();
                    obj.transform.SetParent(parentTransform);
                }

            }
        }

        // update all sub node connections
        List<GameObject> subObjectList;
        SubNodeManager snManager;
        foreach(KeyValuePair<string, List<GameObject>> kv in subElementObjectMap)
        {
            subObjectList = kv.Value;
            foreach(GameObject obj in subObjectList)
            {
                curve = obj.GetComponent<BaseCurve>();
                if (curve != null)
                {
                    Transform parentTransform = obj.transform.parent;
                    obj.transform.SetParent(null);
                    curve.radius = barRadius;
                    curve.refreshVertices();
                    obj.transform.SetParent(parentTransform);
                }
                
                snManager = obj.GetComponent<SubNodeManager>();
                if (snManager != null)
                {
                    snManager.updateConnectionEdgeThickness(barRadius);
                }
            }
        }

    }

    private void populateEdges()
    {
        populateEdges(edgeDist, innerGroupEdgeDist);
    }

    private void populateEdges(float d1, float d2)
    {
        Vector3[] basePts = new Vector3[4];

        Vector3 tVec1, tVec2;

        Color c0;
        Color c1;

        //float sphereScale = projSphere.transform.localScale.x;

        //float barRadius = radius / 125.0f * barRadiusScale * sphereScale;
        float barRadius = getCurrBarRadius();

        GroupInfo fromGroup;
        GroupInfo toGroup;

        float groupWeight = splineGroupWeight;
        float nodeWeight = 1.0f - groupWeight;

        float splineEdgeDistOuter = (1.0f + d1);
        float splineEdgeDistInner = (1.0f + d1 * 0.25f);

        float bezEdgeDist = 1.0f + d2;

        GameObject edgeObj;
        GameObject nodeA, nodeB;

        foreach (EdgeInfo edge in edgeList)
        {

            c0 = groupColorMap[edge.startNode.groupName];
            c1 = groupColorMap[edge.endNode.groupName];

            nodeA = nodeManagerMap[edge.startNode.name].gameObject;
            nodeB = nodeManagerMap[edge.endNode.name].gameObject;

            if (edge.isSameGroup())
            {
                basePts = new Vector3[4];

                if (interpolateSpherical)
                {
                    basePts[0] = edge.startNode.sphereCoords;
                    basePts[1] = basePts[0];
                    basePts[1].z *= bezEdgeDist; // the radius from the sphere center

                    basePts[3] = edge.endNode.sphereCoords;
                    basePts[2] = basePts[3];
                    basePts[2].z *= bezEdgeDist;
                }
                else
                {
                    basePts[0] = edge.startNode.position3;
                    basePts[1] = basePts[0] * bezEdgeDist;

                    basePts[3] = edge.endNode.position3;
                    basePts[2] = basePts[3] * bezEdgeDist;
                }

                edgeObj = (GameObject)Instantiate(bezierPrefab);
                InnerGroupEdge bezBar = edgeObj.GetComponent<InnerGroupEdge>();
                bezBar.radius = barRadius;
                bezBar.edgeThinningAmount = edgeThinAmount;
                bezBar.useSphericalInterpolation = interpolateSpherical;
                bezBar.init(basePts, c0, c1, projSphere.transform);

                bezBar.setNode(nodeA);
                bezBar.setNode(nodeB);

                //bezBar.setNode(edge.startNode);

                edgeObj.transform.SetParent(projSphere.transform);

                edge.edgeGameObject = edgeObj;
            }
            else
            {
                fromGroup = groupMap[edge.startNode.groupName];
                toGroup = groupMap[edge.endNode.groupName];

                if (interpolateSpherical)
                {
                    tVec1 = fromGroup.sphereCoords;
                    tVec2 = toGroup.sphereCoords;

                    basePts = new Vector3[7];

                    basePts[0] = edge.startNode.sphereCoords;  // start point
                    basePts[6] = edge.endNode.sphereCoords; // end point

                    //basePts[1] = basePts[2] = tVec1;
                    basePts[1] = basePts[2] = tVec1 * groupWeight + basePts[0] * nodeWeight;
                    basePts[1].z *= splineEdgeDistInner;  // group1 node radius 1.1*radius
                    basePts[2].z *= splineEdgeDistOuter;  // group1 node radius 1.4*radius

                    //basePts[4] = basePts[5] = tVec2;
                    basePts[4] = basePts[5] = tVec2 * groupWeight + basePts[6] * nodeWeight;
                    basePts[4].z *= splineEdgeDistOuter;  // group2 node radius 1.4*radius
                    basePts[5].z *= splineEdgeDistInner;  // group2 node radius 1.1*radius

                    basePts[3] = (basePts[4] + basePts[2]) * 0.5f;
                }
                else
                {
                    tVec1 = fromGroup.center3;
                    tVec2 = toGroup.center3;

                    basePts = new Vector3[7];


                    basePts[0] = edge.startNode.position3;
                    basePts[6] = edge.endNode.position3;

                    basePts[1] = tVec1 * splineEdgeDistInner;
                    basePts[2] = tVec1 * splineEdgeDistOuter;

                    basePts[4] = tVec2 * splineEdgeDistOuter;
                    basePts[5] = tVec2 * splineEdgeDistInner;

                    basePts[3] = (basePts[4] + basePts[2]) * 0.5f;

                }

                edgeObj = (GameObject)Instantiate(bSplinePrefab);
                InterGroupEdge bspline = edgeObj.GetComponent<InterGroupEdge>();
                bspline.radius = barRadius;
                bspline.edgeThinningAmount = edgeThinAmount;
                bspline.useSphericalInterpolation = interpolateSpherical;
                if (invertFarEdgeGradient) bspline.init(basePts, c1, c0, projSphere.transform);
                else bspline.init(basePts, c0, c1, projSphere.transform);

                bspline.setNode(nodeA);
                bspline.setNode(nodeB);

                edgeObj.transform.SetParent(projSphere.transform);

                edge.edgeGameObject = edgeObj;
            }


            outerEdgeList.Add(edgeObj);

            nodeManagerMap[edge.startNode.name].addOuterConnection(edgeObj, edge.isSameGroup());
            nodeManagerMap[edge.endNode.name].addOuterConnection(edgeObj, edge.isSameGroup());

            nodeManagerMap[edge.startNode.name].addOuterEdgeInfo(edge);
            nodeManagerMap[edge.endNode.name].addOuterEdgeInfo(edge);

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

    int t_testIdx = 0;

    void testSearch()
    {
        string[] testTerms = { "howard", "robert" };

        // scott, howard, judge, jonah
        string term = testTerms[t_testIdx];

        List<NodeManager> list = searchNodesByTerm(term);

        Debug.Log("Found " + list.Count + " nodes while searching for " + term);
        foreach (NodeManager nm in list)
        {
            Debug.Log("Found: " + nm.name);
        }

        t_testIdx++;
        if (t_testIdx >= testTerms.Length) t_testIdx = 0;

        
    }


    List<NodeManager> searchNodesByTerm(string term)
    {
        string uTerm = term.ToUpper();
        List<NodeManager> results = new List<NodeManager>();

        NodeManager currManager;
        NodeInfo currInfo;
        string tUpper;
        foreach(KeyValuePair<string, NodeManager> kv in nodeManagerMap)
        {
            currManager = kv.Value;
            currInfo = currManager.nodeInfo;
            tUpper = currManager.name.ToUpper();
            if (tUpper.Contains(uTerm) )
            {
                results.Add(currManager);
                continue;
            }

            foreach(string currSubElement in currInfo.subElements)
            {
                tUpper = currSubElement.ToUpper();
                if (tUpper.Contains(uTerm))
                {
                    results.Add(currManager);
                    break;
                }
            }

        }

        return results;
    }

    public void updateSphereVisibility()
    {
        updateParameterValues();
        Color c = Color.white;
        c.a = sphereVisibility;
        //projSphere.GetComponent<Renderer>().material.color = c;
        projSphereRenderer.material.color = c;

    }

    public void updateEdgeThinningAmount()
    {
        updateParameterValues();

        BaseCurve curve;

        foreach (GameObject obj in outerEdgeList)
        {
            curve = obj.GetComponent<BaseCurve>();

            if (curve == null) continue;

            curve.edgeThinningAmount = edgeThinAmount;
        }

        recalculateEdgeRadii();
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

    public GameObject edgeGameObject;
    public bool updateNextFrame = false;

    private bool __isSameGroup = false;
    private bool __sameGrpSet = false;
    public bool isSameGroup()
    {
        if (__sameGrpSet) return __isSameGroup;
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
