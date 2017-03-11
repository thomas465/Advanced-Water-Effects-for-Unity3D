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
    static float baseGridSize = 1.5f;
    static int baseGridMaxAmount = 24, baseGridRes = 24;

    //Pool of nodes
    public static List<MarchingSquaresGrid.CellCorner> allCellCorners;

    void Awake()
    {
        if (singleton)
        {
            Destroy(gameObject);
        }
        else
        {
            singleton = this;
            CreateGridPool(baseGridMaxAmount);
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

    public void CreateStain(Vector3 pos, Vector3 normal, GameObject surfaceObj, Material newMaterial = null, float size = 1, float metaballSize = 1)
    {
        if (!IsExistingGrid(pos, size, metaballSize))
        {
            GetAvailableGrid().Create(pos, normal, baseGridSize * size, baseGridRes, surfaceObj, newMaterial, metaballSize);
        }
    }

    //public void CreateGrid(Collision col, Material newMaterial = null)
    //{
    //    if (!IsExistingGrid(col.contacts[0].point))
    //    {
    //        GetAvailableGrid().Create(col.contacts[0].point, col.contacts[0].normal, gridSize, gridRes, col.gameObject, newMaterial);
    //    }
    //}

    bool IsExistingGrid(Vector3 pos, float size, float metaballSize)
    {
        float threshold = baseGridSize * size * 0.15f;
        float burstThreshold = baseGridSize * 0.1f;

        for (int i = 0; i < gridPool.Length; i++)
        {
            float dist = Vector3.Distance(pos, gridPool[i].transform.position);

            if (dist <= threshold && gridPool[i].gameObject.activeInHierarchy)
            {
                if (dist >= burstThreshold)
                {
                    //Debug.Log("Bursting from old one");
                    gridPool[i].BurstMetaballs(1, pos, 1, metaballSize);
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
                baseGridMaxAmount++;
                baseGridRes++;
                Debug.Log(baseGridRes);
                Rebuild();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (baseGridMaxAmount > 1 && baseGridRes > 8)
                {
                    baseGridMaxAmount--;
                    baseGridRes--;
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

        CreateGridPool(baseGridMaxAmount);
    }
}
