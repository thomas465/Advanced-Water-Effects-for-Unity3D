using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour {

    Animator anim;
    Rigidbody rb;

    public float hp = 100;
    public float speed = 1;

    AudioSource hissSource;
    public AudioClip hissSnd, deathSnd;

    // Use this for initialization
    protected virtual void Start() {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        hissSource = gameObject.AddComponent<AudioSource>();
        hissSource.spatialBlend = 1;
        hissSource.clip = hissSnd;
        hissSource.loop = true;

        hissSource.Play();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        float newWeight = Mathf.Lerp(anim.GetLayerWeight(1), 0, 2 * Time.deltaTime);
        anim.SetLayerWeight(1, newWeight);

        hissSource.volume = Mathf.Lerp(hissSource.volume, 0, 3 * Time.deltaTime);
    }

    protected virtual void FixedUpdate()
    {
        Vector3 dir = (PlayerScript.singleton.transform.position - transform.position);
        rb.AddForce(dir.normalized * speed * 0.02f, ForceMode.VelocityChange);

        rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(dir), 3 * Time.deltaTime);
    }

    protected void Hurt(float dmg)
    {
        hp -= dmg;
        //anim.SetFloat("Hit", anim.GetFloat("Hit") + dmg * 0.1f);
        anim.SetLayerWeight(1, anim.GetLayerWeight(1) + dmg * 0.1f);
        hissSource.volume += dmg * Time.deltaTime;
    }

    void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.GetComponent<MetaballScript>())
        {
            Hurt(5);
        }
    }
}
