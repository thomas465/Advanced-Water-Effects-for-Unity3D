using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalPoolScript : MonoBehaviour
{

    public static DecalPoolScript singleton;

    MarchingSquaresGrid[] gridPool;
    public GameObject vMPool;

    int curIndex = 0;

    //Consts
    static float gridSize = 1.5f;
    static int gridMaxAmount = 120, gridRes = 32;

    void Awake()
    {
        if (singleton)
        {
            Destroy(gameObject);
        }
        else
        {
            singleton = this;
            CreateGridPool(gridMaxAmount);
            //DontDestroyOnLoad(gameObject);
        }
    }

    void CreateGridPool(int num)
    {
        gridPool = new MarchingSquaresGrid[num];

        for (int i = 0; i < num; i++)
        {
            gridPool[i] = Instantiate<GameObject>(vMPool).GetComponent<MarchingSquaresGrid>();

            //DontDestroyOnLoad(gridPool[i].gameObject);
            gridPool[i].gameObject.SetActive(false);
        }
    }

    void OnLevelWasLoaded()
    {
        for (int i = 0; i < gridPool.Length; i++)
        {
            gridPool[i].Disable();
        }
    }

    public void CreateStain(Vector3 pos, Vector3 normal, GameObject surfaceObj, Material newMaterial = null)
    {
        if (!IsExistingGrid(pos))
        {
            GetAvailableGrid().Create(pos, normal, gridSize, gridRes, surfaceObj, newMaterial);
        }
    }

    //public void CreateGrid(Collision col, Material newMaterial = null)
    //{
    //    if (!IsExistingGrid(col.contacts[0].point))
    //    {
    //        GetAvailableGrid().Create(col.contacts[0].point, col.contacts[0].normal, gridSize, gridRes, col.gameObject, newMaterial);
    //    }
    //}

    bool IsExistingGrid(Vector3 pos)
    {
        float threshold = gridSize * 0.25f;
        float burstThreshold = gridSize * 0.1f;

        for (int i = 0; i < gridPool.Length; i++)
        {
            float dist = Vector3.Distance(pos, gridPool[i].transform.position);

            if (dist <= threshold && gridPool[i].gameObject.activeInHierarchy)
            {
                if (dist >= burstThreshold)
                {
                    gridPool[i].BurstMetaballs(1, pos, 1);
                }

                return true;
            }
        }

        //existingCell = new Cell();
        return false;
    }

    MarchingSquaresGrid GetAvailableGrid()
    {
        for (int i = 0; i < gridPool.Length; i++)
        {
            if (gridPool[i].gameObject.activeInHierarchy)
            {

            }
            else
            {
                return gridPool[i];
            }
        }

        curIndex++;

        if (curIndex >= gridPool.Length)
        {
            curIndex = 0;
        }

        Debug.Log("RESTPO");

        return gridPool[curIndex];
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                MarchingSquaresGrid.marchingSquaresEnabled = !MarchingSquaresGrid.marchingSquaresEnabled;
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                MarchingSquaresGrid.interpolation = !MarchingSquaresGrid.interpolation;
            }

            if(Input.GetKeyDown(KeyCode.E))
            {
                gridMaxAmount++;
                gridRes++;
                Debug.Log(gridRes);
                Rebuild();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (gridMaxAmount > 1 && gridRes > 8)
                {
                    gridMaxAmount--;
                    gridRes--;
                    Rebuild();
                }
            }
        }
    }

    void Rebuild()
    {
        for(int i=0; i<gridPool.Length; i++)
        {
            Destroy(gridPool[i].gameObject);
        }

        CreateGridPool(gridMaxAmount);
    }
}
