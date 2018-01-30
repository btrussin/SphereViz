using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSplineLine : BasisSpline
{

    public LineRenderer lineRend;

    // Use this for initialization
    void Start()
    {

    }

    public new void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {
        color0 = c0;
        color1 = c1;

        controlPoints = new Vector3[bPts.Length];
        for (int i = 0; i < bPts.Length; i++)
        {
            controlPoints[i] = bPts[i];
        }

        setup(transform);
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
}
