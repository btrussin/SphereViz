using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class ScreenshotCamera : MonoBehaviour {

    Camera mainCamera;
    public GameObject renderTexObject;
    public TextMesh statusText;
    RenderTexture renderTexture;

    string[] keywords;
    KeywordRecognizer recognizer;

    int currStatusCount = 0;
    public int maxStatusFrameCnt = 100;

    // Use this for initialization
    void Start () {
        mainCamera = Camera.main;

        MeshRenderer mRend = renderTexObject.GetComponent<MeshRenderer>();
        renderTexture = mRend.material.mainTexture as RenderTexture;

        keywords = new string[1];
        keywords[0] = "Screen shot";

        recognizer = new KeywordRecognizer(keywords);
        recognizer.OnPhraseRecognized += OnPhraseRecognized;
        recognizer.Start();

        statusText.text = "";

        currStatusCount = maxStatusFrameCnt;

    }
	
	// Update is called once per frame
	void Update () {
        //Debug.Log("update");
        mainCamera = Camera.main;
        /*
        gameObject.transform.forward = mainCamera.transform.forward;
        gameObject.transform.right = mainCamera.transform.right;
        gameObject.transform.up = mainCamera.transform.up;
        */
        gameObject.transform.rotation = mainCamera.transform.rotation;

        gameObject.transform.position = mainCamera.transform.position;

        if( Input.GetKeyDown(KeyCode.Space) )
        {
            captureScreen();
        }

        if(currStatusCount < maxStatusFrameCnt)
        {

            statusText.gameObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward - mainCamera.transform.up * 0.25f;

            currStatusCount++;
        }
        else if( currStatusCount == maxStatusFrameCnt )
        {
            statusText.gameObject.SetActive(false);
            currStatusCount++;
        }

    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (args.text.Equals(keywords[0]))
        {
            captureScreen();
        }
    }

    public void captureScreen()
    {
        Debug.Log("Writing to file");
        int width = renderTexture.width;
        int height = renderTexture.height;

        Texture2D tex2D = get2DTexture(renderTexture);

        byte[] bytes = tex2D.EncodeToJPG();


        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/../Screenshots");
        FileInfo[] info = dir.GetFiles("hi-def-screenshot*.jpg");

        int max = 0;

        foreach (FileInfo fi in info)
        {
            string[] comps = fi.Name.Split('.');
            string[] subComps = comps[0].Split('-');
            try
            {
                int t = int.Parse(subComps[subComps.Length - 1]);
                if (t > max) max = t;
            }
            catch (System.FormatException fe) { }
        }

        max++;

        string fileName = "hi-def-screenshot-" + max + ".jpg";

        File.WriteAllBytes(Application.dataPath + "/../Screenshots/" + fileName, bytes);

        statusText.gameObject.SetActive(true);
        statusText.text = "Saved " + fileName;
        currStatusCount = 0;
    }

    Texture2D get2DTexture(RenderTexture rt)
    {
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the supplied RenderTexture as the active one
        RenderTexture.active = rt;

        // Create a new Texture2D and read the RenderTexture image into it
        Texture2D tex = new Texture2D(rt.width, rt.height);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

        // Restorie previously active render texture
        RenderTexture.active = currentActiveRT;
        return tex;
    }



}
