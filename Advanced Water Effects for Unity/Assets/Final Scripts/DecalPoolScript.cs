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
    static float baseGridSize = 1.25f;
    static int baseGridMaxAmount = 32, baseGridRes = 24;

    //Pool of nodes
    public static List<MarchingSquaresGrid.CellCorner> allCellCorners;

    void Awake()
    {
        if (singleton)
        {
            Destroy(this);
        }
        else
        {
            singleton = this;
            CreateGridPool(baseGridMaxAmount);
            DontDestroyOnLoad(gameObject);

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }
    }

    private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
    {
        Reset();
    }

    /// <summary>
    /// Sets up the pool of inactive grids
    /// </summary>
    /// <param name="num"></param>
    void CreateGridPool(int num)
    {
        gridPool = new MarchingSquaresGrid[num];

        for (int i = 0; i < num; i++)
        {
            {
                gridPool[i] = Instantiate<GameObject>(vMPool).GetComponent<MarchingSquaresGrid>();

                DontDestroyOnLoad(gridPool[i].gameObject);
                gridPool[i].gameObject.SetActive(false);
                gridPool[i].hideFlags = HideFlags.HideInHierarchy;
            }
        }
    }

    void Reset()
    {
        if (gridPool!=null)
        {
            for (int i = 0; i < gridPool.Length; i++)
            {
                if (gridPool[i])
                {
                    Destroy(gridPool[i]);
                }
            }
        }

        CreateGridPool(baseGridMaxAmount);
    }

    /// <summary>
    /// Activates a grid from the pool and places it at the given position
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="normal"></param>
    /// <param name="surfaceObj"></param>
    /// <param name="newMaterial"></param>
    /// <param name="size"></param>
    /// <param name="metaballSize"></param>
    public void CreateStain(Vector3 pos, Vector3 normal, GameObject surfaceObj, Material newMaterial = null, float size = 1, float metaballSize = 1)
    {
        if (!IsExistingGrid(pos, size, metaballSize))
        {
            GetAvailableGrid().Create(pos, normal, baseGridSize * size, baseGridRes, surfaceObj, newMaterial, metaballSize);
        }
    }

        /// <summary>
        /// Checks to see if there is an available grid in this location already which could be re-used for this collision, and creates more metaballs on THAT grid rather than making a new grid.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="metaballSize"></param>
        /// <returns></returns>
    bool IsExistingGrid(Vector3 pos, float size, float metaballSize)
    {
        float threshold = baseGridSize * size * 0.25f;
        float burstThreshold = baseGridSize * 0.1f;

        for (int i = 0; i < gridPool.Length; i++)
        {
            if (gridPool[i])
            {
                float dist = Vector3.Distance(pos, gridPool[i].transform.position);

                if (dist <= threshold && gridPool[i].gameObject.activeInHierarchy)
                {
                    if (dist >= burstThreshold)
                    {
                        //Debug.Log("Re-using old grid");
                        Debug.DrawLine(pos, gridPool[i].transform.position, Color.magenta, 2);
                        gridPool[i].BurstMetaballs(1, pos, 1, metaballSize);
                    }

                    return true;
                }

            }
        }

        //existingCell = new Cell();
        return false;
    }

    /// <summary>
    /// Tries to find an inactive grid, and if there are none then it takes the oldest active grid it can find.
    /// </summary>
    /// <returns></returns>
    MarchingSquaresGrid GetAvailableGrid()
    {
        for (int i = 0; i < gridPool.Length; i++)
        {
            if (gridPool[i])
            {
                if (gridPool[i].gameObject.activeInHierarchy)
                {

                }
                else
                {
                    return gridPool[i];
                }
            }
        }

        curIndex++;

        if (curIndex >= gridPool.Length)
        {
            curIndex = 0;
        }

        return gridPool[curIndex];
    }

    // Update is called once per frame
    void Update()
    {
        //Debug code - Pressing Right CTRL and other buttons will change settings
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

    /// <summary>
    /// Destroys all active grids and makes a fresh pool
    /// </summary>
    void Rebuild()
    {
        for(int i=0; i<gridPool.Length; i++)
        {
            Destroy(gridPool[i].gameObject);
        }

        CreateGridPool(baseGridMaxAmount);
    }
}
