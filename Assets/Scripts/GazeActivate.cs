using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeActivate : MonoBehaviour {

    static List<PopupTextFade> textList = new List<PopupTextFade>();
    static Dictionary<string, PopupTextFade> textMap = new Dictionary<string, PopupTextFade>();

    public float gazeFactor = 0.99f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        Vector3 forward = gameObject.transform.forward;
        Vector3 tmp;
        foreach (PopupTextFade text in textList)
        {
            tmp = text.gameObject.transform.position - gameObject.transform.position;
            tmp.Normalize();
            if( Vector3.Dot(forward, tmp) > gazeFactor) text.inCameraView();

        }

        foreach (PopupTextFade text in textMap.Values)
        {
            tmp = text.gameObject.transform.position - gameObject.transform.position;
            tmp.Normalize();
            if (Vector3.Dot(forward, tmp) > gazeFactor) text.inCameraView();
        }

    }

    public void addTextObject(PopupTextFade scr)
    {
        textList.Add(scr);
    }

    public void addTextObject(string key, PopupTextFade scr)
    {
        if( !textMap.ContainsKey(key)) textMap.Add(key, scr);
    }

    public void removeTextObject(string key)
    {
        if (textMap.ContainsKey(key)) textMap.Remove(key);
    }
}
