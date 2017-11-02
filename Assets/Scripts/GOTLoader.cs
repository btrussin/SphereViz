using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOTLoader : DataLoader
{

    // Use this for initialization
    void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void loadData()
    {
        TextAsset fileData = Resources.Load<TextAsset>("got_popular_chars-Baseline-0");
        string fileDataStr = fileData.text;
        string[] lines = fileData.text.Split("\n"[0]);
        string[] header = lines[0].Split(","[0]);
        string tmpLine;
        string[] line;

        NodeInfo[] l_nodeArray = new NodeInfo[lines.Length-1];

        NodeInfo tmpNode;
        string data;

        for ( int lineNum = 1; lineNum < lines.Length; lineNum++ )       
        {
            tmpLine = lines[lineNum];
            line = new string[header.Length];
            int idx = 0;

            string s = "";

            bool inQuotes = false;

            for( int i = 0; i < tmpLine.Length; i++ )
            {
                char c = tmpLine[i];

                if (c == ',')
                {
                    if (inQuotes) s += c;
                    else
                    {
                        line[idx] = s;
                        s = "";
                        idx++;
                    }
                }
                else if (c == '"') inQuotes = !inQuotes;
                else
                {
                    s += c;
                }
            }
            
            if( s.Length > 0 ) line[idx] = s;

            tmpNode = new NodeInfo();
            tmpNode.name = line[2];
            if (line[14].Length < 1) tmpNode.groupName = "[none]";
            else tmpNode.groupName = line[14];

            string[] items;
            int itemIdx;

            for (int dataIdx = 3; dataIdx < line.Length; dataIdx++)
            {
                data = line[dataIdx];

                if (data.Length == 0) continue;

                switch(dataIdx)
                {
                    case 3: // title
                    case 4: // role
                    //case 5: // language
                    //case 7: // culture
                        items = data.Split(","[0]);
                        for( itemIdx = 0; itemIdx < items.Length; itemIdx++)
                        {
                            tmpNode.subElements.Add(header[dataIdx] + ": " + items[itemIdx]);
                        }
                        break;

                    case 14:
                        break;
                    default:
                        //tmpNode.subElements.Add(header[dataIdx] + ": " + data);
                        break;
                }
            }

            l_nodeArray[lineNum - 1] = tmpNode;
            nodeMap.Add(tmpNode.name, tmpNode);

            
        }

        for (int i = 0; i < l_nodeArray.Length; i++)
        {
            for (int j = i + 1; j < l_nodeArray.Length; j++)
            {
                int numConnections = 0;

                foreach (string s1 in l_nodeArray[i].subElements)
                {
                    foreach (string s2 in l_nodeArray[j].subElements)
                    {
                        if (s1.Equals(s2)) numConnections++;
                    }
                }

                if (numConnections > 0)
                {
                    EdgeInfo info = new EdgeInfo();
                    info.startNode = l_nodeArray[i];
                    info.endNode = l_nodeArray[j];
                    info.forceValue = Mathf.Sqrt((float)numConnections) + 0.5f;

                    edgeList.Add(info);
                }
            }
        }

    }
}
