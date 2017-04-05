using UnityEngine;
using System.Collections;

/// <summary>
/// This is essentially a metaball emitter and should be used similarly to a Particle System.
/// </summary>
public class FountainScript : MonoBehaviour {

    public MetaballManager myManager;
    public MetaballPoolScript myMetaballPool;

    public float metaballMinSize = 0.2f, metaballMaxSize = 0.5f, metaballMinSpeed = 4, metaballMaxSpeed = 8, metaballLife = 2;
    public float directionRandomMultiplier = 0.5f;

    public float rateOfFireMin = 0.05f, rateOfFireMax = 0.2f;

    float timer = 0.1f;

    public bool active = true;

    public Rigidbody attachedToThisRB;

    float flowForSeconds = 0;

    /// <summary>
    /// If no specific manager or pool is given, this function will try to find one.
    /// </summary>
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
	
	protected virtual void Update () {

        //Handles the "flow for this many seconds" functionality
        if (flowForSeconds > 0)
        {
            flowForSeconds -= Time.deltaTime;
            active = true;

            if (flowForSeconds <= 0)
            {
                active = false;
            }
        }

        //If this fountain is active, a timer is used to decide when to release a metaball.
        //A random direction is calculated based on given values, a metaball is fired and the timer is
        //reset.
        if (active)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                Vector3 dir = transform.up;

                float randomSize = directionRandomMultiplier;

                dir += transform.right * Random.Range(-randomSize, randomSize);
                dir += transform.forward * Random.Range(-randomSize, randomSize);

                myMetaballPool.FireMetaball(myManager, transform.position, dir, Random.Range(metaballMinSpeed, metaballMaxSpeed), Random.Range(metaballMinSize, metaballMaxSize), metaballLife);

                timer = Random.Range(rateOfFireMin, rateOfFireMax);
            }
        }
	}

    /// <summary>
    /// Bursts out a given amount of metaballs at once.
    /// </summary>
    /// <param name="amount"></param>
    public void Burst(int amount)
    {
        for(int i=0; i<amount; i++)
        {
            Vector3 dir = transform.up;

            float randomSize = directionRandomMultiplier;

            dir += transform.right * Random.Range(-randomSize, randomSize);
            dir += transform.forward * Random.Range(-randomSize, randomSize);

            myMetaballPool.FireMetaball(myManager, transform.position, dir, Random.Range(metaballMinSpeed, metaballMaxSpeed), Random.Range(metaballMinSize, metaballMaxSize), metaballLife);

            timer = Random.Range(rateOfFireMin, rateOfFireMax);
        }
    }

    /// <summary>
    /// This causes the fountain to produce metaballs for the given amount of time in seconds.
    /// </summary>
    /// <param name="_time"></param>
    public virtual void FlowForSeconds(float _time)
    {
        timer = 0;
        active = true;
        flowForSeconds = _time;
    }
}
