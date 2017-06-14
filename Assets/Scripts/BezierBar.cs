using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierBar : MonoBehaviour {

    public int numMajorDivisions = 100;
    public int numMinorDivisions = 30;
    public float radius = 0.02f;

	// Use this for initialization
	void Start () {
        
	}

    public void populateMesh(Vector3[] controlPoints)
    {

        Vector3[] basePoints = Utils.getBezierPoints(controlPoints, numMajorDivisions);
        Vector3[] baseTangents = Utils.getBezierPointTangents(controlPoints, numMajorDivisions);

        Vector3 currVertex;
        Vector3 tgtDirVector;
        Vector3 currRight = Vector3.one;
        Vector3 currForward;
        Vector3 currUp;

        Vector3 flatDirVector = controlPoints[3] - controlPoints[0];
        flatDirVector.Normalize();

        Vector3[] meshPoints = new Vector3[numMinorDivisions * numMajorDivisions];
        Vector3[] meshNormals = new Vector3[numMinorDivisions * numMajorDivisions];

        int currIdx = 0;

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

            currVertex = currUp * radius;

            Quaternion rotation = Quaternion.AngleAxis(360.0f / numMinorDivisions, currForward);

            for (int j = 0; j < numMinorDivisions; j++)
            {
                meshPoints[currIdx] = currVertex + basePoints[i];
                meshNormals[currIdx] = currVertex;
                meshNormals[currIdx].Normalize();

                currVertex = rotation * currVertex;
                currIdx++;
            }
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
        mesh.triangles = faceList;


    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
