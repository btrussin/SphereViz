using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeselectButtonAnimation : MonoBehaviour {

    public TextMesh text;
    public MeshRenderer rend;

    public int maxCount = 30;
    int currCount = 0;

    bool activeStuff = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if( activeStuff )
        {
            currCount++;
            if (currCount >= maxCount)
            {
                activeStuff = false;
                setOrigColors();
            }
        }
	
        
	}

    public void setOrigColors()
    {
        text.color = Color.black;
        rend.material.color = Color.white;
    }

    public void setAltColors()
    {
        text.color = Color.white;
        rend.material.color = Color.black;

        activeStuff = true;
        currCount = 0;
    }


}
