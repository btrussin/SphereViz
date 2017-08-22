using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionManager : MonoBehaviour {

    public GameObject textPrefab;

    GameObject textObject;
    PopupTextFade popupText;

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
		
	}
}
