using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSpline : BaseCurve
{
    
    protected float[] basisWeights;
    protected int n = 0; // number of control points (zero-based)
    protected int m = 0; // number of weights
    protected int p = 0;


    // Use this for initialization
    void Start () {

    }

    public new void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {
    	color0 = c0;
    	color1 = c1;

        init(bPts, transform);
    }

    public new void init(Vector3[] ctrlPts, Transform transform = null)
    {
        controlPoints = new Vector3[ctrlPts.Length];
        for (int i = 0; i < ctrlPts.Length; i++)
        {
            controlPoints[i] = ctrlPts[i];
        }

        setup(transform);
        populateCurveBarMesh(basePoints, baseTangents);
    }

    protected void setup(Transform transform = null)
    {
        n = controlPoints.Length - 1;

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
    		for( i = 0; i < basePoints.Length; i++ )
    		{
				tmpVec = new Vector3(basePoints[i].z, 0.0f, 0.0f);
        		rotation = Quaternion.Euler(0.0f, basePoints[i].x, basePoints[i].y);
        		basePoints[i] = rotation * tmpVec;
    		}

            if (transform != null)
            {
                for (i = 0; i < basePoints.Length; i++)
                {
                    basePoints[i] = transform.TransformPoint(basePoints[i]);
                }
            }

            baseTangents = new Vector3[basePoints.Length];

    		for( i = 1; i < basePoints.Length; i++ )
    		{
    			tmpVec = basePoints[i] - basePoints[i-1];
    			tmpVec.Normalize();
    			baseTangents[i] = tmpVec;
    		}

    		baseTangents[0] = baseTangents[1];
        }
        else
        {
            if (transform != null)
            {
                for (i = 0; i < basePoints.Length; i++)
                {
                    basePoints[i] = transform.TransformPoint(basePoints[i]);
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

        basePoints = new Vector3[uVals.Length];

        for( int i = 0; i < uVals.Length; i++ )
        {
            basePoints[i] = getPN(uVals[i]);
        }
    }

    void calcBSplineTangents()
    {
        baseTangents = new Vector3[basePoints.Length];

        Vector3 prev, next;

        for( int i = 0; i < basePoints.Length; i++ )
        {
            if( i == 0 )
            {
                prev = basePoints[1] - basePoints[0];
                next = prev;
            }
            else if( i == (basePoints.Length-1) )
            {
                prev = basePoints[basePoints.Length-1] - basePoints[basePoints.Length-2];
                next = prev;
            }
            else
            {
                prev = basePoints[i] - basePoints[i-1];
                next = basePoints[i+1] - basePoints[i];
            }

            prev.Normalize();
            next.Normalize();

            baseTangents[i] = (prev + next) * 0.5f;
            baseTangents[i].Normalize();
        }
    }

    Vector3 getPN(float u)
    {
        if( u == 1f ) return controlPoints[n];

        Vector3 result = Vector3.zero;

        float val;
        for (int i = 0; i <= n; i++)
        {
            val = getN(i, p, u);
            result += controlPoints[i] * val;
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
        Vector3 tv1 = controlPoints[i]; tv1.Normalize();
        Vector3 tv2 = controlPoints[i+1]; tv2.Normalize();
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
