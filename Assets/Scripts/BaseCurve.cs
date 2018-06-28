using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCurve : MonoBehaviour {

    public Vector3[] controlPoints;

    public MeshFilter meshFilter;
    public Mesh mesh = null;

    public int numMajorDivisions = 100;
    public int numMinorDivisions = 30;
    public float radius = 0.02f;

    public bool useSphericalInterpolation = false;

    protected Color color0;
    protected Color color1;

    protected Vector3[] m_basePoints;
    protected Vector3[] m_baseTangents;
    protected Vector3[] m_upVectors;
    protected Vector3[] m_rightVectors;

    protected bool currentlyThinned = false;
    protected float origThickRadius;

    public float edgeThinningAmount = 0.1f;

    public highlightState currHighlightState = highlightState.ONE_HOP;

    protected GameObject nodeA = null;
    protected GameObject nodeB = null;

    protected NodeManager nodeManagerA;
    protected NodeManager nodeManagerB;

    protected Material objectMaterial = null;

    protected Vector3[] basePoints;
    protected Vector3[] baseTangents;


    protected void getUpAndRightVectors(Vector3[] basePoints, Vector3[] baseTangents, out Vector3[] upVecs, out Vector3[] rtVecs)
    {
        rtVecs = new Vector3[basePoints.Length];
        upVecs = new Vector3[basePoints.Length];

        int i, j;
        Vector3 currRight = Vector3.one;
        Vector3 targetForward = Vector3.one;

        for( i = 0; i < basePoints.Length; i++ )
        {
            if( i == 0 )
            {
                targetForward = baseTangents[i];
                bool foundSuitableRightVec = false;
                for( j = 1; j < basePoints.Length; j++ )
                {
                    targetForward = basePoints[j] - basePoints[0];
                    targetForward.Normalize();
                    if( Vector3.Dot(baseTangents[i], targetForward) <= 0.95f )
                    {
                        currRight = Vector3.Cross(targetForward, baseTangents[i]);
                        foundSuitableRightVec = true;
                        break;
                    }
                }

                if(!foundSuitableRightVec)
                {
                    currRight.x = -baseTangents[i].z;
                    currRight.y = baseTangents[i].y;
                    currRight.z = baseTangents[i].x;

                    if (Mathf.Abs(Vector3.Dot(baseTangents[i], currRight)) > 0.95f)
                    {
                        currRight.x = -baseTangents[i].y;
                        currRight.y = baseTangents[i].x;
                        currRight.z = baseTangents[i].z;

                        if (Mathf.Abs(Vector3.Dot(baseTangents[i], currRight)) > 0.95f)
                        {
                            currRight.x = baseTangents[i].x;
                            currRight.y = baseTangents[i].z;
                            currRight.z = -baseTangents[i].y;
                        }
                    }
                }
            }
            else
            {
                currRight = Vector3.Cross(baseTangents[i], upVecs[i - 1]);
            }

            currRight.Normalize();

            rtVecs[i] = currRight;

            upVecs[i] = Vector3.Cross(currRight, baseTangents[i]);
            upVecs[i].Normalize();

        }


    }

    protected void populateCurveBarMesh(Vector3[] basePoints, Vector3[] baseTangents)
    {
        m_basePoints = new Vector3[basePoints.Length];
        m_baseTangents = new Vector3[baseTangents.Length];

        for (int i = 0; i < m_basePoints.Length; i++) m_basePoints[i] = basePoints[i];
        for (int i = 0; i < m_baseTangents.Length; i++) m_baseTangents[i] = baseTangents[i];

        populateCurveBarMesh();
    }

    protected void populateCurveBarMesh()
    {
        Vector3 currVertex;
        Vector3 currForward;

        Vector3[] meshPoints = new Vector3[numMinorDivisions * numMajorDivisions];
        Vector3[] meshNormals = new Vector3[numMinorDivisions * numMajorDivisions];
        Color[] meshColors = new Color[numMinorDivisions * numMajorDivisions];

        int currIdx = 0;

        float rInc = (color1.r - color0.r) / (float)(numMajorDivisions - 1);
        float gInc = (color1.g - color0.g) / (float)(numMajorDivisions - 1);
        float bInc = (color1.b - color0.b) / (float)(numMajorDivisions - 1);
        Color currColor = color0;

       // Vector3[] rightVecs, upVecs;

        getUpAndRightVectors(m_basePoints, m_baseTangents, out m_upVectors, out m_rightVectors);

        for (int i = 0; i < m_basePoints.Length; i++)
        {
            currVertex = m_upVectors[i];
            currForward = m_baseTangents[i];

            Quaternion rotation = Quaternion.AngleAxis(360.0f / numMinorDivisions, currForward);

            for (int j = 0; j < numMinorDivisions; j++)
            {
                meshPoints[currIdx] = m_basePoints[i] + currVertex * radius;
                meshNormals[currIdx] = currVertex;
                meshColors[currIdx] = currColor;

                currVertex = rotation * currVertex;
                currVertex.Normalize();
                currIdx++;
            }

            currColor.r += rInc;
            currColor.g += gInc;
            currColor.b += bInc;
        }


        int[] faceList = new int[(numMajorDivisions - 1) * (numMinorDivisions) * 6];
        int mainIdx = 0;
        int k = 0;

        for (int i = 0; i < numMajorDivisions - 1; i++)
        {
            for (int j = 0; j < (numMinorDivisions - 1); j++)
            {
                faceList[k++] = mainIdx + j;
                faceList[k++] = mainIdx + j + 1;
                faceList[k++] = mainIdx + j + numMinorDivisions + 1;

                faceList[k++] = mainIdx + j;
                faceList[k++] = mainIdx + j + numMinorDivisions + 1;
                faceList[k++] = mainIdx + j + numMinorDivisions;
            }

            faceList[k++] = mainIdx + numMinorDivisions - 1;
            faceList[k++] = mainIdx;
            faceList[k++] = mainIdx + numMinorDivisions;

            faceList[k++] = mainIdx + numMinorDivisions - 1;
            faceList[k++] = mainIdx + numMinorDivisions;
            faceList[k++] = mainIdx + numMinorDivisions + numMinorDivisions - 1;

            mainIdx += numMinorDivisions;


        }

        if (mesh == null) mesh = new Mesh();
        //mesh = new Mesh();
        meshFilter.mesh = mesh;
        mesh.vertices = meshPoints;
        mesh.normals = meshNormals;
        mesh.colors = meshColors;
        mesh.triangles = faceList;
        

        meshFilter.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one* 10.0f);

        //Material mat = gameObject.GetComponent<Renderer>().material;
        //mat.SetFloat("_Highlight", 10.0f);

    }

    public void refreshVertices()
    {
        Vector3 currVertex;
        Vector3 currForward;

        Vector3[] meshPoints = new Vector3[numMinorDivisions * numMajorDivisions];

        int currIdx = 0;


        for (int i = 0; i < m_basePoints.Length; i++)
        {
            currVertex = m_upVectors[i];
            currForward = m_baseTangents[i];

            Quaternion rotation = Quaternion.AngleAxis(360.0f / numMinorDivisions, currForward);

            for (int j = 0; j < numMinorDivisions; j++)
            {
                meshPoints[currIdx] = m_basePoints[i] + currVertex * radius;

                currVertex = rotation * currVertex;
                currVertex.Normalize();
                currIdx++;
            }

           
        }

        meshFilter.mesh = mesh;
        mesh.vertices = meshPoints;

    }


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(Vector3[] bPts, Transform transform = null)
    {

    }

    public void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {

    }

    public virtual void updateHighlightState() { }
    public virtual void updateHighlightState(highlightState state) { }

    public virtual void updateRadiusBasedOnHighlightState() { }
    public virtual void updateRadiusBasedOnHighlightState(highlightState state) { }

    protected void thinTheEdge()
    {
        if (currentlyThinned) return;

        origThickRadius = radius;
        radius *= edgeThinningAmount;

        refreshVertices();

        currentlyThinned = true;
    }

    protected void restoreTheEdgeToOriginalThickness()
    {
        if (!currentlyThinned) return;
        radius = origThickRadius;

        refreshVertices();
        currentlyThinned = false;
    }

    public virtual void updateBarRadius(float r)
    {
        Transform parentTrans = transform.parent;
        transform.SetParent(null);

        if (currentlyThinned)
        {
            origThickRadius = r;
            radius = origThickRadius * edgeThinningAmount;
        }
        else
        {
            radius = r;
        }

        refreshVertices();

        transform.SetParent(parentTrans);
    }
}
