using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubNodeManager : MonoBehaviour {

    Dictionary<string, GameObject> innerConnectionMap = new Dictionary<string, GameObject>();

    public MeshFilter meshFilter;
    public Mesh mesh = null;

    static bool verticesCalculated = false;

    static Vector3[] meshVertices;
    static int[] meshTriangles;

    static void setupMeshElements()
    {
        float flatDim = 0.3333f;
        float tallDim = 0.5f;

        meshVertices = new Vector3[24];

        int idx = 0;
        meshVertices[idx++] = new Vector3(-flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(0f, tallDim, 0f);

        meshVertices[idx++] = new Vector3(-flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(-flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(0f, tallDim, 0f);

        meshVertices[idx++] = new Vector3(flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(-flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(0f, tallDim, 0f);

        meshVertices[idx++] = new Vector3(flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(0f, tallDim, 0f);


        meshVertices[idx++] = new Vector3(flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(-flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(0f, -tallDim, 0f);

        meshVertices[idx++] = new Vector3(-flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(-flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(0f, -tallDim, 0f);

        meshVertices[idx++] = new Vector3(-flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(0f, -tallDim, 0f);

        meshVertices[idx++] = new Vector3(flatDim, 0f, -flatDim);
        meshVertices[idx++] = new Vector3(flatDim, 0f, flatDim);
        meshVertices[idx++] = new Vector3(0f, -tallDim, 0f);


        meshTriangles = new int[meshVertices.Length];
        for (idx = 0; idx < meshTriangles.Length; idx++) meshTriangles[idx] = idx;

        verticesCalculated = true;
    }

    // Use this for initialization
    void Start () {

        if (mesh == null) mesh = new Mesh();
        meshFilter.mesh = mesh;

        if (!verticesCalculated) setupMeshElements();
        
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void addInnerConnection(ConnectionManager conn, GameObject obj)
    {
        if (innerConnectionMap.ContainsKey(conn.name)) return;
        innerConnectionMap.Add(conn.name, obj);
    }

    public void removeInnerConnection(ConnectionManager conn)
    {
        if (innerConnectionMap.ContainsKey(conn.name)) innerConnectionMap.Remove(conn.name);
    }

    public void destroyAndRemoveAllInnerConnections()
    {
        ConnectionManager connMan;
        foreach(KeyValuePair<string, GameObject> kv in innerConnectionMap)
        {
            connMan = kv.Value.GetComponent<ConnectionManager>();
            if( connMan != null )
            {
                connMan.destroyAttachedObjects();
                connMan.removeFromNodeManagers();
            }
            Destroy(kv.Value);
        }

        innerConnectionMap.Clear();
    }

    public void hideAllInnerEdgeNodes()
    {
        ConnectionManager connMan;
        foreach (KeyValuePair<string, GameObject> kv in innerConnectionMap)
        {
            connMan = kv.Value.GetComponent<ConnectionManager>();
            if (connMan != null) connMan.hideEndSubNodes();
        }

    }

    public Dictionary<string, GameObject> getInnerConnectionMap()
    {
        return innerConnectionMap;
    }

    public void updateConnectionEdgeThickness(float barRadius)
    {
        foreach (KeyValuePair<string, GameObject> kv in innerConnectionMap)
        {
            ConnectionManager connMan = kv.Value.GetComponent<ConnectionManager>();
            if (connMan != null) connMan.updateEdge();
        }
    }

    public void updateConnectionCenterNodeScale(Vector3 scale)
    {
        foreach (KeyValuePair<string, GameObject> kv in innerConnectionMap)
        {
            ConnectionManager connMan = kv.Value.GetComponent<ConnectionManager>();
            if (connMan != null) connMan.updateCenterPointScale(scale);
        }
    }

    public void setMeshColors(Color color)
    {
        if (mesh == null) mesh = meshFilter.mesh;
        Color[] meshColors = new Color[mesh.vertexCount];
        Color[] emissionColors = new Color[mesh.vertexCount];

        for (int i = 0; i < meshColors.Length; i++)
        {
            meshColors[i] = color;
        }

        mesh.colors = meshColors;
    }
}
