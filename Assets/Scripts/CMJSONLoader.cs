using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMJSONLoader : DataLoader {

    public TmpNode[] nlNodes;
    public TmpLink[] nlLinks;
    public TmpCoord[] nlCoords;

    public TmpCMData[] cmData;

    Dictionary<string, List<string> > m_publisherMap = new Dictionary<string, List<string>>();
    Dictionary<string, Vector3> m_publisherPointMap = new Dictionary<string, Vector3>();
    Dictionary<string, CMData> m_cmMap = new Dictionary<string, CMData>();
    Dictionary<string, EdgeData> m_edgeMap = new Dictionary<string, EdgeData>();
    Dictionary<string, Vector3> m_nodeCoords = new Dictionary<string, Vector3>();

    public bool purgeStanLee = true;

	// Use this for initialization
	void Start () {
		loadData();
        //loadData_2();

    }

    // Update is called once per frame
    void Update () {
		
	}

	new public void loadData()
	{
		var cmAsset = Resources.Load<TextAsset>("ComicsMovies");
        var cmDataArray = JsonUtility.FromJson<TmpCMDataArray>(cmAsset.text);
        cmData = cmDataArray.data;

        NodeInfo[] tmpNodeArray = new NodeInfo[cmData.Length];

        for (int i = 0; i < cmData.Length; i++)
        {
            tmpNodeArray[i] = new NodeInfo();
            tmpNodeArray[i].name = getMovieKey(cmData[i]);
            tmpNodeArray[i].groupName = cmData[i].publisher;

            nodeMap.Add(tmpNodeArray[i].name, tmpNodeArray[i]);
        }

        for (int i = 0; i < cmData.Length; i++)
        //for (int i = 0; i < 2; i++)
        {
            for (int j = i+1; j < cmData.Length; j++)
            {
                int numConnections = 0;

                for (int m = 0; m < cmData[i].roles.Length; m++)
                {
                	if( purgeStanLee && cmData[i].roles[m].actor.Equals("Stan Lee")) continue;

                    for (int n = 0; n < cmData[j].roles.Length; n++)
                    {
                        if( cmData[i].roles[m].actor.Equals(cmData[j].roles[n].actor))
                        {
                            numConnections++;
                        }
                    }
                }

                if( numConnections > 0 )
                {
                    EdgeInfo info = new EdgeInfo();
                    info.startNode = tmpNodeArray[i];
                    info.endNode = tmpNodeArray[j];
                    info.forceValue = Mathf.Sqrt((float)numConnections) + 0.5f;

                    edgeList.Add(info);
                }
            }
        }

        Debug.Log("Number of Edges: " + edgeList.Count);

        base.loadData();
    }

    new public void loadData_2()
	{
		var nlAsset = Resources.Load<TextAsset>("movies");
        var nlDataArray = JsonUtility.FromJson<TmpDataArray>(nlAsset.text);
        nlNodes = nlDataArray.nodes;
        nlLinks = nlDataArray.links;
        nlCoords = nlDataArray.coords;

        string id;
        TmpNode currNode;
        NodeInfo [] tmpNodeInfoArr = new NodeInfo[nlNodes.Length];

        Dictionary<string, int> nodeIdxMap = new Dictionary<string, int>();

        for(int i = 0; i < nlNodes.Length; i++ )
        {
        	currNode = nlNodes[i];
        	tmpNodeInfoArr[i] = new NodeInfo();
        	tmpNodeInfoArr[i].name = currNode.id;
        	tmpNodeInfoArr[i].groupName = "yr: " + currNode.group;

        	nodeIdxMap.Add(currNode.id, i);
        }

        TmpCoord currCoord;
        int currIdx;
        Vector2 v2;
        for(int i = 0; i < nlCoords.Length; i++ )
        {
        	currCoord = nlCoords[i];
        	currIdx = nodeIdxMap[currCoord.id];
        	v2.x = (float)currCoord.x;
        	v2.y = (float)currCoord.y;

        	tmpNodeInfoArr[currIdx].position2 = v2;
        }

        int idx1;
        int idx2;
		TmpLink currLink;
        for(int i = 0; i < nlLinks.Length; i++ )
        {
        	currLink = nlLinks[i];
        	idx1 = nodeIdxMap[currLink.source];
        	idx2 = nodeIdxMap[currLink.target];

        	EdgeInfo edgeInfo = new EdgeInfo();
        	edgeInfo.startNode = tmpNodeInfoArr[idx1];
        	edgeInfo.endNode = tmpNodeInfoArr[idx2];

        	edgeList.Add(edgeInfo);

        }


        foreach(NodeInfo info in tmpNodeInfoArr)
        {
        	nodeMap.Add(info.name, info);
        }


		base.loadData();
	}

	public static string getMovieKey(TmpCMData data)
    {
        return data.movie + " (" + data.year + ")";
    }
}


public class TmpCMDataArray
{
    public TmpCMData[] data;
}

[System.Serializable]
public class TmpCMData
{
    public string comic;
    public string movie;
    public int year;
    public string publisher;
    public string grouping;
    public string distributor;
    public string[] studios;
    public TmpCMRole[] roles;
}


[System.Serializable]
public class TmpCMRole
{
    public string role;
    public string actor;
    public string name;
    public bool active = true;
}


public class TmpDataArray
{
    public TmpNode[] nodes;
    public TmpLink[] links;
    public TmpCoord[] coords;
}


[System.Serializable]
public class TmpNode
{
    public string id;
    public int group;
}

[System.Serializable]
public class TmpLink
{
    public string source;
    public string target;
    public int value;
}

[System.Serializable]
public class TmpCoord
{
    public string id;
    public double x;
    public double y;
}

