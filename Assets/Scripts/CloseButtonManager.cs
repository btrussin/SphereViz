using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseButtonManager : MonoBehaviour
{
    public GameObject menuObject;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void takeAction()
    {
        menuObject.SetActive(false);
    }
}
