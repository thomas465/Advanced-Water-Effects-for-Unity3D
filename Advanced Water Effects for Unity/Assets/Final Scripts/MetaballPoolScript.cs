using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MetaballPoolScript : MonoBehaviour {

    public GameObject metaballPrefab;
    //public MetaballManager myManager;

    public int poolSize = 10;
    [HideInInspector]
    public List<MetaballScript> myMetaballs;

	// Use this for initialization
	void Awake () {

       // if (!myManager)
            //myManager = GetComponent<MetaballManager>();

        myMetaballs = new List<MetaballScript>();
        
        for(int i=0; i<poolSize; i++)
        {
            GameObject nM = GameObject.Instantiate(metaballPrefab);
            nM.SetActive(false);
            //nM.hideFlags = HideFlags.HideInHierarchy;
            myMetaballs.Add(nM.GetComponent<MetaballScript>());
            //myMetaballs[myMetaballs.Count - 1].myManager = myManager;
        }

        
	}

    public void FireMetaball(MetaballManager manager, Vector3 origin, Vector3 direction, float speed = 3, float metaballRadius = 1.5f, float metaballLife = 10)
    {
        MetaballScript newMetaball = FindValidMetaball();

        if (newMetaball)
        {
            float originalMetaballRadius = metaballRadius;

            //Makes it smaller here so that numbers in the inspector are more reasonable
            metaballRadius *= 0.0001f;

            direction = direction.normalized;
            
            newMetaball.myInfo.radius = metaballRadius;
            newMetaball.transform.position = origin;
            newMetaball.myInfo.life = metaballLife;

            if(manager)
                newMetaball.transform.localScale = Vector3.one * newMetaball.myInfo.radius * (manager.GetDesiredMetaballSize());
            else
                newMetaball.transform.localScale = Vector3.one * originalMetaballRadius;

            newMetaball.myManager = manager;
            newMetaball.Fire(direction * speed);
        }
        else
        {
            //Debug.Log("No ball");
        }
    }

    MetaballScript FindValidMetaball()
    {
        MetaballScript result = null;

        for(int i=0; i<myMetaballs.Count; i++)
        {
            if(!myMetaballs[i].gameObject.activeInHierarchy)
            {
                return myMetaballs[i];
            }
        }

        return result;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
