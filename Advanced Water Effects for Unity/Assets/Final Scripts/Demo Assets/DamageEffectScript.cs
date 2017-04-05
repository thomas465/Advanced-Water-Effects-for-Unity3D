using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageEffectScript : MonoBehaviour {

    static Image effect;
    Color clear;

	// Use this for initialization
	void Start () {
        effect = GetComponent<Image>();
        clear = effect.color;
        clear.a = 0;

        effect.color = clear;
	}
	
	// Update is called once per frame
	void Update () {
        effect.color = Color.Lerp(effect.color, clear, 6 * Time.deltaTime);
	}

    public static void Hit()
    {
        effect.color = new Color(effect.color.r, effect.color.g, effect.color.b, 0.45f);
    }
}
