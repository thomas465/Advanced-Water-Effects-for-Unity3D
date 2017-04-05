using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScript : MonoBehaviour {

    public static GameOverScript singleton;

    public Image background;
    public Text mainText;

    float resetTimer = 5;

    void Awake()
    {
        singleton = this;
        gameObject.SetActive(false);
    }

	// Use this for initialization
	void Start () {
		
	}

    // Update is called once per frame
    void Update()
    {
        background.color = Color.Lerp(background.color, new Color(0, 0, 0, 0.9995f), 3 * Time.deltaTime);

        if (background.color.a > 0.75f)
        {
            mainText.color = Color.Lerp(mainText.color, new Color(1, 1, 1, 1f), 6 * Time.deltaTime);
            float newY = Mathf.Sin(Time.timeSinceLevelLoad * 3) * 6;

            mainText.transform.localPosition = new Vector3(0, newY, 0);
        }

        resetTimer -= Time.deltaTime;

        if(resetTimer<=0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Activate(int score)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);

            mainText.text = "Game Over!\nFinal Score: " + score;

            background.color = Color.clear;
            mainText.color = Color.clear;
        }
    }
}
