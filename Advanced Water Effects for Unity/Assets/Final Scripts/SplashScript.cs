using UnityEngine;
using System.Collections;

public class SplashScript : FountainScript {

    protected override void Start()
    {
        base.Start();


    }

    void LateUpdate()
    {
        int numberReleased = 12;
        float curZRotation = 0;
        Quaternion startRotation = transform.rotation;

        for (int i = 0; i < numberReleased; i++)
        {
            Vector3 direction = transform.up;

            Debug.DrawLine(transform.position, transform.position + direction, Color.magenta, 1);
            myMetaballPool.FireMetaball(myManager, transform.position, direction, metaballMaxSpeed, metaballMaxSize);

            curZRotation += 360 / numberReleased;
            transform.Rotate(Vector3.forward * (360 / numberReleased));
        }

        transform.rotation = startRotation;
        Destroy(gameObject);
    }

    protected override void Update()
    {
        //base.Update();
    }
}
