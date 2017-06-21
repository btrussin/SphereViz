using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasisSplineLine : BasisSpline
{

    public LineRenderer lineRend;

    // Use this for initialization
    void Start()
    {

        init();
    }

    public new void init()
    {
        if (basisPoints == null || basisPoints.Length < 4) return;

        setup();
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
