using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HLButtonManger : MonoBehaviour
{

    public HighlightManager highlightManager;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void takeAction()
    {
        highlightManager.buttonAction(gameObject);
    }
}
