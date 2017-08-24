using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScaleObject : MonoBehaviour {

    ViveController grabObject1 = null;
    ViveController grabObject2 = null;
    ViveController activeGrabObject = null;

    bool activeScale = false;
    bool activeMove = false;

    Vector3 initialScale = Vector3.zero;
    float initialDist = 0;
    Quaternion initialRotation;
    Vector3 inititalOffset = Vector3.zero;

    public bool allowMove = true;
    public bool allowScale = true;


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        updateScale();
        updateMove();
    }


    public void grabSphereWithObject(GameObject obj)
    {
        ViveController controller = obj.GetComponent<ViveController>();

        if (grabObject1 == controller || grabObject2 == controller) return;

        if (grabObject1 == null)
        {
            grabObject1 = controller;
            activeGrabObject = grabObject1;
        }
        else if (grabObject2 == null)
        {
            grabObject2 = controller;
            activeGrabObject = grabObject2;
        }

        if (grabObject1 != null && grabObject2 != null)
        {

            initialScale = gameObject.transform.localScale;
            Vector3 tVec = grabObject1.transform.position - grabObject2.transform.position;
            initialDist = tVec.magnitude;
            if( allowScale ) activeScale = true;
            activeMove = false;
        }
        else
        {
            //initialRotation = Quaternion.Inverse(gameObject.transform.rotation) * activeGrabObject.currRotation;
            initialRotation = Quaternion.Inverse(activeGrabObject.transform.rotation) * gameObject.transform.rotation;
            Vector3 tmpVec = gameObject.transform.position - activeGrabObject.transform.position;

            Transform t = activeGrabObject.transform;

            inititalOffset.Set(
                Vector3.Dot(t.up, tmpVec),
                Vector3.Dot(t.right, tmpVec),
                Vector3.Dot(t.forward, tmpVec)
                );
            if (allowMove) activeMove = true;
            activeScale = false;
        }

    }

    public void releaseSphereWithObject(GameObject obj)
    {
        ViveController controller = obj.GetComponent<ViveController>();

        if (controller == null) return;

        if (grabObject1 == controller)
        {
            grabObject1 = null;
            activeScale = false;

            if (grabObject2 == null) activeMove = false;
        }

        else if (grabObject2 == controller)
        {
            grabObject2 = null;
            activeScale = false;
            if (grabObject1 == null) activeMove = false;

        }

    }

    void updateMove()
    {
        if (activeMove)
        {
            gameObject.transform.rotation = activeGrabObject.transform.rotation * initialRotation;

            gameObject.transform.position = activeGrabObject.deviceRay.origin +
                inititalOffset.x * activeGrabObject.transform.up +
                inititalOffset.y * activeGrabObject.transform.right +
                inititalOffset.z * activeGrabObject.transform.forward;
        }
    }

    void updateScale()
    {
        if (activeScale)
        {
            Vector3 tVec = grabObject1.transform.position - grabObject2.transform.position;
            float scale = tVec.magnitude / initialDist;
            gameObject.transform.localScale = initialScale * scale;
        }
    }
}
