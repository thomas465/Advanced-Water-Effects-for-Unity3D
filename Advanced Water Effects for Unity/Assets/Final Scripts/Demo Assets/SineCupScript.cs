using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineCupScript : MonoBehaviour
{

    Vector3 pos;
    public float sineSpeed, sineLength;

    // Use this for initialization
    void Start()
    {
        pos = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        pos.x = Mathf.Sin(Time.timeSinceLevelLoad * sineSpeed) * sineLength;
        transform.position = pos;
    }
}
