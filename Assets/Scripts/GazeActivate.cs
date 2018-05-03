using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeActivate : MonoBehaviour {

    static List<PopupTextFade> textList = new List<PopupTextFade>();
    static Dictionary<string, PopupTextFade> textMap = new Dictionary<string, PopupTextFade>();

    public float gazeFactor = 0.99f;

    public int maxLabelsToShow = 30;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        Vector3 forward = gameObject.transform.forward;
        Vector3 tmp;

        int count = 0;
        foreach (PopupTextFade text in textList)
        {
            tmp = text.parentObject.transform.position - gameObject.transform.position;
            tmp.Normalize();
            if (Vector3.Dot(forward, tmp) > gazeFactor)
            {
                text.inCameraView();
                if (count++ > maxLabelsToShow) break;
            }
        }

        foreach (PopupTextFade text in textMap.Values)
        {
            tmp = text.parentObject.transform.position - gameObject.transform.position;
            tmp.Normalize();
            if (Vector3.Dot(forward, tmp) > gazeFactor)
            {
                text.inCameraView();
                if (count++ > maxLabelsToShow) break;
            }
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

    /*
    public void removeAllTextObjects()
    {
        textList.Clear();
        textMap.Clear();
    }
    */
}
