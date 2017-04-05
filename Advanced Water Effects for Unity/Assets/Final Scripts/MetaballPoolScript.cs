using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MetaballPoolScript : MonoBehaviour {

    public GameObject metaballPrefab;

    public int poolSize = 100;

    [HideInInspector]
    public List<MetaballScript> myMetaballs;

    // Use this for initialization
    void Awake()
    {
        myMetaballs = new List<MetaballScript>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject nM = GameObject.Instantiate(metaballPrefab);
            nM.SetActive(false);

            //This hides metaballs from the Transform hierarchy in Unity
            nM.hideFlags = HideFlags.HideInHierarchy;

            myMetaballs.Add(nM.GetComponent<MetaballScript>());
            myMetaballs[i].myMetaballPool = this;
        }
    }

    /// <summary>
    /// Creates a metaball at the given position, assigns it to the given MetaballManager, gives the metaball info such as radius and then fires it.
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    /// <param name="metaballRadius"></param>
    /// <param name="metaballLife"></param>
    public void FireMetaball(MetaballManager manager, Vector3 origin, Vector3 direction, float speed = 3, float metaballRadius = 1.5f, float metaballLife = 10)
    {
        MetaballScript newMetaball = FindValidMetaball();

        if (newMetaball)
        {
            float originalMetaballRadius = metaballRadius;

            //Makes it smaller here so that numbers written in the inspector can be larger and easier to work with
            metaballRadius *= 0.0001f;

            direction = direction.normalized;
            
            newMetaball.myInfo.radius = metaballRadius;
            newMetaball.transform.position = origin;
            newMetaball.myInfo.life = metaballLife;

            if (manager)
            {
                newMetaball.transform.localScale = Vector3.one * originalMetaballRadius * (manager.GetDesiredMetaballSize());
            }
            else
            {
                newMetaball.transform.localScale = Vector3.one * originalMetaballRadius;
            }

            newMetaball.myManager = manager;
            newMetaball.Fire(direction * speed);
        }
        else
        {
            //Debug.Log("No ball");
        }
    }

    /// <summary>
    /// Finds an inactive metaball and returns it. Will return null if there are no available metaballs.
    /// </summary>
    /// <returns></returns>
    MetaballScript FindValidMetaball()
    {
        MetaballScript result = null;

        for(int i=0; i<myMetaballs.Count; i++)
        {
            if (myMetaballs[i])
            {
                if (!myMetaballs[i].gameObject.activeInHierarchy)
                {
                    return myMetaballs[i];
                }
            }
        }

        return result;
    }
}
