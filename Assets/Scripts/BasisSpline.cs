using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSpline : MonoBehaviour {

    Vector3[] basisPoints;
    float[] basisWeights;
    public int n = 0; // number of control points (zero-based)
    public int m = 0; // number of weights
    public int p = 0;
    public int k = 0;

    Vector3[] bsPoints;

    public LineRenderer lineRend;

    // Use this for initialization
    void Start () {

        n = 3;
        k = 4;

        basisPoints = new Vector3[n+1];

        basisPoints[0] = new Vector3(0f, 0f, 0f);
        basisPoints[1] = new Vector3(0f, 1f, 0f);
        basisPoints[2] = new Vector3(1f, 1f, 0f);
        basisPoints[3] = new Vector3(1f, 0f, 0f);

        int numOutWts = k/2;
        int numInnerWts = n;
        m = numOutWts * 2 + numInnerWts;
        basisWeights = new float[m+1];
        int i = 0;
        for( i = 0; i < numOutWts; i++ )
        {
            basisWeights[i] = 0f;
            basisWeights[basisWeights.Length - 1 - i] = 1f;
        }

        float inc = 1f / (float)(numInnerWts+1);
        for( int j = 1; j <= numInnerWts; j++ )
        {
            basisWeights[i+j] = inc*j;
        }

        p = Mathf.Max(m - n -1, 0);


        doStuff();
        //populateLine();
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
        float[] uVals = new float[11];
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
        Vector3 result = Vector3.zero;
        float val;
        for(int i = 0; i < n; i++ )
        {
            val = getN(i, p, u);
            Debug.Log("Val: " + val);
            result += basisPoints[i] * val;
        }

        return result;
    }

    float getN(int i, int j, float u)
    {
        if( j == 0 )
        {
            if (basisWeights[i] == basisWeights[i + 1]) return 0f;
            else if (i >= basisWeights[i] && i < basisWeights[i + 1]) return 1f;
            else return 0f;
        }

        float v1, v2;

        if (basisWeights[i + j] == basisWeights[i]) v1 = 1f;
        else v1 = (u - basisWeights[i]) / (basisWeights[i + j] - basisWeights[i]);

        if (basisWeights[i + j + 1] == basisWeights[i + 1]) v2 = 1f;
        else v2 = (basisWeights[i + j + 1] - u) / (basisWeights[i + j + 1] - basisWeights[i + 1]);

        return v1 * getN(i, j - 1, u) + v2 * getN(i + 1, j - 1, u);
    }

   
   
}
