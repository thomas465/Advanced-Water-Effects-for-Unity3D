using UnityEngine;
using System.Collections;

public class MetaballScript : MonoBehaviour
{
    Rigidbody rb;

    public MetaballManager myManager;
    MarchingSquaresManager myMSManager;

    public MetaballManager.Metaball myInfo;
    public MetaballPoolScript myMetaballPool;

    [Range(0.75f, 1.0f)]
    public float viscocity = 0.95f;

    bool bounced = false;

    protected bool shrinkAway = true;

    // Use this for initialization
    void OnEnable()
    {
        if (myManager)
        {
            if (!myMSManager)
            {
                myMSManager = myManager.GetComponentInChildren<MarchingSquaresManager>();
            }

            myManager.allMetaballs.Add(myInfo);
        }
        else
        {
            Debug.LogError("Metaball has no manager!");
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
    }

    /// <summary>
    /// Updates this metaball's info struct, counts down its life and destroys it when life reaches 0.
    /// </summary>
    void Update()
    {
        myInfo.pos = transform.position;
        myInfo.life -= Time.deltaTime;

        //Makes the metaball get smaller rather than suddenly disappearing
        if(myInfo.life<=1 && shrinkAway)
        {
            myInfo.radius = Mathf.Lerp(0, myInfo.radius, myInfo.life);
        }

        if (myInfo.life <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Splits the metaball into two metaballs and each of the new metaballs have half the radius of the original metaball.
    /// </summary>
    /// <param name="howManyTimes"></param>
    public void Split(int howManyTimes)
    {
        for(int i=0; i<howManyTimes; i++)
        {
            Vector3 dir = Vector3.zero;

            float randomSize = 2;

            dir += transform.right * Random.Range(-randomSize, randomSize);
            dir += transform.forward * Random.Range(-randomSize, randomSize);

            float newSize = myInfo.radius * 0.5f;
            myInfo.radius = newSize;

            if (myMetaballPool)
            {
                myMetaballPool.FireMetaball(myManager, transform.position, dir, Random.Range(GetVelocity().magnitude * 0.2f, GetVelocity().magnitude * 0.5f), newSize, myInfo.life);
            }
        }
    }

    /// <summary>
    /// Removes this metaball from its metaball manager's metaball list and sets this gameobject to be inactive until it is fired again
    /// </summary>
    public void Die()
    {
        myInfo.radius = 0;

        if (myManager)
        {
            myManager.allMetaballs.Remove(myInfo);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Basic code for seeing if a collision is a direct hit or if the metaball is just sitting on the floor
    /// </summary>
    /// <returns></returns>
    public bool CanHit()
    {
        return GetVelocity().magnitude>4 && gameObject.layer==4;
    }

    /// <summary>
    /// Gives the metaball a new velocity and resets the metaball's info
    /// </summary>
    /// <param name="newVelo"></param>
    public void Fire(Vector3 newVelo)
    {
        if (!rb)
        {
            Awake();
        }

        if (rb)
        {
            rb.velocity = newVelo;
        }

        myInfo.pos = transform.position;
        bounced = false;

        gameObject.SetActive(true);
    }

    //Previously, the decal grids were generated here but this functionality has since moved to DecalPoolScript.
    //void CreateMarchingSquaresNodesOnSurface(Collision col)
    //{
    //    transform.rotation = Quaternion.LookRotation(-col.contacts[0].normal);

    //    Vector3 gridUp, gridRight;

    //    gridUp = transform.up;
    //    gridRight = transform.right;

    //    float size = 2;
    //    float movementSize = 0.5f;

    //    Vector3 curGridPos = transform.position - (gridRight * size * 0.5f) + (gridUp * size * 0.5f);
    //    Vector3 startGridPos = curGridPos;

    //    for (float y = 0; y < size; y += movementSize)
    //    {
    //        for (float x = 0; x < size; x += movementSize)
    //        {
    //            myMSManager.CreateCell(curGridPos, gridRight * movementSize, gridUp * movementSize, transform.forward);
    //            curGridPos += gridRight * movementSize;
    //        }

    //        curGridPos -= gridRight * size;
    //        curGridPos -= gridUp * movementSize;
    //    }
    //}

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    /// <summary>
    /// Bounces the metaball off of the surface and tells the DecalPool to create a decal stain if appropriate
    /// </summary>
    /// <param name="col"></param>
    protected virtual void OnCollisionEnter(Collision col)
    {
        if (!bounced && col.gameObject.layer!=10 && col.gameObject.layer != 4 && col.gameObject.layer!=LayerMask.NameToLayer("Mud"))
        {
            //This asks the DecalPool to create a Marching Squares grid at the point of collision
            if (DecalPoolScript.singleton && myManager)
            {
                DecalPoolScript.singleton.CreateStain(col.contacts[0].point, col.contacts[0].normal, col.gameObject, myManager.GetComponent<MeshRenderer>().materials[0]);
            }

            Vector3 normal = col.contacts[0].normal;
            Vector3 sideVelocity = Vector3.Cross(normal, Vector3.right);

            transform.rotation = Quaternion.LookRotation(sideVelocity, normal);
            transform.Rotate(Vector3.up * Random.Range(0, 360));

            if (rb)
            {
                Vector3 velo = col.relativeVelocity * 0.5f;
                rb.velocity += transform.forward * Random.Range(velo.magnitude * 0.5f, velo.magnitude * 0.75f);
            }

            //Makes fluids break down based on their viscocity
            myInfo.radius *= viscocity;
            transform.localScale *= viscocity;

            bounced = true;
        }
    }
}
