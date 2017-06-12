using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JSONLoader : MonoBehaviour {

	public CMData[] cmData;
    public NLNode[] nlNodes;
    public NLLink[] nlLinks;
    public NLCoord[] nlCoords;

    Dictionary<string, List<string> > m_publisherMap = new Dictionary<string, List<string>>();
    Dictionary<string, CMData> m_cmMap = new Dictionary<string, CMData>();
    Dictionary<string, EdgeData> m_edgeMap = new Dictionary<string, EdgeData>();
    Dictionary<string, Vector2> m_nodeCoords = new Dictionary<string, Vector2>();

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
            v.x = (((float)nlCoords[i].x)-xMin)/xRange;
            v.y = (((float)nlCoords[i].y)-yMin)/yRange;

            m_nodeCoords.Add(nlCoords[i].id, v);
        }

    }

    Vector3 get3DPointProjection(Vector2 v, float radius)
    {
        Vector3 result = new Vector3(radius, 0.0f, 0.0f);

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
                edgeData.dataFrom = cmData[j];
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