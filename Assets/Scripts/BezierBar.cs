using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierBar : BaseCurve
{

    public bool sphereCoords = false;

    // Use this for initialization
    void Start () {

	}

    public new void init(Vector3[] bPts, Color c0, Color c1)
    {
        color0 = c0;
        color1 = c1;

        Vector3[] basePoints;
        Vector3[] baseTangents;

        if (sphereCoords)
        {
            Vector3[] tmpPoints = Utils.getBezierPoints(bPts, numMajorDivisions);
            basePoints = new Vector3[tmpPoints.Length];
            baseTangents = new Vector3[tmpPoints.Length];
            Vector3 tmpVec;
            Quaternion rotation;
            for (int i = 0; i < basePoints.Length; i++)
            {
                tmpVec = new Vector3(tmpPoints[i].z, 0.0f, 0.0f);
                rotation = Quaternion.Euler(0.0f, tmpPoints[i].x, tmpPoints[i].y);
                basePoints[i] = rotation * tmpVec;
            }

            for (int i = 1; i < basePoints.Length; i++)
            {
                tmpVec = basePoints[i] - basePoints[i - 1];
                tmpVec.Normalize();
                baseTangents[i] = tmpVec;
            }

            baseTangents[0] = baseTangents[1];
        }
        else
        {
            basePoints = Utils.getBezierPoints(bPts, numMajorDivisions);
            baseTangents = Utils.getBezierPointTangents(bPts, numMajorDivisions);
        }

        populateCurveBarMesh(basePoints, baseTangents);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
