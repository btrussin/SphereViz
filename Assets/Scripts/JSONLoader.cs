using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JSONLoader : MonoBehaviour {

    public float radius;
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject bezierPrefab;
    public GameObject bSplinePrefab;

    public bool useBezierBars = true;
    public bool useBSplineBars = true;
    public bool useSLERP = false;

    public CMData[] cmData;
    public CMNode[] nlNodes;
    public CMLink[] nlLinks;
    public CMCoord[] nlCoords;

    Dictionary<string, List<string> > m_publisherMap = new Dictionary<string, List<string>>();
    Dictionary<string, Vector3> m_publisherPointMap = new Dictionary<string, Vector3>();
    Dictionary<string, CMData> m_cmMap = new Dictionary<string, CMData>();
    Dictionary<string, EdgeData> m_edgeMap = new Dictionary<string, EdgeData>();
    Dictionary<string, Vector2> m_nodeCoordsNormalized = new Dictionary<string, Vector2>();
    Dictionary<string, Vector3> m_nodeCoords = new Dictionary<string, Vector3>();
    Dictionary<string, Color> m_catColorMap = new Dictionary<string, Color>();


    // Use this for initialization
    void Start () {
		loadData();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void loadData()
    {
        var cmAsset = Resources.Load<TextAsset>("ComicsMovies");
        var cmDataArray = JsonUtility.FromJson<CMDataArray>(cmAsset.text);
        cmData = cmDataArray.data;

        var nlAsset = Resources.Load<TextAsset>("movies");
        var nlDataArray = JsonUtility.FromJson<NLDataArray>(nlAsset.text);
        nlNodes = nlDataArray.nodes;
        nlLinks = nlDataArray.links;
        nlCoords = nlDataArray.coords;
        
        processData();

        float xMin = 10000000.0f;
        float xMax = -1000000.0f;
        float yMin = 10000000.0f;
        float yMax = -1000000.0f;
        Vector2 v;
        

        for( int i = 0; i < nlCoords.Length; i++ )
        {
            v.x = (float)nlCoords[i].x;
            v.y = (float)nlCoords[i].y;

            if( v.x < xMin ) xMin = v.x;
            if( v.y < yMin ) yMin = v.y;
            if( v.x > xMax ) xMax = v.x;
            if( v.y > yMax ) yMax = v.y;
        }

        float xRange = xMax - xMin;
        float yRange = yMax - yMin;

        for( int i = 0; i < nlCoords.Length; i++ )
        {
            v.x = ((((float)nlCoords[i].x)-xMin)/xRange)*2.0f - 1.0f;
            v.y = ((((float)nlCoords[i].y)-yMin)/yRange)*2.0f - 1.0f;

            m_nodeCoordsNormalized.Add(nlCoords[i].id, v);
        }

        Vector3 currVec;
        foreach( KeyValuePair<string, Vector2> kv in m_nodeCoordsNormalized)
        {
            currVec = get3DPointProjectionSphere(kv.Value, radius);
            m_nodeCoords.Add(kv.Key, currVec);
        }

        populatePublisherPoints();
        populatePublisherColors();

        populatePts();
        populateEdges();

    }

    void populatePublisherPoints()
    {
    	foreach(KeyValuePair<string, List<string> > kv in m_publisherMap)
    	{
    		Vector3 v = Vector3.zero;

    		foreach( string s in kv.Value )
    		{
    			v += m_nodeCoords[s];
    		}

    		v /= (float)kv.Value.Count;
    		v.Normalize();
    		v *= radius * 1.2f;

    		m_publisherPointMap.Add(kv.Key, v);

    	}
    }

    void populatePublisherColors()
    {
    	
    	Color[] colors = ColorUtils.getColorPalette();
    	ColorUtils.randomizeColorPalette(colors, 6);
    	Color currColor;
    	int idx = 0;
    	int maxIdx = colors.Length;

    	foreach(KeyValuePair<string, Vector3> kv in m_publisherPointMap)
    	{
    		currColor = colors[idx % maxIdx];
    		m_catColorMap.Add(kv.Key, currColor);
    		idx++;
    	}
    }

    void populatePts()
    {
        foreach (KeyValuePair<string, Vector3> kv in m_nodeCoords)
        {
            GameObject point = (GameObject)Instantiate(nodePrefab);
            point.transform.position = kv.Value;
        }
    }

    void populateEdges()
    {
        Vector3[] basePts = new Vector3[4];
        
        Vector3[] pts;
        Vector3 tVec1, tVec2;
        
        string fromKey, toKey;
        Color c0;
        Color c1;

        float barRadius = radius / 125.0f;

        foreach (KeyValuePair<string, EdgeData> kv in m_edgeMap)
        {

            fromKey = getMovieKey(kv.Value.dataFrom);
            toKey = getMovieKey(kv.Value.dataTo);

            c0 = m_catColorMap[kv.Value.dataFrom.publisher];
            c1 = m_catColorMap[kv.Value.dataTo.publisher];

            if( !useBSplineBars )
            {
            	basePts[0] = m_nodeCoords[fromKey];
            	basePts[basePts.Length-1] = m_nodeCoords[toKey];

            	basePts[1] = basePts[0] * 2.0f;
            	basePts[basePts.Length - 2] = basePts[basePts.Length - 1] * 2.0f;
            }

            if( useBSplineBars )
            {
            	
            	tVec1 = m_publisherPointMap[kv.Value.dataFrom.publisher];
            	tVec2 = m_publisherPointMap[kv.Value.dataTo.publisher];
            	Vector3 dirVec = tVec2 - tVec1;

            	if( dirVec.magnitude < 0.0001f )
            	{
            		basePts = new Vector3[4];

            		basePts[0] = m_nodeCoords[fromKey];
            		basePts[1] = basePts[0] * 1.1f;
                		
                	basePts[3] = m_nodeCoords[toKey];
                	basePts[2] = basePts[3] * 1.1f;

                	GameObject edge = (GameObject)Instantiate(bezierPrefab);
                	BezierBar bezBar = edge.GetComponent<BezierBar>();
                	bezBar.populateMesh(basePts, c0, c1);
                	
            	}
				else
				{
					basePts = new Vector3[7];

            		basePts[0] = m_nodeCoords[fromKey];
            		basePts[6] = m_nodeCoords[toKey];

                	basePts[1] = tVec1 * 1.1f;
                	basePts[2] = tVec1 * 1.4f;

                	basePts[4] = tVec2 * 1.4f;
                	basePts[5] = tVec2 * 1.1f;

                	basePts[3] = (basePts[4] + basePts[2]) * 0.5f;

                    GameObject edge = (GameObject)Instantiate(bSplinePrefab);
                	BasisSpline bspline = edge.GetComponent<BasisSpline>();
                    bspline.radius = barRadius;
                	bspline.useSphericalInterpolation = useSLERP;
                	bspline.init(basePts, c0, c1);

				}                
            

                
            }
            else if(useBezierBars)
            {
                GameObject edge = (GameObject)Instantiate(bezierPrefab);
                BezierBar bezBar = edge.GetComponent<BezierBar>();
                bezBar.radius = barRadius;
                bezBar.populateMesh(basePts, c0, c1);
            }
            else
            {
                GameObject edge = (GameObject)Instantiate(edgePrefab);
                LineRenderer rend = edge.GetComponent<LineRenderer>();

                pts = Utils.getBezierPoints(basePts, 100);
                rend.SetPositions(pts);
                rend.startColor = Color.white;
                rend.endColor = Color.white;
            }

        }
    }

	// v: 2D graph coordinate where values are between -1 and 1
	// r: radius of the sphere
    Vector3 get3DPointProjectionSphere(Vector2 v, float r)
    {
        Vector3 result = new Vector3(r, 0.0f, 0.0f);

        float horizontalAngle = v.x * 180.0f;
        float verticalAngle = v.y * 90.0f;

        Quaternion rotation = Quaternion.Euler(0.0f, horizontalAngle, verticalAngle);

        result = rotation * result;
        return result;
    }

	void processData()
	{
        string key;
        string publisher;
        List<string> list;
        foreach(CMData data in cmData)
        {
            key = getMovieKey(data);
            publisher = data.publisher;


            m_cmMap.Add(key, data);

            if( !m_publisherMap.TryGetValue(publisher, out list) )
            {
                list = new List<string>();
                m_publisherMap.Add(publisher, list);
            }

            list.Add(key);
        }

        string key1;
        string key2;
        int numConnections;
        for( int i = 0; i < cmData.Length; i++ )
        {
            for( int j = i+1; j < cmData.Length; j++ )
            {
                numConnections = 0;
                for( int k = 0; k < cmData[i].roles.Length; k++ )
                {
                    for( int l = 0; l < cmData[j].roles.Length; l++ )
                    {
                        if( cmData[i].roles[k].actor.Equals(cmData[j].roles[l].actor))
                        {
                            numConnections++;
                        }
                    }
                }

                if( numConnections == 0 ) continue;

                key1 = getEdgeKey(cmData[i], cmData[j]);
                key2 = getEdgeKey(cmData[j], cmData[i]);
                EdgeData edgeData = new EdgeData();
                edgeData.dataFrom = cmData[i];
                edgeData.dataTo = cmData[j];
                edgeData.numConnections = numConnections;
                edgeData.innerEdge = cmData[i].publisher.Equals(cmData[j].publisher);

                m_edgeMap.Add(key1, edgeData);
                m_edgeMap.Add(key2, edgeData);


            }
        }

        Debug.Log("Num Edges: " + (m_edgeMap.Count/2));
        
	}

    public static string getMovieKey(CMData data)
    {
        return data.movie + " " + data.year;
    }

    public static string getEdgeKey(CMData d1, CMData d2)
    {
        return getMovieKey(d1)+"|"+getMovieKey(d2);
    }



}
public class EdgeData
{
    public CMData dataFrom;
    public CMData dataTo;
    public int numConnections;
    public bool innerEdge;
}

public class CMDataArray
{
    public CMData[] data;
}

[System.Serializable]
public class CMData
{
    public string comic;
    public string movie;
    public int year;
    public string publisher;
    public string grouping;
    public string distributor;
    public string[] studios;
    public CMRole[] roles;
}


[System.Serializable]
public class CMRole
{
    public string role;
    public string actor;
    public string name;
    public bool active = true;
}

[System.Serializable]
public class CMType
{
    public uint type;
    public bool active = true;
}


public class NLDataArray
{
    public CMNode[] nodes;
    public CMLink[] links;
    public CMCoord[] coords;
}

[System.Serializable]
public class CMNode
{
    public string id;
    public int year;
}

[System.Serializable]
public class CMLink
{
    public string source;
    public string target;
    public int value;
}

[System.Serializable]
public class CMCoord
{
    public string id;
    public double x;
    public double y;
}




