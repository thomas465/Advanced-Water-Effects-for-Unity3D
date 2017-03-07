using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MudMonsterScript : EnemyScript {

    public Material stainMat;

    protected override void Start()
    {
        base.Start();

        StartCoroutine("LeaveTrail");
    }

    IEnumerator LeaveTrail()
    {
        while(true)
        {
            if(gameObject.activeInHierarchy)
            {
                RaycastHit rH;

                if(Physics.Raycast(transform.position, -transform.up, out rH))
                {
                    DecalPoolScript.singleton.CreateStain(rH.point, rH.normal, rH.collider.gameObject, stainMat);
                }
            }

            yield return new WaitForSeconds(0.4f);
        }
    }
}
