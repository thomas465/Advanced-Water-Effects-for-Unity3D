using UnityEngine;
using System.Collections;

public class HoseScript : FountainScript {

    protected override void Update()
    {
        active = Input.GetButton("Fire1");

        base.Update();
    }
}
