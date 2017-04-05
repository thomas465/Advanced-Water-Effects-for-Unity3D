using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {

    public static PlayerScript singleton;
    void Awake()
    {
        singleton = this;
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
