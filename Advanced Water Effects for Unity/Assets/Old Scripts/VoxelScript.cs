using UnityEngine;
using System.Collections;

public class VoxelScript : MonoBehaviour {

    public VoxelGrid.Voxel myInfo;
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnMouseOver()
    {
        //myInfo.active = true;
        //Debug.Break();
        
    }

    void OnTriggerEnter(Collider other)
    {
        MetaballScript v = other.GetComponent<MetaballScript>();

        if (v)
        {
            //Debug.Log("Active!");
            myInfo.active = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        MetaballScript v = other.GetComponent<MetaballScript>();

        if (v)
        {
            //myInfo.active = false;
        }
    }
}
