using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierLine : BezierBar
{

    public LineRenderer lineRend;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public new void init(Vector3[] bPts, Color c0, Color c1, Transform transform = null)
    {
        color0 = c0;
        color1 = c1;

        if (useSphericalInterpolation)
        {
            Vector3[] tmpPoints = Utils.getBezierPoints(bPts, numMajorDivisions);
            basePoints = new Vector3[tmpPoints.Length];

            if (transform != null)
            {
                for (int i = 0; i < basePoints.Length; i++)
                {
                    basePoints[i] = transform.TransformPoint(basePoints[i]);
                }
            }
        }
        else
        {
            basePoints = Utils.getBezierPoints(bPts, numMajorDivisions);

            if (transform != null)
            {
                for (int i = 0; i < basePoints.Length; i++)
                {
                    basePoints[i] = transform.TransformPoint(basePoints[i]);
                }
            }

        }

        populateLine();
    }

    void populateLine()
    {
        lineRend.positionCount = basePoints.Length;
        lineRend.SetPositions(basePoints);
        lineRend.startColor = color0;
        lineRend.endColor = color1;
        lineRend.startWidth = radius;
        lineRend.endWidth = radius;
    }
}
