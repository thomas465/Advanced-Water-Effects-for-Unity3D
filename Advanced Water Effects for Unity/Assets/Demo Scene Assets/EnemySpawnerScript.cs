using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemySpawnerScript : MonoBehaviour {

    public static EnemySpawnerScript singleton;

    public Text waveDisplayText;
    int curWave = 0;

    [SerializeField]
    static int enemiesLeft;

    public GameObject mudMonster;

    float waveStartDelay = 3;

    void Awake()
    {
        singleton = this;
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(waveStartDelay>0)
        {
            waveStartDelay -= Time.deltaTime;

            if(waveStartDelay<=0)
            {
                NewWave();
            }
        }
    }

    void SpawnEnemy()
    {
        EnemySpawnPointScript.SpawnRandomly(mudMonster);
        enemiesLeft++;
    }

    public static void EnemyHasDied()
    {
        enemiesLeft--;

        if(enemiesLeft<1)
        {
            singleton.EndWave();
        }
    }

    public void EndWave()
    {
        if (waveDisplayText)
        {
            waveDisplayText.text = "Wave complete!!";
        }

        Debug.Log("Wave complete!");
        waveStartDelay = 3;
    }

    public void NewWave(int newWave)
    {
        curWave = newWave - 1;
        NewWave();
    }

    public void NewWave()
    {
        curWave++;

        Debug.Log("new wave: " + curWave);
        waveDisplayText.text = "Wave " + curWave;
        int enemiesThisWave = 1 + Mathf.FloorToInt(curWave * 0.3f);

        for(int i=0; i<enemiesThisWave; i++)
        {
            SpawnEnemy();
        }
    }
}
