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
        dir.y = 0;
        dir = dir.normalized;

        float prevY = rb.velocity.y;
        rb.velocity = dir * speed * 0.25f;
        rb.velocity = new Vector3(rb.velocity.x, prevY, rb.velocity.z);

        rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(dir), 3 * Time.deltaTime);
    }

    protected void Hurt(float dmg)
    {
        hp -= dmg;
        //anim.SetFloat("Hit", anim.GetFloat("Hit") + dmg * 0.1f);
        anim.SetLayerWeight(1, anim.GetLayerWeight(1) + dmg * 0.1f);
        hissSource.volume += dmg * Time.deltaTime;

        transform.localScale -= Vector3.one * 0.05f;

        if(transform.localScale.x<0.15f)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        MetaballScript m = col.gameObject.GetComponent<MetaballScript>();

        if (m)
        {
            if (m.CanHit())
            {
                Hurt(5);
            }
        }
    }
}
