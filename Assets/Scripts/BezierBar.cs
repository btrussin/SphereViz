using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierBar : MonoBehaviour {

    public int numMajorDivisions = 10;
    public int numMinorDivisions = 30;
    public float radius = 0.02f;

    Color color0;
    Color color1;

	// Use this for initialization
	void Start () {

	}

    public void populateMesh(Vector3[] controlPoints, Color c0, Color c1, bool sphereCoords = false)
    {

    	color0 = c0;
    	color1 = c1;

    	Vector3[] basePoints;
    	Vector3[] baseTangents;

    	if( sphereCoords )
    	{
    		Vector3[] tmpPoints = Utils.getBezierPoints(controlPoints, numMajorDivisions);
    		basePoints =  new Vector3[tmpPoints.Length];
    		baseTangents =  new Vector3[tmpPoints.Length];
    		Vector3 tmpVec;
    		Quaternion rotation;
    		for( int i = 0; i < basePoints.Length; i++ )
    		{
				tmpVec = new Vector3(tmpPoints[i].z, 0.0f, 0.0f);
        		rotation = Quaternion.Euler(0.0f, tmpPoints[i].x, tmpPoints[i].y);
        		basePoints[i] = rotation * tmpVec;
    		}

    		for( int i = 1; i < basePoints.Length; i++ )
    		{
    			tmpVec = basePoints[i] - basePoints[i-1];
    			tmpVec.Normalize();
    			baseTangents[i] = tmpVec;
    		}

    		baseTangents[0] = baseTangents[1];
    	}
    	else
    	{
    		basePoints = Utils.getBezierPoints(controlPoints, numMajorDivisions);
    		baseTangents = Utils.getBezierPointTangents(controlPoints, numMajorDivisions);
    	}
       

        Vector3 currVertex;
        Vector3 tgtDirVector;
        Vector3 currRight = Vector3.one;
        Vector3 currForward;
        Vector3 currUp;

        Vector3 flatDirVector = controlPoints[3] - controlPoints[0];
        flatDirVector.Normalize();

        Vector3[] meshPoints = new Vector3[numMinorDivisions * numMajorDivisions];
        Vector3[] meshNormals = new Vector3[numMinorDivisions * numMajorDivisions];
        Color[] meshColors = new Color[numMinorDivisions * numMajorDivisions];

        int currIdx = 0;

        float rInc = ( color1.r - color0.r ) / (float)(numMajorDivisions-1);
        float gInc = ( color1.g - color0.g ) / (float)(numMajorDivisions-1);
        float bInc = ( color1.b - color0.b ) / (float)(numMajorDivisions-1);
        Color currColor = color0;

        for (int i = 0; i < basePoints.Length; i++)
        {
            baseTangents[i].Normalize();

            currForward = baseTangents[i];
            tgtDirVector = currForward;
            for( int j = i+1; j < basePoints.Length; j++ )
            {
                tgtDirVector = basePoints[j] - basePoints[i];
                tgtDirVector.Normalize();
                float dot = Vector3.Dot(currForward, tgtDirVector);
   
                if (dot <= 0.99f )
                {
                    currRight = Vector3.Cross(tgtDirVector, currForward);
                    break;
                }
            }

            if (Mathf.Abs(Vector3.Dot(currForward, tgtDirVector)) > 0.99f)
            {
                currRight = Vector3.Cross(currForward, flatDirVector);
            }

            currRight.Normalize();

            currUp = Vector3.Cross(currRight, currForward);
            currUp.Normalize();

            currVertex = currUp;

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

        int[] faceList = new int[(numMajorDivisions - 1)*(numMinorDivisions)*6];
        int mainIdx = 0;
        int k = 0;
        for( int i = 0; i < numMajorDivisions-1; i++ )
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
            faceList[k++] = mainIdx + numMinorDivisions + numMinorDivisions-1;

            mainIdx += numMinorDivisions;
        }

       
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = meshPoints;
        mesh.normals = meshNormals;
        mesh.colors = meshColors;
        mesh.triangles = faceList;


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
