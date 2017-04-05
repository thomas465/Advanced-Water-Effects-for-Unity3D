using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthbarScript : MonoBehaviour {

    public static HealthbarScript singleton;

    public float health = 100;
    public Image healthbarGreen;

    void Awake()
    {
        singleton = this;
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        healthbarGreen.fillAmount = Mathf.Lerp(healthbarGreen.fillAmount, health * 0.01f, 8 * Time.deltaTime);
	}

    public void TakeDamage(float dmg)
    {
        health -= dmg;
        DamageEffectScript.Hit();

        if(health<0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        PlayerScript.singleton.GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>().enabled = false;
        PlayerScript.singleton.enabled = false;
        GameOverScript.singleton.Activate(ScoreManagerScript.singleton.GetScore());
    }

    public void Reset()
    {
        health = 100;
        healthbarGreen.fillAmount = 1;
    }
}
