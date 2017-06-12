﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JSONLoader : MonoBehaviour {

    public float radius;
    public GameObject nodePrefab;
    public GameObject edgePrefab;

    public CMData[] cmData;
    public NLNode[] nlNodes;
    public NLLink[] nlLinks;
    public NLCoord[] nlCoords;

    Dictionary<string, List<string> > m_publisherMap = new Dictionary<string, List<string>>();
    Dictionary<string, CMData> m_cmMap = new Dictionary<string, CMData>();
    Dictionary<string, EdgeData> m_edgeMap = new Dictionary<string, EdgeData>();
    Dictionary<string, Vector2> m_nodeCoordsNormalized = new Dictionary<string, Vector2>();
    Dictionary<string, Vector3> m_nodeCoords = new Dictionary<string, Vector3>();

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
            currVec = get3DPointProjection(kv.Value, radius);
            m_nodeCoords.Add(kv.Key, currVec);
        }

        populatePts();
        populateEdges();

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
        string fromKey, toKey;

        foreach (KeyValuePair<string, EdgeData> kv in m_edgeMap)
        {
            GameObject edge = (GameObject)Instantiate(edgePrefab);
            LineRenderer rend = edge.GetComponent<LineRenderer>();

            fromKey = getMovieKey(kv.Value.dataFrom);
            toKey = getMovieKey(kv.Value.dataTo);

            basePts[0] = m_nodeCoords[fromKey];
            basePts[3] = m_nodeCoords[toKey];

            basePts[1] = basePts[0] * 2.0f;
            basePts[2] = basePts[3] * 2.0f;

            pts = getBezierPoints(basePts, 100);


            rend.SetPositions(pts);
            rend.startColor = Color.white;
            rend.endColor = Color.white;
        }
    }

    Vector3 get3DPointProjection(Vector2 v, float r)
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



    public static Vector3[] getBezierPoints(Vector3[] basePts, int size)
    {
        //TODO Could possibly use bundling with the control points A0, B0, C0, D0 and then with future control points

        //P0' = BS * P0 + (1 - BS) * (P0 + 0/(N - 1) * (P(N-1) - P0))
        //P1' = BS * P1 + (1 - BS) * (P0 + 1/(N - 1) * (P(N-1) - P0))

        float h = 1.0f / (float)(size - 1);
        float h_2 = h * h;

        //          A0                   B0              C0       D0
        // t^3(-p0+3p1-3p2+p3) + t^2(3p0-6p1+3p2) + t(-3p0+3p1) + p0
        Vector3 A0 = basePts[0] * -1.0f + basePts[1] * 3.0f + basePts[2] * -3.0f + basePts[3];
        Vector3 B0 = basePts[0] * 3.0f + basePts[1] * -6.0f + basePts[2] * 3.0f;
        Vector3 C0 = basePts[0] * -3.0f + basePts[1] * 3.0f;

        //      A1            B1               C1
        // t^2(3A0h) + t(3A0h^2+2B0h) + (A0h^3+B0h^2+C0h)
        Vector3 A1 = A0 * 3.0f * h;
        Vector3 B1 = A0 * 3.0f * h_2 + B0 * 2.0f * h;
        Vector3 C1 = A0 * h * h_2 + B0 * h_2 + C0 * h;

        //    A2          B2
        // t(2A1h) + (A1h^2+B1h)
        Vector3 A2 = A1 * 2.0f * h;
        Vector3 B2 = A1 * h_2 + B1 * h;

        //  A3
        // (A2h)
        Vector3 A3 = A2 * h;


        // D1 = C1
        Vector3 D1 = C1;

        // D2 = B2
        Vector3 D2 = B2;

        // D3 = A3
        Vector3 D3 = A3;

        Vector3[] pts = new Vector3[size];
        pts[0] = basePts[0];

        for (int i = 1; i < size; i++)
        {
            pts[i] = pts[i - 1] + D1;
            D1 += D2;
            D2 += D3;
        }

        return pts;
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
    public NLNode[] nodes;
    public NLLink[] links;
    public NLCoord[] coords;
}

[System.Serializable]
public class NLNode
{
    public string id;
    public int year;
}

[System.Serializable]
public class NLLink
{
    public string source;
    public string target;
    public int value;
}

[System.Serializable]
public class NLCoord
{
    public string id;
    public double x;
    public double y;
}
 