using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOTLoader : DataLoader
{


    [Header("Data Set (0-4)")]
    [Tooltip("Valid values (0-4)")]

    public int dataSetIndex = 0;

    public string groupName;

    public List<string> egdeNameList;


    [Header("Attributes To Link By")]
    public bool uuidHeader;
    public bool popularityHeader;
    public bool nameHeader;
    public bool titleHeader;
    public bool roleHeader;
    public bool languageHeader;
    public bool genderHeader;
    public bool cultureHeader;
    public bool kingdomHeader;
    public bool dateOfBirthHeader;
    public bool DateofdeathHeader;
    public bool ageHeader;
    public bool motherHeader;
    public bool fatherHeader;
    public bool heirHeader;
    public bool houseHeader;
    public bool secondHouseHeader;
    public bool spouseHeader;
    public bool killerHeader;


    // Use this for initialization
    void Start () {
        


    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void populateEdgeList()
    {
        if (uuidHeader) egdeNameList.Add("uuid");
        if (popularityHeader) egdeNameList.Add("popularity");
        if (nameHeader) egdeNameList.Add("name");
        if (titleHeader) egdeNameList.Add("title");
        if (roleHeader) egdeNameList.Add("role");
        if (languageHeader) egdeNameList.Add("language");
        if (genderHeader) egdeNameList.Add("gender");
        if (cultureHeader) egdeNameList.Add("culture");
        if (kingdomHeader) egdeNameList.Add("kingdom");
        if (dateOfBirthHeader) egdeNameList.Add("dateOfBirth");
        if (DateofdeathHeader) egdeNameList.Add("DateoFdeath");
        if (ageHeader) egdeNameList.Add("age");
        if (motherHeader) egdeNameList.Add("mother");
        if (fatherHeader) egdeNameList.Add("father");
        if (heirHeader) egdeNameList.Add("heir");
        if (houseHeader) egdeNameList.Add("house");
        if (secondHouseHeader) egdeNameList.Add("second house");
        if (spouseHeader) egdeNameList.Add("spouse");
        if (killerHeader) egdeNameList.Add("killer");
    }

    int getDataSetIndex()
    {
        if (dataSetIndex < 0) return 0;
        else if (dataSetIndex > 4) return 4;
        else return dataSetIndex;
    }

    public override void loadData()
    {
        populateEdgeList();

        TextAsset fileData = Resources.Load<TextAsset>("got_popular_chars-" + getDataSetIndex());
        string fileDataStr = fileData.text;
        string[] lines = fileData.text.Split("\n"[0]);
        string[] header = lines[0].Split(","[0]);
        string tmpLine;
        string[] line;

        NodeInfo[] l_nodeArray = new NodeInfo[lines.Length-1];

        NodeInfo tmpNode;
        string data;

        int groupIdx = 0;

        List<int> grpIdxList = new List<int>();

        for (int i = 0; i < header.Length; i++)
        {
            if (header[i].Equals(groupName)) groupIdx = i;

            else if(egdeNameList.Contains(header[i])) grpIdxList.Add(i);
           
        }

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
            if (line[groupIdx].Length < 1) tmpNode.groupName = "[none]";
            else tmpNode.groupName = line[groupIdx];

            string[] items;
            int itemIdx;

            

            for (int dataIdx = 3; dataIdx < line.Length; dataIdx++)
            {
                data = line[dataIdx];

                if (data.Length == 0) continue;

                if(grpIdxList.Contains(dataIdx))
                {
                    items = data.Split(","[0]);
                    for (itemIdx = 0; itemIdx < items.Length; itemIdx++)
                    {
                        tmpNode.subElements.Add(header[dataIdx] + ": " + items[itemIdx]);
                    }
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
