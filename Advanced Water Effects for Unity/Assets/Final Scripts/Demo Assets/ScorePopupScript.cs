using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScorePopupScript : MonoBehaviour {

    Text myScoreText;
    float alpha = 1.5f;

    Transform cam;

	// Use this for initialization
	void Start () {
        cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        
	}

    // Update is called once per frame
    void Update()
    {
        alpha -= Time.deltaTime;
        myScoreText.color = new Color(1, 1, 1, alpha);

        if (alpha < 0.9f)
        {
            transform.Translate(Vector3.up * Time.deltaTime * 2);
        }

        transform.rotation = Quaternion.LookRotation(cam.forward);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 0.4f, 5 * Time.deltaTime);

        if(alpha<=0)
        {
            Destroy(gameObject);
        }
    }

    public void GiveNewScore(int _score)
    {
        transform.localScale = Vector3.one * 2f;
        myScoreText = GetComponentInChildren<Text>();
        myScoreText.text = "+ " + _score;
    }
}
