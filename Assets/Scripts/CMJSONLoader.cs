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
        
    }

    // Update is called once per frame
    void Update () {
        
	}

    public override void loadData()
    {
        Debug.Log("loadData CMJSONLoader ");

        var cmAsset = Resources.Load<TextAsset>("ComicsMovies");
        var cmDataArray = JsonUtility.FromJson<CMDataObject>(cmAsset.text);
        cmData = cmDataArray.data;

        NodeInfo[] tmpNodeArray = new NodeInfo[cmData.Length];

        for (int i = 0; i < cmData.Length; i++)
        {
            tmpNodeArray[i] = new NodeInfo();
            tmpNodeArray[i].name = getMovieKey(cmData[i]);
            tmpNodeArray[i].groupName = cmData[i].publisher;
			for (int j = 0; j < cmData[i].roles.Length; j++) 
			{
				tmpNodeArray [i].subElements.Add (cmData [i].roles [j].actor);
			}

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
                	if( purgeStanLee )
                    {
                        if (cmData[i].roles[m].actor.Contains("Stan") && cmData[i].roles[m].actor.Contains("Lee")) continue;
                        
                    }
                    for (int n = 0; n < cmData[j].roles.Length; n++)
                    {
                        if (cmData[i].roles[m].actor.Equals(cmData[j].roles[n].actor))
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
		Dictionary<string, List<string> > publisherMovieMap = new Dictionary<string, List<string> > ();   // 1 (complete)
        Dictionary<string, List<CMData> > movieConnectionMap = new Dictionary<string, List<CMData>>(); // 2 (complete), 3 (complete)
        Dictionary<string, HashSet<string>> moviePublisherMap = new Dictionary<string, HashSet<string>>(); // 4 (complete)
        

        string iKey;
        CMData iData, jData;


        for ( int i = 0; i < cmData.Length; i++ )
        {
            iData = cmData[i];
            iKey = getMovieKey(iData);

            keyMovieDataMap.Add(iKey, iData);

            List<string> pubList;

            if(!publisherMovieMap.TryGetValue(iData.publisher, out pubList))
            {
                pubList = new List<string>();
                publisherMovieMap.Add(iData.publisher, pubList);
                pubList = publisherMovieMap[iData.publisher];
            }

            pubList.Add(iKey);

            List<CMData> connList = new List<CMData>();
            HashSet<string> pubSet = new HashSet<string>();

            bool hasConnection = false;

            for (int j = 0; j < cmData.Length; j++)
            {
                if (j == i) continue;
                jData = cmData[j];
                hasConnection = false;

                for ( int m = 0; m < iData.roles.Length; m++ )
                {
                    if (purgeStanLee && iData.roles[m].actor.Contains("Stan") && iData.roles[m].actor.Contains("Lee")) continue;

                    for ( int n = 0; n < jData.roles.Length; n++ )
                    {
                        if( iData.roles[m].actor.Equals(jData.roles[n].actor))
                        {
                            m += iData.roles.Length;
                            n += jData.roles.Length;
                            hasConnection = true;
                            break;
                        }
                    }
                }

                if( hasConnection )
                {
                    connList.Add(jData);

                    if (iData.publisher.Equals(jData.publisher) || pubSet.Contains(jData.publisher)) continue;

                    pubSet.Add(jData.publisher);
                }
            }



            movieConnectionMap.Add(iKey, connList);
            moviePublisherMap.Add(iKey, pubSet);

        }


        /*
		1. Which publisher has produced the most movies?
		2. Which movie has the most connections with other movies?
		3. Which movie has no connections with other movies?
		4. Which movie is connected to the most publishers?
		5. How are Batman Begins (DC) and Avengers (Marvel) connected?
		*/
        /*
        Dictionary<string, CMData> keyMovieDataMap;
        Dictionary<string, List<string>> publisherMovieMap;
        Dictionary<string, List<CMData>> movieConnectionMap;
        Dictionary<string, HashSet<string>> moviePublisherMap;
        */

        foreach (KeyValuePair<string, HashSet<string>> kv in moviePublisherMap)
        {
            HashSet<string> set = kv.Value;

            if (set.Count == 4)
            {
                //Debug.Log(kv.Key + " has " + set.Count + " publisher connections");    
            }
        }

        foreach (KeyValuePair<string, List<CMData>> kv in movieConnectionMap)
        {
            List<CMData> list = kv.Value;

            if (list.Count > 10)
            {
                //Debug.Log(kv.Key + " has " + list.Count + " connections");    
            }
        }


        foreach (KeyValuePair<string, List<string>> kv in publisherMovieMap)
        {
            List<string> list = kv.Value;
            if( list.Count <= 5 || list.Count > 1 )
            {
                Debug.Log(kv.Key + " has " + list.Count + " movies");     
            }
        }

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

