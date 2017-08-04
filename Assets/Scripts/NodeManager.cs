using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeManager : MonoBehaviour {

    public string nodeName = "";
    Color origColor = Color.white;
    Material material = null;
    MeshRenderer meshRend = null;
    public NodeInfo nodeInfo = null;

    List<string> subNodeNames = new List<string>();

    int numCollisions = 0;

	// Use this for initialization
	void Start () {
        meshRend = gameObject.GetComponent<MeshRenderer>();
        if( meshRend != null )
        {
            material = meshRend.material;

            if (material != null)
            {
                origColor = material.color;
            }
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void setSubNodeNames(List<string> list)
    {
        subNodeNames.Clear();
        subNodeNames.AddRange(list);
    }

    public List<string> getSubNodeNames()
    {
        return subNodeNames;
    }

    
    void OnCollisionEnter(Collision collision)
    {
        numCollisions++;
        if (material == null) material = gameObject.GetComponent<Material>();
        if (material != null) material.color = Color.white;
    }

    void OnCollisionExit(Collision collision)
    {
        numCollisions--;
        if (numCollisions <= 0)
        {
            numCollisions = 0;
            if (material != null) material.color = origColor;
        }
    }
    

}
