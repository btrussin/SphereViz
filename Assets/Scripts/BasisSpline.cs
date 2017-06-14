using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSpline : MonoBehaviour {

    public Vector3[] basisPoints;
    float[] basisWeights;
    int n = 0; // number of control points (zero-based)
    int m = 0; // number of weights
    int p = 0;

    Vector3[] bsPoints;

    public LineRenderer lineRend;

    // Use this for initialization
    void Start () {

        n = basisPoints.Length - 1;

        int numOutWts = 4;
        int numInnerWts = n-3;
        m = numOutWts * 2 + numInnerWts - 1;
        basisWeights = new float[m+1];
        int i = 0;
        for( i = 0; i < numOutWts; i++ )
        {
            basisWeights[i] = 0f;
            basisWeights[m - i] = 1f;
        }

        float inc = 1f / (float)(numInnerWts+1);
        for( int j = 0; j < numInnerWts; j++ )
        {
            basisWeights[i+j] = inc*(j+1);
        }


        p = m - n - 1;

        Debug.Log("M: " + m);
        Debug.Log("N: " + n);
        Debug.Log("P: " + p);


        doStuff();
        populateLine();
    }

    void populateLine()
    {
        lineRend.positionCount = bsPoints.Length;
        lineRend.SetPositions(bsPoints);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void doStuff()
    {
        float[] uVals = new float[100];
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

        for (int i = 0; i < uVals.Length; i++)
        {
            Debug.Log(i + ": " + bsPoints[i]);
        }

    }

    Vector3 getPN(float u)
    {
        if( u == 1f ) return basisPoints[n];

        Vector3 result = Vector3.zero;
        float val;
        for(int i = 0; i <= n; i++ )
        {
            val = getN(i, p, u);
            result += basisPoints[i] * val;
        }

        return result;
    }

    float getN(int i, int j, float u)
    {
        if( j == 0 )
        {
            if (basisWeights[i] == basisWeights[i + 1]) return 0f;
            else if (u >= basisWeights[i] && u < basisWeights[i + 1]) return 1f;
            else return 0f;
        }
    
        float v1, v2;

        if( basisWeights[i + j] == basisWeights[i] )
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
