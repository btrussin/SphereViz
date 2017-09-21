using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSpline : BaseCurve
{
    
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

    }

    public new void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {
    	color0 = c0;
    	color1 = c1;

        basisPoints = new Vector3[bPts.Length];
        for( int i = 0; i < bPts.Length; i++ )
        {
            basisPoints[i] = bPts[i];
        }

        setup(transform);
        populateCurveBarMesh(bsPoints, bsTangents);
    }

    protected void setup(Transform transform = null)
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

        calcBSplinePoints();

        if( useSphericalInterpolation )
        {
    		Vector3 tmpVec;
    		Quaternion rotation;
    		for( i = 0; i < bsPoints.Length; i++ )
    		{
				tmpVec = new Vector3(bsPoints[i].z, 0.0f, 0.0f);
        		rotation = Quaternion.Euler(0.0f, bsPoints[i].x, bsPoints[i].y);
        		bsPoints[i] = rotation * tmpVec;
    		}

            if (transform != null)
            {
                for (i = 0; i < bsPoints.Length; i++)
                {
                    bsPoints[i] = transform.TransformPoint(bsPoints[i]);
                }
            }

            bsTangents = new Vector3[bsPoints.Length];

    		for( i = 1; i < bsPoints.Length; i++ )
    		{
    			tmpVec = bsPoints[i] - bsPoints[i-1];
    			tmpVec.Normalize();
    			bsTangents[i] = tmpVec;
    		}

    		bsTangents[0] = bsTangents[1];
        }
        else
        {
            if (transform != null)
            {
                for (i = 0; i < bsPoints.Length; i++)
                {
                    bsPoints[i] = transform.TransformPoint(bsPoints[i]);
                }
            }

            calcBSplineTangents();
        }
        
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

        float val;
        for (int i = 0; i <= n; i++)
        {
            val = getN(i, p, u);
            result += basisPoints[i] * val;
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


    

   
   
}
