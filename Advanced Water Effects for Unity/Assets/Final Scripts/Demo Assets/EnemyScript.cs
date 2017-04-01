﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{

    Animator anim;
    Rigidbody rb;

    public float hp = 100;
    public float speed = 1;
    float deathThreshold = 0.2f;

    AudioSource hissSource;
    public AudioClip hissSnd, deathSnd;

    FountainScript myDamageEffect;

    // Use this for initialization
    protected virtual void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        //Sets up audio
        hissSource = gameObject.AddComponent<AudioSource>();
        hissSource.spatialBlend = 1;
        hissSource.clip = hissSnd;
        hissSource.loop = true;

        hissSource.Play();

        myDamageEffect = GetComponentInChildren<FountainScript>();

        //Attempts to find a mud-based Marching Cubes grid
        if (GameObject.Find("MarchingCubes_Mud"))
        {
            myDamageEffect.myManager = GameObject.Find("MarchingCubes_Mud").GetComponent<MetaballManager>();
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        float newWeight = Mathf.Lerp(anim.GetLayerWeight(1), 0, 0.1f * Time.deltaTime);
        anim.SetLayerWeight(1, newWeight);

        hissSource.volume = Mathf.Lerp(hissSource.volume, 0, 3 * Time.deltaTime);
    }

    protected virtual void FixedUpdate()
    {
        Vector3 dir = (PlayerScript.singleton.transform.position - transform.position);
        dir.y = 0;
        dir = dir.normalized;

        float prevY = rb.velocity.y;
        rb.velocity = dir * speed * 0.25f;
        rb.velocity = new Vector3(rb.velocity.x, prevY, rb.velocity.z);

        rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(dir), 3 * Time.deltaTime);
    }

    protected void Hurt(float dmg, Collision col = null)
    {
        hp -= dmg;

        anim.SetLayerWeight(1, anim.GetLayerWeight(1) + dmg * 0.991f);
        hissSource.volume += dmg * Time.deltaTime;

        transform.localScale -= Vector3.one * 0.05f;

        if (col != null)
        {
            myDamageEffect.transform.position = col.contacts[0].point;
            myDamageEffect.FlowForSeconds(0.16f);
            myDamageEffect.transform.rotation = Quaternion.LookRotation(col.contacts[0].normal);
        }

        if (transform.localScale.x < deathThreshold || hp < 1)
        {
            myDamageEffect.Burst(20);
            Destroy(gameObject);
        }

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }

    void OnCollisionStay(Collision col)
    {
        MetaballScript m = col.gameObject.GetComponent<MetaballScript>();

        if (m)
        {
            if (m.CanHit())
            {
                Hurt(5, col);
            }
            else
            {
                Hurt(0.25f * Time.deltaTime, col);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        EnemySpawnerScript.EnemyHasDied();
    }
}