using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterGroupEdge : BasisSpline
{

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void setNode(GameObject node)
    {
        if (nodeA == null)
        {
            nodeA = node;
            nodeManagerA = nodeA.GetComponent<NodeManager>();
        }
        else if (nodeB == null)
        {
            nodeB = node;
            nodeManagerB = nodeB.GetComponent<NodeManager>();
        }
    }

    public override void updateHighlightState()
    {
        updateHighlightState(currHighlightState);
    }

    public override void updateHighlightState(highlightState state)
    {
        if (objectMaterial == null)
        {
            objectMaterial = gameObject.GetComponent<Renderer>().material;
        }
        switch (state)
        {
            case highlightState.NONE:
                objectMaterial.SetFloat("_Highlight", 1f);
                break;
            case highlightState.FAR:
                objectMaterial.SetFloat("_Highlight", 1f);
                nodeManagerA.adjustNodeColor(1f);
                nodeManagerB.adjustNodeColor(1f);
                break;
            case highlightState.NEAR:
                objectMaterial.SetFloat("_Highlight", 0.1f);
                break;
            case highlightState.ONE_HOP:
                if (nodeManagerA.isSelected || nodeManagerB.isSelected)
                {
                    objectMaterial.SetFloat("_Highlight", 1f);
                    nodeManagerA.adjustNodeColor(1f);
                    nodeManagerB.adjustNodeColor(1f);
                }
                else
                {
                    objectMaterial.SetFloat("_Highlight", 0.1f);
                }

                break;
        }

    }

    public override void updateRadiusBasedOnHighlightState()
    {
        updateRadiusBasedOnHighlightState(currHighlightState);
    }

    public override void updateRadiusBasedOnHighlightState(highlightState state)
    {
        switch (state)
        {
            case highlightState.NONE:
            case highlightState.FAR:
                restoreTheEdgeToOriginalThickness();
                break;
            case highlightState.NEAR:
                thinTheEdge();
                break;
            case highlightState.ONE_HOP:
                if (nodeManagerA.isSelected || nodeManagerB.isSelected) restoreTheEdgeToOriginalThickness();
                else thinTheEdge();

                break;
        }

    }

}
