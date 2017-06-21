using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSpline : MonoBehaviour 
{
    public MeshFilter meshFilter;

    public int numMajorDivisions = 100;
    public int numMinorDivisions = 30;
    public float radius = 0.02f;

    public Vector3[] basisPoints;
    protected float[] basisWeights;
    protected int n = 0; // number of control points (zero-based)
    protected int m = 0; // number of weights
    protected int p = 0;

    protected Vector3[] bsPoints;
    protected Vector3[] bsTangents;

    public bool useSphericalInterpolation = false;

    

    // Use this for initialization
    void Start () {

        init();
    }

    public void init()
    {
        if (basisPoints == null || basisPoints.Length < 4) return;

        setup();
        populateMesh();
    }

    public void init(Vector3[] bPts)
    {
        basisPoints = new Vector3[bPts.Length];
        for( int i = 0; i < bPts.Length; i++ )
        {
            basisPoints[i] = bPts[i];
        }

        init();
    }

    protected void setup()
    {
        n = basisPoints.Length - 1;

        int numOutWts = 4;
        int numInnerWts = n - 3;
        m = numOutWts * 2 + numInnerWts - 1;
        basisWeights = new float[m + 1];
        int i = 0;
        for (i = 0; i < numOutWts; i++)
        {
            basisWeights[i] = 0f;
            basisWeights[m - i] = 1f;
        }

        float inc = 1f / (float)(numInnerWts + 1);
        for (int j = 0; j < numInnerWts; j++)
        {
            basisWeights[i + j] = inc * (j + 1);
        }


        p = m - n - 1;

        Debug.Log("M: " + m);
        Debug.Log("N: " + n);
        Debug.Log("P: " + p);


        calcBSplinePoints();
        calcBSplineTangents();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void calcBSplinePoints()
    {
        float[] uVals = new float[numMajorDivisions];
        float inc = 1f / (uVals.Length - 1);
        for( int i = 0; i < uVals.Length; i++ )
        {
            uVals[i] = (float)i * inc;
        }

        bsPoints = new Vector3[uVals.Length];

        for( int i = 0; i < uVals.Length; i++ )
        {
            bsPoints[i] = getPN(uVals[i]);
        }
    }

    void calcBSplineTangents()
    {
        bsTangents = new Vector3[bsPoints.Length];

        Vector3 prev, next;

        for( int i = 0; i < bsPoints.Length; i++ )
        {
            if( i == 0 )
            {
                prev = bsPoints[1] - bsPoints[0];
                next = prev;
            }
            else if( i == (bsPoints.Length-1) )
            {
                prev = bsPoints[bsPoints.Length-1] - bsPoints[bsPoints.Length-2];
                next = prev;
            }
            else
            {
                prev = bsPoints[i] - bsPoints[i-1];
                next = bsPoints[i+1] - bsPoints[i];
            }

            prev.Normalize();
            next.Normalize();

            bsTangents[i] = (prev + next) * 0.5f;
            bsTangents[i].Normalize();
        }
    }

    Vector3 getPN(float u)
    {
        if( u == 1f ) return basisPoints[n];

        Vector3 result = Vector3.zero;

        if(useSphericalInterpolation)
        {
            for (int i = 0; i < n; i++)
            {
                result += basisPoints[i] * getNVector(i, p, u);
            }
        }
        else
        {
            float val;
            for (int i = 0; i <= n; i++)
            {
                val = getN(i, p, u);
                result += basisPoints[i] * val;
            }
        }

        

        return result;
    }

    float getNVector(int i, int j, float u)
    {
        Vector3 result = Vector3.zero;

        if (j == 0)
        {
            if (basisWeights[i] == basisWeights[i + 1]) return 0f;
            else if (u >= basisWeights[i] && u < basisWeights[i + 1]) return 1f;
            else return 0f;
        }

        float v1, v2, t, sn;
        Vector3 tv1 = basisPoints[i]; tv1.Normalize();
        Vector3 tv2 = basisPoints[i+1]; tv2.Normalize();
        float angle = Mathf.Acos(Vector3.Dot(tv1, tv2));

        if (basisWeights[i + j] == basisWeights[i])
        {
            v1 = 1.0f;
            v2 = 0.0f;
        }
        else
        {
            t = (u - basisWeights[i]) / (basisWeights[i + j] - basisWeights[i]);
            sn = Mathf.Sin(angle);

            v1 = Mathf.Sin((1f - t) * angle) / sn;
            v2 = Mathf.Sin(t * angle) / sn;
        }

        return v1 * getNVector(i, j - 1, u) + v2 * getNVector(i+1, j - 1, u);
    }


    float getN(int i, int j, float u)
    {
        if (j == 0)
        {
            if (basisWeights[i] == basisWeights[i + 1]) return 0f;
            else if (u >= basisWeights[i] && u < basisWeights[i + 1]) return 1f;
            else return 0f;
        }

        float v1, v2;

        if (basisWeights[i + j] == basisWeights[i])
        {
            v1 = 0f;
        }
        else
        {
            v1 = (u - basisWeights[i]) / (basisWeights[i + j] - basisWeights[i]) * getN(i, j - 1, u);
        }

        if (basisWeights[i + j + 1] == basisWeights[i + 1])
        {
            v2 = 0f;
        }
        else
        {
            v2 = (basisWeights[i + j + 1] - u) / (basisWeights[i + j + 1] - basisWeights[i + 1]) * getN(i + 1, j - 1, u);
        }

        return v1 + v2;
    }


    public void populateMesh()
    {

        Vector3[] basePoints = bsPoints;
        Vector3[] baseTangents = bsTangents;

        Vector3 currVertex;
        Vector3 tgtDirVector;
        Vector3 currRight = Vector3.one;
        Vector3 currForward;
        Vector3 currUp;

        Vector3[] meshPoints = new Vector3[numMinorDivisions * numMajorDivisions];
        Vector3[] meshNormals = new Vector3[numMinorDivisions * numMajorDivisions];

        int currIdx = 0;
        Vector3 prevUp = Vector3.zero;
        Vector3 prevRight = Vector3.zero;

        for (int i = 0; i < basePoints.Length; i++)
        {
            baseTangents[i].Normalize();

            currForward = baseTangents[i];
            
            if( i == 0 )
            {
                tgtDirVector = currForward;
                for (int j = 1; j < basePoints.Length; j++)
                {
                    tgtDirVector = basePoints[j] - basePoints[i];
                    tgtDirVector.Normalize();
                    float dot = Vector3.Dot(currForward, tgtDirVector);

                    if (dot <= 0.95f)
                    {
                        currRight = Vector3.Cross(tgtDirVector, currForward);
                        break;
                    }
                }

                if (Mathf.Abs(Vector3.Dot(currForward, tgtDirVector)) > 0.95f)
                {
                    currRight.x = -currForward.y;
                    currRight.y = currForward.x;
                    currRight.z = currForward.z;

                    if (Mathf.Abs(Vector3.Dot(currForward, currRight)) > 0.99f)
                    {
                        currRight.x = -currForward.z;
                        currRight.y = currForward.y;
                        currRight.z = currForward.x;
                    }
                }
                

            }


            
            else currRight = Vector3.Cross(currForward, prevUp);

            currRight.Normalize();

            //if( i > 0 && Vector3.Dot(prevRight, currRight) < 0.0f ) currRight = -currRight;
           
          
          	prevRight = currRight;

            currUp = Vector3.Cross(currRight, currForward);
            currUp.Normalize();

            prevUp = currUp;

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
        meshFilter.mesh = mesh;
        mesh.vertices = meshPoints;
        mesh.normals = meshNormals;
        mesh.triangles = faceList;


    }

   
   
}
