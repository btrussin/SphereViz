using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupTextFade : MonoBehaviour {

    public float secondsToFade = 2f;
    float secondsToFade_inv;
    float currSeconds = -1f;

    public Material textMaterial;

    Color origColor;

	// Use this for initialization
	void Start () {
        textMaterial = gameObject.GetComponent<MeshRenderer>().material;
        origColor = textMaterial.color;
        secondsToFade_inv = 1f / secondsToFade;
    }
	
	// Update is called once per frame
	void Update () {

        if (!gameObject.activeInHierarchy) return;

        gameObject.transform.forward = Camera.main.transform.forward;

        currSeconds -= Time.deltaTime;

        if (currSeconds < 0.0f) gameObject.SetActive(false);
        else textMaterial.color = origColor * currSeconds * secondsToFade_inv;
        

    }
    public void inCameraView()
    {
        currSeconds = secondsToFade;
        gameObject.SetActive(true);
        textMaterial.color = origColor;
    }


}
