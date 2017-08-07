using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class MainCameraController : MonoBehaviour {

    private CVRSystem vrSystem;

    public GameObject steamVRObject;
    public GameObject steamVRCameraRig;
    public GameObject mainCamera;

    public bool forceUseScreenCamera;

    // Use this for initialization
    void Start () {
        
        if(forceUseScreenCamera )
        {

			Destroy (steamVRCameraRig);
			Destroy (steamVRObject);

            mainCamera.SetActive(true);
        }
        else
        {
			vrSystem = OpenVR.System;

			if( vrSystem == null )
			{
				steamVRCameraRig.SetActive(false);
				steamVRObject.SetActive(false);

				mainCamera.SetActive(true);
			}
			else
			{
				vrSystem = OpenVR.System;

				steamVRCameraRig.SetActive(true);
				steamVRObject.SetActive(true);

				mainCamera.SetActive(false);
			}

        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
