using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script simply moves the attached object back and forth using Mathf.Sin
public class SineCupScript : MonoBehaviour
{

    Vector3 pos;
    public float sineSpeed, sineLength;
    public float acceleration;

    // Use this for initialization
    void Start()
    {
        pos = transform.position;
    }


    /// <summary>
    /// This moves the cup back and forth while slowly increasing the speed so that a more dramatic effect happens to the water inside.
    /// </summary>
    void FixedUpdate()
    {
        pos.x = Mathf.Sin(Time.timeSinceLevelLoad * sineSpeed) * sineLength;
        transform.position = pos;

        sineSpeed += acceleration * Time.deltaTime;
    }
}
