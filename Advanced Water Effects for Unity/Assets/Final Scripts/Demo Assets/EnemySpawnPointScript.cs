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

    void OnDestroy()
    {
        allSpawnPoints = null;
    }

    public static int SpawnRandomly(GameObject enemyPrefab, float difficulty)
    {
        int randomIndex = Random.Range(0, allSpawnPoints.Length);

        GameObject newEnemy = GameObject.Instantiate<GameObject>(enemyPrefab, allSpawnPoints[randomIndex].transform.position, allSpawnPoints[randomIndex].transform.rotation);
        newEnemy.GetComponent<EnemyScript>().ApplyDifficulty(difficulty);

        return randomIndex;
    }
}
