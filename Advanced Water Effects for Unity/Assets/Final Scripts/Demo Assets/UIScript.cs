using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIScript : MonoBehaviour
{
    public static UIScript singleton;


    // Use this for initialization
    void Start()
    {
        if (!singleton)
        {
            DontDestroyOnLoad(gameObject);
            singleton = this;

            if(Application.isEditor)
            {
                GetComponentInChildren<UnityEngine.UI.Text>().text = "Scene switching is disabled when playing in Editor";
            }
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SceneManager.LoadScene(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SceneManager.LoadScene(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SceneManager.LoadScene(2);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                SceneManager.LoadScene(3);
            if (Input.GetKeyDown(KeyCode.Alpha5))
                SceneManager.LoadScene(4);

            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();
        }
    }
}
