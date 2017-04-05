using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingSquaresGrid : MonoBehaviour
{
    int debug = 0;
    public static bool marchingSquaresEnabled = true, interpolation =  false;


    public ComputeShader myCS;
    ComputeBuffer cellCornerBuffer;
    ComputeBuffer metaballBuffer;

    [System.Serializable]
    struct Cell
    {
        public Vector3[] cornerPositions;
        public float[] cornerDensities;
        public bool disabled;

        public int[] cornerIndices;
    }

    [System.Serializable]
    public struct CellCorner
    {
        public Vector3 cornerPosition;
        public float density;
    }

    Cell[] allCells;

    [SerializeField]
    List<CellCorner> allNodes;

    List<Vector3> vertices;
    List<int> triangles;
    Mesh mesh;
    MeshRenderer meshRenderer;

    float worldSize = 0;
    float resolution = 0;
    int sideLength = 0;

    float animatedTime = 2;
    public float dryOutTime = 20;

    /// <summary>
    /// Struct used to hold info about metaballs like position and velocity
    /// </summary>
    [System.Serializable]
    public struct Metaball2D
    {
        public Vector3 pos, velocity;
        public float radius;

        public Metaball2D(Vector3 _pos, float _radius)
        {
            pos = _pos;
            radius = _radius;

            velocity = Vector3.zero;
        }
    }

    [SerializeField]
    List<Metaball2D> allMetaball2Ds;
    Vector3 metaballGravity;

    int sizeOfNodeStruct, sizeOfMetaball;

    Vector3 prevPos = Vector3.zero;
    Vector3 movementSinceLastFrame = Vector3.zero;

    Vector3 prevCellPos;

    void Awake()
    {
        sizeOfMetaball = (sizeof(float) * 6) + sizeof(float);
        sizeOfNodeStruct = (sizeof(float) * 4);
    }

    CellCorner[] gpuNodeInfo;
    Metaball2D[] gpuMetaballInfo;

    // Use this for initialization
    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Dynamic Decal Mesh (Marching Squares)";

        meshRenderer = GetComponent<MeshRenderer>();

        GetComponent<MeshFilter>().mesh = mesh;

        if(metaballBuffer==null)
        {
            metaballBuffer = new ComputeBuffer(1, 8);
        }

        if (cellCornerBuffer == null)
        {
            cellCornerBuffer = new ComputeBuffer(1, 8);
        }

        StartCoroutine("HeavyCalculations");
    }

    public void Enlarge()
    {
        //float maxSize = 10;

        //if (worldSize < maxSize)
        //{
        //    worldSize += 0.05f;
        //    resolution += 0.05f;

        //    Create(transform.position, transform.forward, worldSize, resolution);
        //}
    }

    /// <summary>
    /// Resets the grid and positions it at the new surface
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="forward"></param>
    /// <param name="newSize"></param>
    /// <param name="newResolution"></param>
    /// <param name="surface"></param>
    /// <param name="newMaterial"></param>
    public void Create(Vector3 pos, Vector3 forward, float newSize, float newResolution, GameObject surface, Material newMaterial = null, float metaballSize = 1)
    {
        if (newMaterial)
        {
            if (!meshRenderer)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            meshRenderer.material = newMaterial;
        }

        transform.position = pos + (forward * 0.025f);
        transform.rotation = Quaternion.LookRotation(forward);
        transform.SetParent(surface.transform);

        prevPos = transform.position;

        allCells = null;
        allNodes = new List<CellCorner>();

        vertices = new List<Vector3>();
        triangles = new List<int>();

        if (mesh)
        {
            mesh.Clear();
        }

        allMetaball2Ds = new List<Metaball2D>();

        animatedTime = dryOutTime;

        int i = 0;

        worldSize = newSize;
        resolution = newResolution;

        Vector3 curPos = transform.position - ((transform.right + transform.up) * (worldSize * 0.5f));
        //Vector3 startPos = curPos;

        //Debug.DrawLine(startPos, startPos + transform.forward, Color.magenta);

        float movementSize = worldSize / resolution;

        allCells = new Cell[(int)resolution * (int)resolution];

        for (int y = 0; y < (int)resolution; y++)
        {
            for (int x = 0; x < (int)resolution; x++)
            {
                //Debug.DrawLine(curPos, curPos + transform.forward);
                CreateCell(curPos, i, movementSize, x, y);
                i++;

                curPos += transform.right * movementSize;
            }

            curPos -= transform.right * (movementSize * resolution);
            curPos += transform.up * movementSize;
        }

        BurstMetaballs(1, transform.position, 6, metaballSize);

        //Gets the correct gravity direction while making sure that the metaballs won't move off of this 2D grid
        metaballGravity = -Vector3.up;
        metaballGravity = transform.InverseTransformDirection(metaballGravity);
        metaballGravity.z = 0;
        metaballGravity = transform.TransformDirection(metaballGravity);

        gameObject.SetActive(true);

        if (cellCornerBuffer != null)
        {
            cellCornerBuffer.Release();
        }

        cellCornerBuffer = new ComputeBuffer(allNodes.Count, sizeOfNodeStruct);
        cellCornerBuffer.SetData(allNodes.ToArray());

        sideLength = (int)Mathf.Sqrt(allNodes.Count);
    }

    void FixedUpdate()
    {      
       // Debug.Log(marchingSquaresEnabled);
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if(metaballBuffer!=null)
        {
            metaballBuffer.Release();
        }

        if(cellCornerBuffer!=null)
        {
            cellCornerBuffer.Release();
        }
    }

    void MoveMetaball(int metaballIndex)
    {
        Metaball2D curMetaball2D;
        curMetaball2D = allMetaball2Ds[metaballIndex];

        curMetaball2D.pos += curMetaball2D.velocity * Time.deltaTime;

        //Friction
        float friction = 3;
        curMetaball2D.velocity -= curMetaball2D.velocity * Time.deltaTime * friction;

        //Gravity
        float gravityIntensity = 0.5f;
        curMetaball2D.velocity += metaballGravity * Time.deltaTime * gravityIntensity;

        allMetaball2Ds[metaballIndex] = curMetaball2D;
    }

    void AssignDensities()
    {
        for (int cellIndex = 0; cellIndex < allCells.Length; cellIndex++)
        {
            for (int cornerIndex = 0; cornerIndex < allCells[cellIndex].cornerDensities.Length; cornerIndex++)
            {
                allCells[cellIndex].cornerDensities[cornerIndex] = 0;

                for (int metaballIndex = 0; metaballIndex < allMetaball2Ds.Count; metaballIndex++)
                {                   
                    Metaball2D m = allMetaball2Ds[metaballIndex];
                    //m.pos = transform.InverseTransformPoint(m.pos);
                    //Debug.DrawLine(m.pos, m.pos + transform.forward * 3, Color.magenta);

                    //float otherBit = (m.radius - Vector3.Magnitude((allCells[cellIndex].cornerPositions[cornerIndex] - m.pos)) * Vector3.Magnitude((allCells[cellIndex].cornerPositions[cornerIndex] - m.pos)));
                    //otherBit *= otherBit;
                    float dist = Vector3.Distance(m.pos, allCells[cellIndex].cornerPositions[cornerIndex]);

                    if (dist <= m.radius)
                    {
                        allCells[cellIndex].cornerDensities[cornerIndex] += 1 - (dist / m.radius);
                    }
                    else
                    {
                        allCells[cellIndex].cornerDensities[cornerIndex] += 0;
                    }


                    //allCells[cellIndex].cornerDensities[cornerIndex] += (m.radius / otherBit) * 0.0001f;
                }
            }
        }
    }

    /// <summary>
    /// Creates a given number of 2D metaballs at the given location and gives them velocity.
    /// The maxiumum number of metaballs allowed is also here.
    /// </summary>
    /// <param name="speed"></param>
    /// <param name="pos"></param>
    /// <param name="num"></param>
    /// <param name="size"></param>
    public void BurstMetaballs(float speed, Vector3 pos, float num = 6, float size = 1)
    {
        int maxBalls = 32;
        float ballSize = 0.35f * size;

        speed = speed * 3.5f;

        for (int i = 0; i < num; i++)
        {
            //This was an old approach - I decided it looks less jarring to prevent new metaballs then remove old ones
            if (allMetaball2Ds.Count >= maxBalls)
            {
                //allMetaball2Ds.RemoveAt(0);
            }

            if (allMetaball2Ds.Count < maxBalls)
            {
                Metaball2D newMetaball = new Metaball2D(pos, ballSize);

                newMetaball.velocity = new Vector2(Random.Range(-speed, speed), Random.Range(-speed, speed));
                newMetaball.velocity = transform.TransformDirection(newMetaball.velocity);

                newMetaball.radius = Mathf.Clamp(ballSize / (newMetaball.velocity.magnitude/ballSize), ballSize*0.2f, ballSize * 0.85f);

                allMetaball2Ds.Add(newMetaball);
                Debug.DrawLine(newMetaball.pos, newMetaball.pos + Vector3.up, Color.red, 0.1f);
            }
            else
            {
                Debug.DrawLine(pos, pos + Vector3.up * 100, Color.magenta, 0.1f);
            }
        }

        if (metaballBuffer != null)
        {
            metaballBuffer.Release();
        }

        metaballBuffer = new ComputeBuffer(allMetaball2Ds.Count, sizeOfMetaball);
        metaballBuffer.SetData(allMetaball2Ds.ToArray());
    }

    //This uses raycasting to find an appropriate place to perform the proper raycast.
    //This will ensure that the 2nd raycast has a clear path to the surface the metaball hit.
    Vector3 FindRaycastStart(Vector3 pos, Vector3 forward, float distanceForward)
    {
        RaycastHit rH;

        //return pos + transform.forward * 0.1f;

        //Debug.DrawLine(pos, pos + transform.forward * distanceForward, Color.green, 1);

        if (Physics.Raycast(pos + transform.forward*0.1f, transform.forward, out rH, distanceForward, LayerMask.GetMask("Default")))
        {
            Debug.DrawLine(pos, rH.point, Color.red, 4);
            //Debug.Break();
            
            return rH.point - transform.forward * 0.1f;
        }
        else
        {
            return pos + transform.forward * 0.1f;
        }
    }

    /// <summary>
    /// This creates a new cell and its corners
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="i"></param>
    /// <param name="cellSize"></param>
    void CreateCell(Vector3 pos, int i, float cellSize, int x, int y)
    {
        allCells[i] = new Cell();

        allCells[i].cornerDensities = new float[4];
        allCells[i].cornerPositions = new Vector3[4];

        RaycastHit rH;

        //Debug.DrawLine(pos, pos - transform.forward, Color.magenta, 2);
        float raycastDistance = 1;
        Vector3 startPos = FindRaycastStart(pos, transform.forward, raycastDistance);

        if(Physics.Raycast(startPos, -transform.forward, out rH, raycastDistance, LayerMask.GetMask("Default")))
        {
            if (Vector3.Dot(transform.forward, rH.normal) > 0.15f)
            {
                //Debug.DrawLine(pos, pos + (transform.forward * 0.12f), Color.green, 5);
                pos = rH.point + (rH.normal * 0.015f);
            }
            else
            {
                //No cell here
                allCells[i].disabled = true;
            }
        }
        else
        {
            //No cell here
            allCells[i].disabled = true;
        }

        if(allCells[i].disabled)
        {

        }

        //Cell's origin is the bottom left corner
        allCells[i].cornerPositions[0] = pos;// + (transform.up * halfCellSize) + (transform.right * halfCellSize);
        allCells[i].cornerPositions[1] = pos + (transform.right * cellSize);
        allCells[i].cornerPositions[2] = pos + (transform.up * cellSize) + (transform.right * cellSize);
        allCells[i].cornerPositions[3] = pos + (transform.up * cellSize);


        allCells[i].cornerDensities[0] = 0;
        allCells[i].cornerDensities[1] = 0;
        allCells[i].cornerDensities[2] = 0;
        allCells[i].cornerDensities[3] = 0;

        allCells[i].cornerIndices = new int[4];

        bool createAllFour = false;

        if (createAllFour)
        {
            for (int j = 0; j < 4; j++)
            {
                CellCorner newNode = new CellCorner();
                newNode.cornerPosition = allCells[i].cornerPositions[j];
                allNodes.Add(newNode);
            }
        }
        else
        {
            CellCorner bottomLeft = new CellCorner();
            bottomLeft.cornerPosition = allCells[i].cornerPositions[0];
            allNodes.Add(bottomLeft);
            int newNodeIndex = allNodes.Count-1;

            allCells[i].cornerIndices[0] = newNodeIndex;

            if(x>0)
            {
                allCells[i - 1].cornerIndices[1] = newNodeIndex;
            }

            if (y > 0)
            {
                allCells[i - (int)resolution].cornerIndices[3] = newNodeIndex;
            }

            if (y > 0 && x > 0)
            {
                allCells[(i - (int)resolution) - 1].cornerIndices[2] = newNodeIndex;
            }
        }
    }

    //Properly disables this grid
    public void Disable()
    {
        allCells = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Debug things
    /// </summary>
    void OnDrawGizmos()
    {
        //for (int i = 0; i < allCells.Length; i++)
        {
            //for (int j = 0; j < allCells[i].cornerPositions.Length; j++)
            {
                //if(allCells[i].cornerDensities[j]>=1)
                //    Gizmos.DrawSphere(allCells[i].cornerPositions[j], 0.1115f);
                //else
                //    Gizmos.DrawSphere(allCells[i].cornerPositions[j], 0.025f);

                ////UnityEditor.Handles.Label(allCells[i].cornerPositions[j], ""+ allCells[i].cornerDensities[j]);

                //if(i==0)
                //UnityEditor.Handles.Label(allCells[i].cornerPositions[j], "" + j);
            }
        }

        //if (gameObject.activeInHierarchy)
        //{
        //    if (allNodes.Count < 6)
        //    {
        //        for (int i = 0; i < allNodes.Count; i++)
        //        {
        //            UnityEditor.Handles.Label(allNodes[i].cornerPosition, "" + allNodes[i].density);
        //        }
        //    }
        //}

        //if (allMetaball2Ds != null)
        //{
        //    for (int i = 0; i < allMetaball2Ds.Count; i++)
        //    {
        //        Gizmos.DrawSphere(allMetaball2Ds[i].pos, 0.05f * allMetaball2Ds[i].radius);
        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            debug++;
        }

        //Used to detect movements and these variables are used later to move all cells
        movementSinceLastFrame += transform.position - prevPos;
        prevPos = transform.position;
    }

    /// <summary>
    /// This is where the mesh is reset and then rebuilt by looping through each cell
    /// </summary>
    void Triangulate()
    {
        if(vertices==null)
        {
            Disable();
            return;
        }

        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        for (int i = 0; i < allCells.Length; i++)
        {
            if (allCells[i].disabled)
            {

            }
            else
            {
                //int startPos = i * 4;
                TriangulateCell(ref allCells[i]);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
    }


    /// <summary>
    /// This interpolates a vertex position between two nodes based on their densities
    /// </summary>
    /// <param name="c"></param>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    /// <returns></returns>
    Vector3 Interpolate(Cell c, int index1, int index2)
    {
        Vector3 pos1 = c.cornerPositions[index1];
        Vector3 pos2 = c.cornerPositions[index2];

        float d1 = c.cornerDensities[index1];
        float d2 = c.cornerDensities[index2];

        return Interpolation(pos1, pos2, d1, d2);
    }

    Vector3 Interpolation(Vector3 v1, Vector3 v2, float i1, float i2)
    {
        Vector3 finalPoint;
        float length = Vector3.Distance(v1, v2);
        if (interpolation)
            finalPoint = v1 + Vector3.ClampMagnitude((1 - i1) * (v2 - v1) / (i2 - i1), length);
        else
            finalPoint = Vector3.Lerp(v1, v2, 0.5f);

        return finalPoint;
    }

    /// <summary>
    /// Sets info on the shader and then dispatches the Physics kernel of the Marching Squares shader
    /// </summary>
    void ComputeShaderPhysics()
    {
        myCS.SetInt("numBalls", allMetaball2Ds.Count);
        myCS.SetBuffer(myCS.FindKernel("UpdatePhysics"), "allMetaballs", metaballBuffer);
        myCS.SetVector("gravity", metaballGravity);
        myCS.SetFloat("deltaTime", Time.deltaTime);
        myCS.SetVector("movementSinceLastFrame", movementSinceLastFrame);

        myCS.Dispatch(myCS.FindKernel("UpdatePhysics"), allMetaball2Ds.Count, 1, 1);
    }

    /// <summary>
    /// Sends info to the GPU and dispatches the GetDensities kernel on the GPU
    /// </summary>
    void ApplyComputeShader()
    {
        if(cellCornerBuffer.count>1 && metaballBuffer.count>1)
        {
            myCS.SetInt("numBalls", allMetaball2Ds.Count);
            myCS.SetInt("gridWidth", sideLength);
            myCS.SetInt("gridHeight", sideLength);

            myCS.SetFloat("deltaTime", Time.deltaTime);

            myCS.SetVector("gravity", metaballGravity);

            
            myCS.SetVector("movementSinceLastFrame", movementSinceLastFrame);
            movementSinceLastFrame = Vector3.zero;

            myCS.SetBuffer(myCS.FindKernel("GetDensities"), "allCorners", cellCornerBuffer);
            myCS.SetBuffer(myCS.FindKernel("GetDensities"), "allMetaballs", metaballBuffer);

            myCS.Dispatch(myCS.FindKernel("GetDensities"), sideLength / 8, sideLength / 8, 1);
        }
    }

    /// <summary>
    /// Gets data back from the GPU. This is a seperate function to allow the GPU time to calculate this data before asking it to return it
    /// </summary>
    void ReturnDataFromGPU()
    {
        if (gpuNodeInfo == null || gpuNodeInfo.Length != cellCornerBuffer.count)
        {
            gpuNodeInfo = new CellCorner[cellCornerBuffer.count];
        }

        if (gpuMetaballInfo == null || gpuMetaballInfo.Length != metaballBuffer.count)
        {
            gpuMetaballInfo = new Metaball2D[metaballBuffer.count];
        }

        cellCornerBuffer.GetData(gpuNodeInfo);
        metaballBuffer.GetData(gpuMetaballInfo);

        for (int i = 0; i < allNodes.Count; i++)
        {
            allNodes[i] = gpuNodeInfo[i];
        }

        for (int i = 0; i < allMetaball2Ds.Count; i++)
        {
            allMetaball2Ds[i] = gpuMetaballInfo[i];
        }
    }

    /// <summary>
    /// This is the main Co-routine, which dispatches compute shaders and puts the final mesh together
    /// </summary>
    /// <returns></returns>
    IEnumerator HeavyCalculations()
    {
        float timeDelay = 0.025f;
        WaitForSeconds delay = new WaitForSeconds(timeDelay);

        while (true)
        {
            //Moves the grid + metaballs using the GPU                    
            ComputeShaderPhysics();

            if (animatedTime > 0)
            {

                if (marchingSquaresEnabled)
                {
                    //Gets the data from the Compute Shader which was dispatched in the previous tick
                    ReturnDataFromGPU();

                    //Calculates densities for every node of every cell using the GPU
                    ApplyComputeShader();

                    yield return new WaitForSeconds(timeDelay);                    
                }

                animatedTime -= Time.deltaTime;
            }
            else
            {
                mesh.Clear();
            }




            Triangulate();
            

            yield return delay;
        }
    }

    /// <summary>
    /// This is where the densities of a given cell's corner are considered and vertices are created accordingly
    /// </summary>
    /// <param name="c"></param>
    void TriangulateCell(ref Cell c)
    {

        int cellType = 0;

        //Cell c = new Cell();
        //c.cornerPositions = new Vector3[4];
        //c.cornerDensities = new float[4];

        for (int i = 0; i < 4; i++)
        {
            c.cornerPositions[i] = allNodes[c.cornerIndices[i]].cornerPosition;
            c.cornerDensities[i] = allNodes[c.cornerIndices[i]].density;
        }

        //c.cornerPositions[1] = two.cornerPosition;
        //c.cornerPositions[2] = three.cornerPosition;
        //c.cornerPositions[3] = four.cornerPosition;

        //c.cornerDensities[0] = one.density;
        //c.cornerDensities[1] = two.density;
        //c.cornerDensities[2] = three.density;
        //c.cornerDensities[3] = four.density;

        if (c.cornerDensities[0] >= 1)
            cellType |= 1;
        if (c.cornerDensities[1] >= 1)
            cellType |= 2;
        if (c.cornerDensities[2] >= 1)
            cellType |= 4;
        if (c.cornerDensities[3] >= 1)
            cellType |= 8;

        //if(Input.GetKeyDown(KeyCode.C))
        //    Debug.Log(cellType);

        //if(Input.GetKey(KeyCode.T))
        //cellType = debug;


        //Placing triangles
        switch (cellType)
        {
            case 0:
                return;

            //SINGLE CORNER CASES
            case 1:
                AddTriangle(c.cornerPositions[0], Interpolate(c, 0, 1), Interpolate(c, 0, 3));
                break;
            case 2:
                AddTriangle(c.cornerPositions[1], Interpolate(c, 1, 2), Interpolate(c, 0, 1));
                break;
            case 4:
                AddTriangle(c.cornerPositions[2], Interpolate(c, 2, 3), Interpolate(c, 2, 1));
                break;
            case 8:
                AddTriangle(Interpolate(c, 2, 3), c.cornerPositions[3], Interpolate(c, 0, 3));
                break;

            //CASES INVOLVING SQUARES
            case 3:
                AddQuad(c.cornerPositions[0], c.cornerPositions[1], Interpolate(c, 1, 2), Interpolate(c, 0, 3));
                break;
            case 6:
                AddQuad(c.cornerPositions[1], c.cornerPositions[2], Interpolate(c, 2, 3), Interpolate(c, 1, 0));
                break;
            case 9:
                AddQuad(c.cornerPositions[3], c.cornerPositions[0], Interpolate(c, 1, 0), Interpolate(c, 2, 3));
                break;
            case 12:
                AddQuad(c.cornerPositions[2], c.cornerPositions[3], Interpolate(c, 3, 0), Interpolate(c, 1, 2));
                break;
            case 15:
                AddQuad(c.cornerPositions[0], c.cornerPositions[1], c.cornerPositions[2], c.cornerPositions[3]);
                break;

            //PENTAGON STUFF
            case 7:
                AddQuad(c.cornerPositions[1], c.cornerPositions[2], Interpolate(c, 2, 3), Interpolate(c, 1, 0));
                AddTriangle(Interpolate(c, 0, 2), Interpolate(c, 2, 3), Interpolate(c, 0, 3));
                AddQuad(Interpolate(c, 0, 1), Interpolate(c, 0, 2), Interpolate(c, 0, 3), c.cornerPositions[0]);
                break;
            case 11:
                AddQuad(c.cornerPositions[0], Interpolate(c,0,1), Interpolate(c, 2, 3), c.cornerPositions[3]);
                AddTriangle(Interpolate(c, 1, 2), Interpolate(c, 2, 3), Interpolate(c, 0, 2));
                AddQuad(Interpolate(c, 1, 2), Interpolate(c, 0, 2), Interpolate(c, 0, 1), c.cornerPositions[1]);
                //AddPentagon(c.cornerPositions[1], c.cornerPositions[0], Interpolate(c, 0, 3), Interpolate(c, 2, 3), c.cornerPositions[3]);
                break;
            case 13:
                AddQuad(c.cornerPositions[2], c.cornerPositions[3], Interpolate(c,3,0), Interpolate(c,2,1));
                AddTriangle(Interpolate(c, 1, 2), Interpolate(c, 2, 0), Interpolate(c, 0, 1));
                AddQuad(Interpolate(c, 1, 0), Interpolate(c, 0, 2), Interpolate(c, 0, 3), c.cornerPositions[0]);
                //AddPentagon(c.cornerPositions[2], c.cornerPositions[3], Interpolate(c, 1, 2), Interpolate(c, 0, 1), c.cornerPositions[0]);
                break;
            case 14:
                AddQuad(c.cornerPositions[2], Interpolate(c,2,3), Interpolate(c,1,0), c.cornerPositions[1]);
                AddTriangle(Interpolate(c, 1, 0), Interpolate(c, 2, 0), Interpolate(c, 0, 3));
                AddQuad(Interpolate(c, 2, 3), c.cornerPositions[3], Interpolate(c, 0, 3), Interpolate(c, 0, 2));
                //AddPentagon(c.cornerPositions[3], c.cornerPositions[1], Interpolate(c, 0, 1), Interpolate(c, 0, 3), c.cornerPositions[2]);
                break;

            //EXTRAS
            case 5:
                AddTriangle(c.cornerPositions[0], Interpolate(c, 0, 1), Interpolate(c, 0, 3));
                AddTriangle(c.cornerPositions[2], Interpolate(c, 2, 3), Interpolate(c, 2, 1));
                break;

            case 10:
                AddTriangle(c.cornerPositions[1], Interpolate(c, 1, 2), Interpolate(c, 0, 1));
                AddTriangle(c.cornerPositions[3], Interpolate(c, 0, 3), Interpolate(c, 3, 2));
                break;
        }
    }

    /// <summary>
    /// Creates a triangle with the given positions
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int vertexIndex = vertices.Count;

        //Debug.DrawLine(a, b);
        //Debug.DrawLine(b, c);
        //Debug.DrawLine(c, a);

        a = transform.InverseTransformPoint(a);
        b = transform.InverseTransformPoint(b);
        c = transform.InverseTransformPoint(c);

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        //Debug.Break();
    }

    /// <summary>
    /// Creates a quad with the given positions
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int vertexIndex = vertices.Count;

        //Debug.DrawLine(a, b);
        //Debug.DrawLine(b, c);
        //Debug.DrawLine(c, a);
        //Debug.DrawLine(d, c);

        a = transform.InverseTransformPoint(a);
        b = transform.InverseTransformPoint(b);
        c = transform.InverseTransformPoint(c);
        d = transform.InverseTransformPoint(d);

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    /// <summary>
    /// Creates a pentagon with the given positions
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <param name="e"></param>
    void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        int vertexIndex = vertices.Count;

        //Debug.DrawLine(a, b);
        //Debug.DrawLine(b, c);
        //Debug.DrawLine(c, a);
        //Debug.DrawLine(d, c);
        //Debug.DrawLine(e, d);

        a = transform.InverseTransformPoint(a);
        b = transform.InverseTransformPoint(b);
        c = transform.InverseTransformPoint(c);
        d = transform.InverseTransformPoint(d);
        e = transform.InverseTransformPoint(e);

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        vertices.Add(e);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 3);
        triangles.Add(vertexIndex + 4);
    }
}
