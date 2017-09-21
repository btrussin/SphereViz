using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCurve : MonoBehaviour {


    public MeshFilter meshFilter;
    public Mesh mesh = null;

    public int numMajorDivisions = 100;
    public int numMinorDivisions = 30;
    public float radius = 0.02f;

    protected Color color0;
    protected Color color1;
    

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
                    currRight.x = -baseTangents[i].y;
                    currRight.y = baseTangents[i].x;
                    currRight.z = baseTangents[i].z;

                    if (Mathf.Abs(Vector3.Dot(baseTangents[i], currRight)) > 0.95f)
                    {
                        currRight.x = -baseTangents[i].z;
                        currRight.y = baseTangents[i].y;
                        currRight.z = baseTangents[i].x;
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

        Vector3[] rightVecs, upVecs;

        getUpAndRightVectors(basePoints, baseTangents, out upVecs, out rightVecs);

        for (int i = 0; i < basePoints.Length; i++)
        {
            currVertex = upVecs[i];
            currForward = baseTangents[i];

            Quaternion rotation = Quaternion.AngleAxis(360.0f / numMinorDivisions, currForward);

            for (int j = 0; j < numMinorDivisions; j++)
            {
                meshPoints[currIdx] = basePoints[i] + currVertex * radius;
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

    }


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {

    }
}
