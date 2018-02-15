using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HighlightManager : MonoBehaviour {

    public Renderer noneRend;
    public Renderer oneHopRend;
    public Renderer farRend;
    public Renderer nearRend;

    public Material radioEmptyMaterial;
    public Material radioFullMaterial;

    //bool hlSelectedNear = true;
    //bool hlSelectedFar = true;

    highlightState highlightType = highlightState.ONE_HOP;

    public DataObjectManager dataManager;

    // Use this for initialization
    void Start () {
        updateAllStates();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void buttonAction(GameObject obj)
    {
        if (obj.name.Equals("HL_One")) highlightType = highlightState.ONE_HOP;
        else if (obj.name.Equals("HL_Near")) highlightType = highlightState.NEAR;
        else if (obj.name.Equals("HL_Far")) highlightType = highlightState.FAR;
        else if (obj.name.Equals("HL_None")) highlightType = highlightState.NONE;

        updateAllStates();
    }

    void updateAllStates()
    {

        noneRend.material = radioEmptyMaterial;
        oneHopRend.material = radioEmptyMaterial;
        farRend.material = radioEmptyMaterial;
        nearRend.material = radioEmptyMaterial;

        switch (highlightType)
        {
            case highlightState.ONE_HOP:
                oneHopRend.material = radioFullMaterial;
                break;
            case highlightState.NEAR:
                nearRend.material = radioFullMaterial;
                break;
            case highlightState.FAR:
                farRend.material = radioFullMaterial;
                break;
            case highlightState.NONE:
                noneRend.material = radioFullMaterial;
                break;
        }

        dataManager.updateHighlightState(highlightType);
    }
}
