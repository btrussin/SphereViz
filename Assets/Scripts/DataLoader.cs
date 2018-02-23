using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DataLoader : MonoBehaviour {
	
    // populated by the derived class
    public Dictionary<string, NodeInfo> nodeMap = new Dictionary<string, NodeInfo>();
    public List<EdgeInfo> edgeList = new List<EdgeInfo>();

    // Use this for initialization
    void Start () {}
	
	// Update is called once per frame
	void Update () {}

    abstract public void loadData();

}
