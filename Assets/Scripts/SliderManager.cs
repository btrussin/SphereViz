using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderManager : MonoBehaviour {

    public GameObject leftEdgeLine;
    public GameObject rightEdgeLine;
    public GameObject sliderCircle;

    public TextMesh mainLabel;
    public TextMesh leftLabel;
    public TextMesh rightLabel;
    public TextMesh valueLabel;

    public string mainLabelText = "";
    public string leftLabelText = "";
    public string rightLabelText = "";

    public float leftValue = 0f;
    public float rightValue = 1f;

    float currentSliderValue = 0f;
    float currentRelativePosition = 0f;

    public float getValue()
    {
        return currentSliderValue;
    }

    // Use this for initialization
    void Start () {
        currentSliderValue = (rightValue + leftValue) * 0.5f;

        mainLabel.text = mainLabelText;
        leftLabel.text = leftLabelText;
        rightLabel.text = rightLabelText;

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void tryMoveValue(Vector3 pos)
    {
       
        // project that point onto the world positions of the slider ends
        Vector3 v1 = rightEdgeLine.transform.position - leftEdgeLine.transform.position;
        Vector3 v2 = pos - leftEdgeLine.transform.position;

        // 'd' is the vector-projection amount of v2 onto v1 [0,1]
        float d = Vector3.Dot(v1, v2) / Vector3.Dot(v1, v1);
        currentRelativePosition = Mathf.Clamp(d, 0.0f, 1.0f);
        currentSliderValue = leftValue + (rightValue - leftValue) * currentRelativePosition;


        // 'd' is also the correct linear combination of the left and right slider edges
        // left * d + right * ( 1 - d )
        placeSliderPoint();
    }

    public void suggestValue(float t)
    {
        if( leftValue > rightValue ) currentSliderValue = Mathf.Clamp(t, rightValue, leftValue);
        else currentSliderValue = Mathf.Clamp(t, leftValue, rightValue);
        currentRelativePosition = (currentSliderValue - leftValue) / (rightValue - leftValue);
        placeSliderPoint();
    }

    protected void placeSliderPoint()
    {
        Vector3 tVec = (rightEdgeLine.transform.localPosition - leftEdgeLine.transform.localPosition) * currentRelativePosition;
        sliderCircle.transform.localPosition = leftEdgeLine.transform.localPosition + tVec;

        tVec = valueLabel.gameObject.transform.localPosition;
        tVec.x = sliderCircle.transform.localPosition.x;
        valueLabel.gameObject.transform.localPosition = tVec;

        valueLabel.text = currentSliderValue.ToString("0.00");
    }

}
