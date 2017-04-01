using UnityEngine;
using System.Collections;

public class MetaballScript : MonoBehaviour
{

    Rigidbody rb;
    float scale = 0.2f;

    public MetaballManager myManager;
    MarchingSquaresManager myMSManager;

    public MetaballManager.Metaball myInfo;

    bool scaleUp = true;
    float maxSize = 1;

    float viscocity = 0.95f;

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

        //hideFlags = HideFlags.HideInHierarchy;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
    }

    // Update is called once per frame
    void Update()
    {
        myInfo.pos = transform.position;
        myInfo.life -= Time.deltaTime;

        if(myInfo.life<=1)
        {
            myInfo.radius = Mathf.Lerp(0, myInfo.radius, myInfo.life);
        }

        if (myInfo.life <= 0)
        {
            Die();
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
        maxSize = myInfo.radius;
        bounced = false;

        gameObject.SetActive(true);
    }

    //Previously, the decal grids were generated here
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
            //CreateMarchingSquaresNodesOnSurface(col);

            //myMSManager.MetaballCollision(col, this);

            //Debug.Log("Metaball hit layer " + col.gameObject.layer);

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

            //Makes fluids break down if they're not gloopy
            myInfo.radius *= viscocity;
            transform.localScale *= viscocity;


            //myMSManager.CreateDecalSquares(transform.position, col.contacts[0].normal, rb.velocity.normalized, myInfo.radius, rb.velocity.magnitude);

            bounced = true;
        }
    }
}
