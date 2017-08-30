using System.Collections;
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

    public void recalculateEdge()
    {
        recalcEdge = true;
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
            //ctrlPts[1] = ctrlPts[0] * 0.67f + ctrlPts[3] * 0.33f;
            //ctrlPts[2] = ctrlPts[3] * 0.67f + ctrlPts[0] * 0.33f;

            ctrlPts[1] = ctrlPts[0] + tmpVecA * 0.001f;
            ctrlPts[2] = ctrlPts[3] + tmpVecB * 0.001f;
        }
        else
        {
            ctrlPts[1] = ctrlPts[0] + tmpVecA;
            ctrlPts[2] = ctrlPts[3] + tmpVecB;
        }

        bezBar.init(ctrlPts, colorA, colorB);
    }
}
