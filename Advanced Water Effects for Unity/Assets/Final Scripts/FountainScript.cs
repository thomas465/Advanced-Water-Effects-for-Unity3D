using UnityEngine;
using System.Collections;

public class FountainScript : MonoBehaviour {

    public MetaballManager myManager;
    public MetaballPoolScript myMetaballPool;

    public float metaballMinSize = 0.2f, metaballMaxSize = 0.5f, metaballMinSpeed = 4, metaballMaxSpeed = 8, metaballLife = 2;
    public float directionRandomMultiplier = 0.5f;

    public float rateOfFireMin = 0.05f, rateOfFireMax = 0.2f;

    float timer = 0.1f;

    public bool active = true;

    public Rigidbody attachedToThisRB;

    // Use this for initialization
    protected virtual void Start()
    {
        if (!myManager)
        {
            myManager = Object.FindObjectOfType<MetaballManager>();
        }

        if (!myMetaballPool)
        {
            myMetaballPool = Object.FindObjectOfType<MetaballPoolScript>();
        }
    }
	
	// Update is called once per frame
	protected virtual void Update () {

        if (active)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Vector3 dir = transform.up;

                float randomSize = directionRandomMultiplier;

                dir += transform.right * Random.Range(-randomSize, randomSize);
                dir += transform.forward * Random.Range(-randomSize, randomSize);

                if(attachedToThisRB)
                {
                    //dir += attachedToThisRB.velocity;
                }

                myMetaballPool.FireMetaball(myManager, transform.position, dir, Random.Range(metaballMinSpeed, metaballMaxSpeed), Random.Range(metaballMinSize, metaballMaxSize), metaballLife);

                timer = Random.Range(rateOfFireMin, rateOfFireMax);
            }
        }
	}
}
