using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMJSONLoader : DataLoader {

    public CMNode[] nlNodes;
    public CMLink[] nlLinks;
    public CMCoord[] nlCoords;

    public CMData[] cmData;

    public bool purgeStanLee = true;

    bool useOldMethod = false;

	// Use this for initialization
	void Start () {
        if (useOldMethod) loadData_2();
		else loadData();
    }

    // Update is called once per frame
    void Update () {
		
	}

	new public void loadData()
	{
		var cmAsset = Resources.Load<TextAsset>("ComicsMovies");
        var cmDataArray = JsonUtility.FromJson<CMDataObject>(cmAsset.text);
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

	private void procData()
	{
		/*
		1. Which publisher has produced the most movies?
		2. Which movie has the most connections with other movies?
		3. Which movie has no connections with other movies?
		4. Which movie is connected to the most publishers?
		5. How are Batman Begins (DC) and Avengers (Marvel) connected?
		*/
		Dictionary<string, CMData> keyMovieDataMap = new Dictionary<string, CMData> ();
		Dictionary<string, HashSet<string> > publisherMovieMap = new Dictionary<string, HashSet<string> > ();   // 2
		Dictionary<string, HashSet<CMData> > actorMovieMap = new Dictionary<string, HashSet<CMData> > (); // 
		Dictionary<string, 
	}

    public void loadData_2()
	{
		var nlAsset = Resources.Load<TextAsset>("movies");
        var nlDataArray = JsonUtility.FromJson<CMDataArray>(nlAsset.text);
        nlNodes = nlDataArray.nodes;
        nlLinks = nlDataArray.links;
        nlCoords = nlDataArray.coords;

        CMNode currNode;
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

        CMCoord currCoord;
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
		CMLink currLink;
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

	public static string getMovieKey(CMData data)
    {
        return data.movie + " (" + data.year + ")";
    }
}


public class CMDataObject
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


public class CMDataArray
{
    public CMNode[] nodes;
    public CMLink[] links;
    public CMCoord[] coords;
}


[System.Serializable]
public class CMNode
{
    public string id;
    public int group;
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

