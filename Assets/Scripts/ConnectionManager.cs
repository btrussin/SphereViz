﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour {

    public GameObject textPrefab;

    GameObject textObject;
    PopupTextFade popupText;

    bool recalcEdge = false;

    public Color colorA;
    public Color colorB;
    public bool innerConnStraightLine = false;
    public Transform projSphereTransform;
    public GameObject nodePointA;
    public GameObject nodePointB;
    public GameObject subPointA;
    public GameObject subPointB;
    public BezierBar bezBar;
    public BezierLine bezLine;

    static Quaternion noRotation = Quaternion.Euler(0f, 0f, 0f);

    public DataObjectManager dataManager;

    public GameObject mainCurve;
    public GameObject altLineCurve;

    bool restrictDrawingOfEdge = false;

    Vector3[] ctrlPts = new Vector3[4];

    public void displayText(Vector3 pt)
    {
        textObject.SetActive(true);
        textObject.transform.position = pt;

        popupText.inCameraView();
    }

    public void hideText()
    {
        textObject.SetActive(false);
    }

    public void setupText(string name)
    {
        textObject = (GameObject)Instantiate(textPrefab);
        textObject.name = "Conn" + name;
  
        TextMesh tMesh = textObject.GetComponent<TextMesh>();
        tMesh.text = name;

        popupText = textObject.GetComponent<PopupTextFade>();
        popupText.setup();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if( recalcEdge )
        {
            updateEdge();
            recalcEdge = false;
        }
	}

    public void recalculateEdge(bool restrictCurveRedraw)
    {
        recalcEdge = true;

        if( restrictDrawingOfEdge != restrictCurveRedraw )
        {
            restrictDrawingOfEdge = restrictCurveRedraw;

            if (restrictDrawingOfEdge)
            {
                mainCurve.SetActive(false);
                altLineCurve.SetActive(true);
            }
            else
            {
                mainCurve.SetActive(true);
                altLineCurve.SetActive(false);
            }
        }
    }

    void updateEdge()
    {
        ctrlPts[0] = subPointA.transform.position;
        ctrlPts[3] = subPointB.transform.position;
        
        Vector3 tmpVecA = projSphereTransform.position - ctrlPts[0];
        tmpVecA.Normalize();
        tmpVecA *= Vector3.Dot(tmpVecA, ctrlPts[0] - nodePointA.transform.position);

        Vector3 tmpVecB = projSphereTransform.position - ctrlPts[3];
        tmpVecB.Normalize();
        tmpVecB *= Vector3.Dot(tmpVecB, ctrlPts[3] - nodePointB.transform.position);


        if (innerConnStraightLine)
        {
            ctrlPts[1] = ctrlPts[0] + tmpVecA * 0.001f;
            ctrlPts[2] = ctrlPts[3] + tmpVecB * 0.001f;
        }
        else
        {
            ctrlPts[1] = ctrlPts[0] + tmpVecA;
            ctrlPts[2] = ctrlPts[3] + tmpVecB;
        }

        Vector3 pos = gameObject.transform.position;
        for( int i = 0; i < 4; i++ )
        {
           ctrlPts[i] = ctrlPts[i] - pos;
        }

        // get node scale
        float nScale = projSphereTransform.localScale.x;
        for (int i = 0; i < 4; i++)
        {
            ctrlPts[i] /= nScale;
        }

        if (restrictDrawingOfEdge)
        {
            bezLine.gameObject.transform.rotation = noRotation;
            bezLine.gameObject.transform.localScale = Vector3.one;

            bezLine.radius = 2f * dataManager.getCurrBarRadius() / nScale;
            bezLine.init(ctrlPts, colorA, colorB, null);
        }
        else
        {
            bezBar.gameObject.transform.rotation = noRotation;
            bezBar.gameObject.transform.localScale = Vector3.one;

            bezBar.radius = dataManager.getCurrBarRadius() / nScale;
            bezBar.init(ctrlPts, colorA, colorB, null);
        }

    }
}
