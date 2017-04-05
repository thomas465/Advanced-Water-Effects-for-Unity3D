using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManagerScript : MonoBehaviour {

    public static ScoreManagerScript singleton;

    public Text score;
    int curScore = 0;

    public GameObject scorePopupPrefab;

    public int GetScore()
    {
        return curScore;
    }

    // Use this for initialization
    void Awake() {
        singleton = this;
        GiveScore(0, Vector3.zero);
    }
	
	// Update is called once per frame
	void Update () {
		
	}


    public void GiveScore(int _score, Vector3 pos)
    {
        GameObject newPopup = Instantiate<GameObject>(singleton.scorePopupPrefab);
        newPopup.transform.position = pos;

        newPopup.GetComponent<ScorePopupScript>().GiveNewScore(_score);

        curScore += _score;
        score.text = "Score: <i>" + curScore + "</i>";
        //Debug.Break();
    }
}
