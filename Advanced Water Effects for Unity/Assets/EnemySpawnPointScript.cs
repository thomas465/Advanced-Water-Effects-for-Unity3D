using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPointScript : MonoBehaviour {

    static EnemySpawnPointScript[] allSpawnPoints;

    void Awake()
    {
        if(allSpawnPoints==null)
        {
            allSpawnPoints = Object.FindObjectsOfType<EnemySpawnPointScript>();
        }
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public static int SpawnRandomly(GameObject enemyPrefab)
    {
        int randomIndex = Random.Range(0, allSpawnPoints.Length);

        GameObject.Instantiate<GameObject>(enemyPrefab, allSpawnPoints[randomIndex].transform.position, allSpawnPoints[randomIndex].transform.rotation);

        return randomIndex;
    }
}
