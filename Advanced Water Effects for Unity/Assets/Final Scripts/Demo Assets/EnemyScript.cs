using System.Collections;
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

    public FountainScript mudFountain;

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

        //myDamageEffect = GetComponentInChildren<FountainScript>();

        //Attempts to find a mud-based Marching Cubes grid
        if (GameObject.Find("MarchingCubes_Mud"))
        {
            mudFountain.myManager = GameObject.Find("MarchingCubes_Mud").GetComponent<MetaballManager>();
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //Gradually adjusts animation layers back to default
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

        //Moves and rotates the enemy towards the player
        rb.velocity = new Vector3(rb.velocity.x, prevY, rb.velocity.z);
        rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(dir), 3 * Time.deltaTime);
    }

    /// <summary>
    /// Takes damage, gets smaller, changes animation, plays a sound and checks to see if the monster is now dead.
    /// </summary>
    /// <param name="dmg"></param>
    /// <param name="col"></param>
    protected void Hurt(float dmg, Collision col = null)
    {
        hp -= dmg;

        anim.SetLayerWeight(1, anim.GetLayerWeight(1) + dmg * 0.991f);
        hissSource.volume += dmg * Time.deltaTime;

        transform.localScale -= Vector3.one * 0.05f;

        if (col != null)
        {
            mudFountain.transform.position = col.contacts[0].point;
            mudFountain.FlowForSeconds(0.1f);
            mudFountain.transform.rotation = Quaternion.LookRotation(col.contacts[0].normal);
        }

        //If the monster is too small to carry on, it dies here leaving a metaball explosion and
        //gives the player 100 points using ScoreManagerScript.
        if (transform.localScale.x < deathThreshold || hp < 1)
        {
            if (gameObject.activeInHierarchy)
            {
                mudFountain.Burst(20);
                ScoreManagerScript.singleton.GiveScore(100, transform.position);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        //Tries to prevent the monster from moving upwards due to the collision
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
    }

    /// <summary>
    /// Detects if the collider is a metaball and if so, takes damage.
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionStay(Collision col)
    {
        MetaballScript m = col.gameObject.GetComponent<MetaballScript>();

        if (m)
        {
            //If the metaball is currently going quickly through the air, take lots of damage
            if (m.CanHit())
            {
                Hurt(5, col);
            }
            else
            {
                //If the metaball is dormant, the monster takes some damage for each frame it is inside it
                Hurt(0.25f * Time.deltaTime, col);
            }
        }
        else
        {

        }
    }

    /// <summary>
    /// Damages the player on collision
    /// </summary>
    /// <param name="col"></param>
    void OnCollisionEnter(Collision col)
    {
        PlayerScript p = col.gameObject.GetComponent<PlayerScript>();

        if (p)
        {
            HealthbarScript.singleton.TakeDamage(15);
            p.transform.Translate(Vector3.up * 0.1f);

            float force = 5;
            p.GetComponent<Rigidbody>().AddForce(transform.forward * force + (Vector3.up * force * 0.5f), ForceMode.VelocityChange);
        }
    }

    protected virtual void OnDestroy()
    {
        //Lets the wave manager know that an enemy has died.
        EnemySpawnerScript.EnemyHasDied();
    }

    public void ApplyDifficulty(float difficulty)
    {
        speed += difficulty * 0.35f;
        transform.localScale *= 1 + (difficulty * 0.09f);
    }
}
