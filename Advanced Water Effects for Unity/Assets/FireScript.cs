using UnityEngine;
using System.Collections;

public class FireScript : MonoBehaviour
{

    public GameObject steamPrefab;
    float health = 100;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        //MetaballScript otherM = other.GetComponent<MetaballScript>();

        if (other.gameObject.layer == 4)
        {
            health -= 10;

            if (health <= 0)
                Extinguish();
        }
    }

    public void Extinguish()
    {
        Instantiate<GameObject>(steamPrefab).transform.position = transform.position;
        GetComponent<ParticleSystem>().Stop();
        GetComponent<SphereCollider>().enabled = false;
        Destroy(gameObject, 3);
    }
}
