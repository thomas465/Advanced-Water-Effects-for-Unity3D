using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MetaballManager : MonoBehaviour
{
    WaitForSeconds levelOfDetailTickRate = new WaitForSeconds(2.0f);

    //Mesh/Shader stuff
    int shaderKernel = 0;
    MeshRenderer myRenderer;
    Mesh myMesh;

    //Movement
    Vector3 movementSinceLastTick = Vector3.zero;

    //ComputeBuffer cellPositions, metaballPositions, metaballPowers;
    
    public ComputeShader myComputeShader;
    ComputeBuffer cellBuffer;
    ComputeBuffer cornerBuffer;
    ComputeBuffer metaballBuffer;

    ComputeBuffer vertexBuffer;
    ComputeBuffer triangleBuffer;

    Shader myShader;
    
    int[] edgeTable;
    int[,] triTable;
    //int[,] edgeConnections;

    [HideInInspector]
    List<Vector3> vertList;
    [HideInInspector]
    List<Vector2> uvList;

    [HideInInspector]
    Vector3[] newVerts;
    [HideInInspector]
    int[] newTris;

    [HideInInspector]
    List<int> triangleList;

    /// <summary>
    /// Class used to maintain info like metaball life and radius
    /// </summary>
    [System.Serializable]
    [RequireComponent(typeof(BoxCollider))]
    public class Metaball
    {
        public Vector3 pos;
        public float radius, weightScalar, life;

        public Metaball(Vector3 _startPos, float _radius, Vector3 _velocity, float _life = 9, float _weightScalar = 1)
        {
            pos = _startPos;
            radius = _radius;
            weightScalar = _weightScalar;
            life = _life;
        }
    }

    /// <summary>
    /// Struct to put metaball info in to send to the GPU
    /// </summary>
    [System.Serializable]
    public struct GPUMetaball
    {
        public Vector3 pos;
        public float radius;
    }

    /// <summary>
    /// Struct to put Cell info in to send to the GPU
    /// </summary>
    [System.Serializable]
    public struct GPUCell
    {
        public GPUCorner[] positions;
    }

    [HideInInspector]
    public List<Metaball> allMetaballs;

    public int resolution = 3;
    int[] cellsPerSide;

    int numberOfNodes;

    Vector3 prevPos;
    Quaternion prevRot;

    /// <summary>
    /// Struct to put Corner info in to send to the GPU
    /// </summary>
    [System.Serializable]
    public struct GPUCorner
    {
        public Vector3 pos;
        public float currentIntensity;
    }

    /// <summary>
    /// Class used for corners of cells 
    /// </summary>
    [System.Serializable]
    public class CellCorner
    {
        public Vector3 pos;
        public float currentIntensity;
        public bool active;

        public CellCorner(Vector3 _pos)
        {
            pos = _pos;
            //Debug.DrawLine(pos, pos + Vector3.up * 0.1f, Color.cyan);
        }

        //This was where density was previously calculated - this was much slower than the compute shader
        //public float CalculateIntensity(List<Metaball> metaballs)
        //{
        //    //float fxy = 0.0f;
        //    currentIntensity = 0;

        //    for (int i = 0; i < metaballs.Count; i++)
        //    {
        //        Metaball m = metaballs[i];

        //        float radiusSq = m.radius * m.radius;
        //        float otherbit = (Vector3.Scale((pos - m.pos), (pos - m.pos))).magnitude;

        //        currentIntensity += radiusSq / otherbit;
        //    }

        //    active = currentIntensity >= 1;

        //    if (active)
        //    {
        //        //Debug.DrawLine(pos, pos + Vector3.up * 0.1f, Color.cyan, 0.01f);
        //    }

        //    return currentIntensity;
        //}
    }

    [System.Serializable]
    public class Cell
    {
        public Vector3 pos, size;
        public CellCorner[] myCorners;

        public Cell(Vector3 _pos, Vector3 _size)
        {
            pos = _pos;
            size = _size;

            //Debug.DrawLine(pos-size/2, pos + size/2, Color.white, 0.1f);

            Vector3 reverseSize = size;
            reverseSize.y = -reverseSize.y;
            //Debug.DrawLine(pos - reverseSize / 2, pos + reverseSize / 2, Color.white, 0.05f);

            myCorners = new CellCorner[8];
        }

        public CellCorner CreateBottomLeftNode()
        {
            //This creates a node in the bottom left corner of this one
            //Debug.DrawLine(pos - size / 2, pos - size / 2 + Vector3.up * 0.1f, Color.red, 0.1f);
            return new CellCorner(pos - size / 2);
        }

        //public CellCorner CreateTopRightNode()
        //{
        //    //This creates a node in the top right corner of this one
        //    //Debug.DrawLine(pos + size / 2, pos + size / 2 + Vector3.up * 0.1f, Color.green, 0.1f);
        //    return new CellCorner(pos + size / 2);
        //}

        public void AssignNodes(CellCorner _n1, CellCorner _n2, CellCorner _n3, CellCorner _n4)
        {
            myCorners = new CellCorner[4];
            myCorners[0] = _n1;
            myCorners[1] = _n2;
            myCorners[2] = _n3;
            myCorners[3] = _n4;
        }

        public void AssignNodes(CellCorner[] _nodes)
        {
            myCorners = _nodes;
        }

        public void AssignEdges()
        {

        }
    }

    List<Cell> allCells;
    List<CellCorner> allCellCorners;
    float cellVolume = 0;

    [HideInInspector]
    GPUCell[] gpuCells;
    [HideInInspector]
    GPUCorner[] gpuCorners;
    [HideInInspector]
    GPUMetaball[] gpuMetaballs;

    BoxCollider metaballContainer;
    Vector3 prevContainerSize;
    float prevRes;

    [Header("Settings")]
    public bool calculateNormals = true;
    [Header("VERY SLOW")]
    public bool calculateUVs = false;

    [Header("Level of Detail Settings")]
    Transform mainCamera;
    float tickRate = 0.03f;
    [Range(0,1)]
    public float priority = 1;
    public float currentDetailLevel = 1;
    public float halfDetailDistance = 45;
    public float hideDistance = 125;

    //Variables to prevent garbage collection
    Vector3[] cutEdgePoints;

    // Use this for initialization
    void Awake()
    {
        if(!GetComponent<BoxCollider>())
        {
            Debug.LogError("Metaball manager " + gameObject.name + " has no BoxCollider to use as a frame! Put a box collider on " + gameObject.name + " and size it as if it is the bounds for your desired 3D effect.");
            Debug.Break();
            return;
        }
        else
        {
            if (!GetComponent<BoxCollider>().isTrigger)
            {
                Debug.LogWarning("Collider on " + gameObject.name + " has been turned into a trigger; MetaballManager box colliders should always be trigger colliders.");
                GetComponent<BoxCollider>().isTrigger = true;
            }
        }

        allMetaballs = new List<Metaball>();

        prevPos = transform.position;
        prevRot = transform.rotation;

        RefreshGrid();
    }

    /// <summary>
    /// Initialises lists and buffers and starts some coroutines
    /// </summary>
    void Start()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;

        FillTriTable();
        FillEdgeTable();
        //SetUpEdgeConnections();

        myMesh = new Mesh();
        myMesh.name = "Marching Cubes Mesh";
        GetComponent<MeshFilter>().mesh = myMesh;

        triangleList = new List<int>();
        vertList = new List<Vector3>();
        uvList = new List<Vector2>();

        int sizeOfCornerStruct = (sizeof(float) * 3) + sizeof(float);

        cellBuffer = new ComputeBuffer(allCells.Count, sizeOfCornerStruct*8);
        cornerBuffer = new ComputeBuffer(allCellCorners.Count, sizeOfCornerStruct);
        metaballBuffer = new ComputeBuffer(2000, (sizeof(float) * 3) + sizeof(float));

        gpuMetaballs = new GPUMetaball[0];

        vertexBuffer = new ComputeBuffer(cellsPerSide[0] * cellsPerSide[1] * cellsPerSide[2], (sizeof(float) * 3));
        triangleBuffer = new ComputeBuffer(vertexBuffer.count, sizeof(int));

        cutEdgePoints = new Vector3[12];

        StartCoroutine("LevelOfDetail");
        StartCoroutine("MarchingCubesTick");
    }

    //Changes the size of metaballs based on the resolution of the grid
    public float GetDesiredMetaballSize()
    {
        return cellVolume * 7.5f;
        //return cellVolume * 0.0001f;
    }

    void OnDrawGizmos()
    {
        //for(int i=0; i<allNodes.Count; i++)
        //{
        //    if(allNodes[i].currentIntensity>0.1f)
        //    Gizmos.DrawCube(allNodes[i].pos, Vector3.one * allNodes[i].currentIntensity * 0.1f);
        //}
    }

    /// <summary>
    /// Creates a 3D grid using the attached Box Collider component as the size reference.
    /// </summary>
    void CreateAllCells()
    {
        int i = 0;

        float movementLengthX = metaballContainer.size.x / cellsPerSide[0];
        float movementLengthY = metaballContainer.size.y / cellsPerSide[1];
        float movementLengthZ = metaballContainer.size.z / cellsPerSide[2];

        cellVolume = movementLengthX * movementLengthY * movementLengthZ;

        Vector3 newCellPos = transform.position + metaballContainer.center - metaballContainer.bounds.extents;

        newCellPos += new Vector3(movementLengthX, movementLengthY, movementLengthZ) / 2;

        Vector3 startCellPos = newCellPos;

        //This section sets up the grid in a way which means there are the minimum amount of cell corners
        //All cell corners are shared between cells rather than each cell creating 8 corners each.
        for (int z = 0; z < cellsPerSide[2]; z++)
        {
            for (int y = 0; y < cellsPerSide[1]; y++)
            {
                for (int x = 0; x < cellsPerSide[0]; x++)
                {
                    //if (i < allCells.Length)
                    {
                        allCells.Add(new Cell(newCellPos, new Vector3(movementLengthX, movementLengthY, movementLengthZ)));

                        CellCorner newNode = allCells[i].CreateBottomLeftNode();
                        allCellCorners.Add(newNode);

                        /*
                      7 4___5 6
                        |   |
                        |3__|2 
                        0   1
                        */

                        //0
                        allCells[i].myCorners[0] = newNode;

                        //Talking to the cell left of me
                        if(x>0)
                        {
                            //1
                            allCells[i - 1].myCorners[1] = newNode;
                        }

                        //Talking to cells lower than me
                        if (y > 0)
                        {
                            //4
                            allCells[i - cellsPerSide[0]].myCorners[4] = newNode;

                            //5
                            if (x > 0)
                                allCells[i - (cellsPerSide[0] + 1)].myCorners[5] = newNode;
                        }

                        //Talking to cells behind me
                        if (z > 0)
                        {
                            int behindOffset = (cellsPerSide[1] * cellsPerSide[0]);

                            //3
                            allCells[i - behindOffset].myCorners[3] = newNode;
                            //Debug.Log("Current Cell: " + i + "/ Cell behind me?: " + (i - behindOffset));

                            //This talks to the one left of the one behind me
                            //2
                            if (x > 0)
                            {
                                allCells[i - (behindOffset + 1)].myCorners[2] = newNode;

                                //6
                                if (y > 0)
                                    allCells[i - (behindOffset + cellsPerSide[0] + 1) ].myCorners[6] = newNode;
                            }

                            //7
                            if (y > 0)
                                allCells[i - (behindOffset + cellsPerSide[0])].myCorners[7] = newNode;
                        }
                    }

                    newCellPos += Vector3.right * movementLengthX;

                    if (x >= cellsPerSide[0] - 1)
                    {
                        //allCells[i].CreateTopRightNode();
                    }

                    i++;
                }

                newCellPos.x = startCellPos.x;
                newCellPos.y += movementLengthY;
            }

            newCellPos.y = startCellPos.y;
            newCellPos.z += movementLengthZ;
        }

        //This was to debug cells and make sure they all had 8 corners to work with
        //foreach (Cell c in allCells)
        //{
        //    for (int b = 0; b < c.myCorners.Length; b++)
        //    {
        //        if (c.myCorners[b] == null)
        //        {
        //            //Debug.Log("This one " + (b) + " has no node " + b);
        //        }
        //    }
        //}

        AssignAllNodes();
    }

    void AssignAllNodes()
    {
        //This creates nodes to fill in null spaces
        for(int i = 0; i<allCells.Count; i++)
        {
            for(int c = 0; c<allCells[i].myCorners.Length; c++)
            {
                if(allCells[i].myCorners[c]==null)
                {
                    allCells[i].myCorners[c] = new CellCorner(allCells[i].pos);
                }
            }
        }
    }

    void RefreshGrid()
    {
        allCellCorners = new List<CellCorner>();
        allCells = new List<Cell>();

        metaballContainer = GetComponent<BoxCollider>();
        prevContainerSize = metaballContainer.size;
        prevRes = resolution;

        cellsPerSide = new int[3];
        cellsPerSide[0] = (int)metaballContainer.size.x * resolution;
        cellsPerSide[1] = (int)metaballContainer.size.y * resolution;
        cellsPerSide[2] = (int)metaballContainer.size.z * resolution;

        CreateAllCells();

        gpuCorners = new GPUCorner[allCellCorners.Count];
        
        for(int i=0; i<gpuCorners.Length; i++)
        {
            gpuCorners[i].pos = allCellCorners[i].pos;
            gpuCorners[i].currentIntensity = allCellCorners[i].currentIntensity;
        }

        gpuCells = new GPUCell[allCells.Count];

        for(int i=0; i<gpuCells.Length; i++)
        {
            gpuCells[i].positions = new GPUCorner[8];

            for(int c = 0; c<8; c++)
            {
                int index = (i * 8) + c;

                if (index < gpuCorners.Length)
                {
                    gpuCells[i].positions[c] = gpuCorners[index];
                }
                else
                {
                    gpuCells[i].positions[c] = gpuCells[i].positions[0];
                }
            }
        }

        //for (int i=0; i< allMetaballs.Count; i++)
        {
            //allMetaballs[i] = new Metaball(new Vector3(Random.Range(-cellsPerSide[0], cellsPerSide[0]), Random.Range(-cellsPerSide[1], cellsPerSide[1]), Random.Range(-cellsPerSide[2], cellsPerSide[2])), 1.2f);
        }

        //cellPositions = new ComputeBuffer(255, 25);
        //metaballPositions = new ComputeBuffer(255, 25);
        //metaballPowers = new ComputeBuffer(allMetaballs, 25);
    }

    /// <summary>
    /// Moves all cells along with the manager
    /// </summary>
    /// <param name="movement"></param>
    void MoveAllThings(Vector3 movement)
    {
        for (int i = 0; i < allCells.Count; i++)
        {
            allCells[i].pos += movement;
        }

        for (int j = 0; j < allCellCorners.Count; j++)
        {
            allCellCorners[j].pos += movement;
            gpuCorners[j].pos += movement;
        }
    }

    /// <summary>
    /// This is the coroutine which handles all large calculations by dispatching compute shaders,
    /// clearing the mesh and putting a new mesh together. It is given a variable WaitForSeconds value as a form
    /// of "Level of Detail" - distant effects have a larger delay.
    /// </summary>
    /// <returns></returns>
    IEnumerator MarchingCubesTick()
    {
        while (true)
        {
            //Skips all of these calculations if the level of detail is low enough
            while (currentDetailLevel <= 0)
            {
                myMesh.Clear();
                break;
            }

            //Skips all of these calculations if there are no metaballs
            while(allMetaballs.Count==0)
            {
                myMesh.Clear();
                break;
            }

            //Pauses Unity if the frame rate is unreasonably low
            if (Time.deltaTime > 0.25f && Time.timeSinceLevelLoad > 1)
            {
                Debug.Break();
            }

            //Keeps the manager's rotation the same no matter what happens in the scene
            transform.rotation = Quaternion.LookRotation(Vector3.forward);

            //Changes the grid to suit the size of the box
            if (metaballContainer.size != prevContainerSize || resolution != prevRes)
            {
                RefreshGrid();
            }

            //Dispatches a Compute Shader kernel which will return data in the next tick
            GetDensitiesFromGPU();

            //Clear the mesh from the previous tick
            myMesh.Clear();

            for (int i = 0; i < allCells.Count; i++)
            {
                //TriangulateGPUCell(gpuCorners[i], gpuCorners[i + 1], gpuCorners[i + 2], gpuCorners[i + 3], gpuCorners[i + 4], gpuCorners[i + 5], gpuCorners[i + 6], gpuCorners[i + 7]);
                TriangulateCell(allCells[i].myCorners);
                //TriangulateCell(newCorners);
            }

            //Moves the cells with the Manager's gameobject
            if (movementSinceLastTick != Vector3.zero)
            {
                Debug.Log(gameObject.name + "'s movement since LF: " + movementSinceLastTick);
                Debug.DrawLine(transform.position, transform.position - movementSinceLastTick.normalized, Color.red, 0.1f);
                MoveAllThings(movementSinceLastTick);
                movementSinceLastTick = Vector3.zero;
            }

            //Applies the new information to the Mesh component and calculates normals
            if (vertList.Count == triangleList.Count)
            {
                myMesh.vertices = vertList.ToArray();
                myMesh.triangles = triangleList.ToArray();

                if (calculateNormals)
                {
                    myMesh.RecalculateNormals();
                }
            }

            //This works well but is very slow
            if (calculateUVs && myMesh.vertexCount>3)
            {
                uvList.Clear();
                //uvList.AddRange(UnityEditor.Unwrapping.GeneratePerTriangleUV(myMesh));
                myMesh.SetUVs(0, uvList);
            }

            vertList.Clear();
            triangleList.Clear();
            
            yield return new WaitForSeconds(tickRate);
            //Returns data from the Compute Shader, dispatched last tick
            ReturnDataFromGPUBuffer();

        }
    }

    /// <summary>
    /// Syncs the given CPU corner's info with the GPU info
    /// </summary>
    /// <param name="corner"></param>
    /// <param name="node"></param>
    void AssignGPUCornerInfo(ref GPUCorner corner, ref CellCorner node)
    {
        corner.currentIntensity = node.currentIntensity;
        corner.pos = node.pos;
    }

    // Update is called once per frame
    void LateUpdate()
    {

    }

    /// <summary>
    /// Sends information to the GPU and dispatches the kernel which calculates corner densities.
    /// The calculated data is returned in a different function at a later time.
    /// </summary>
    void GetDensitiesFromGPU()
    {
        //Setting GPU array values to match the Unity scene
        if (gpuMetaballs.Length != allMetaballs.Count)
        {
            gpuMetaballs = new GPUMetaball[allMetaballs.Count];
        }

        for (int i = 0; i < allMetaballs.Count; i++)
        {
            gpuMetaballs[i].pos = allMetaballs[i].pos;
            gpuMetaballs[i].radius = allMetaballs[i].radius;
        }

        //Setting up info in the compute shader to match the Unity scene
        cornerBuffer.SetData(gpuCorners);
        metaballBuffer.SetData(gpuMetaballs);

        myComputeShader.SetInt("numBalls", allMetaballs.Count);
        myComputeShader.SetInt("numCorners", allCellCorners.Count);

        myComputeShader.SetInt("gridWidth", cellsPerSide[0]);
        myComputeShader.SetInt("gridHeight", cellsPerSide[1]);
        myComputeShader.SetInt("gridDepth", cellsPerSide[2]);

        myComputeShader.SetBuffer(myComputeShader.FindKernel("GetDensities"), "allCorners", cornerBuffer);
        myComputeShader.SetBuffer(myComputeShader.FindKernel("GetDensities"), "allMetaballs", metaballBuffer);

        //Calculation happens here
        myComputeShader.Dispatch(myComputeShader.FindKernel("GetDensities"), cellsPerSide[0] / 4, cellsPerSide[1] / 4, cellsPerSide[2] / 4);
    }

    /// <summary>
    /// Gets the calculated corner densities back from the GPU.
    /// </summary>
    void ReturnDataFromGPUBuffer()
    {
        //Returns the info to the "real world"
        cornerBuffer.GetData(gpuCorners);

        for (int i = 0; i < gpuCorners.Length; i++)
        {
            allCellCorners[i].currentIntensity = gpuCorners[i].currentIntensity;
            allCellCorners[i].active = allCellCorners[i].currentIntensity >= 1;
        }
    }

    /// <summary>
    /// Cancelled functionality to include other Marching Cubes calculations in the Compute Shader
    /// </summary>
    void GetVerticesFromGPU()
    {
        //myComputeShader.Dispatch(myComputeShader.FindKernel("GetVertices"), Cell)
    }

    void FixedUpdate()
    {
        //Stores values used to keep cells aligned with the MetaballManager if it moves at runtime
        movementSinceLastTick += (transform.position - prevPos);
        prevPos = transform.position;
    }

    /// <summary>
    /// Stops co-routines and releases buffers.
    /// </summary>
    void OnDestroy()
    {
        StopAllCoroutines();

        if (cornerBuffer != null)
            cornerBuffer.Release();
        if (metaballBuffer != null)
            metaballBuffer.Release();

        if (vertexBuffer != null)
            vertexBuffer.Release();
        if (cellBuffer != null)
            cellBuffer.Release();

        if (triangleBuffer != null)
            triangleBuffer.Release();
        

    }

    //This is from before metaballs were physical PhysX objects
    //void PositionMetaballs()
    //{
    //    float gravity = -9.14f;

    //    for (int i = 0; i < allMetaballs.Count; i++)
    //    {
    //        Metaball m = allMetaballs[i];

    //        m.velocity += -Vector3.up * gravity;

    //        m.pos += m.velocity * Time.deltaTime;
    //    }
    //}

        /// <summary>
        /// Version of triangulation which will use a compute shader - CANCELLED
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <param name="three"></param>
        /// <param name="four"></param>
        /// <param name="five"></param>
        /// <param name="six"></param>
        /// <param name="seven"></param>
        /// <param name="eight"></param>
    //void TriangulateGPUCell(GPUCorner one, GPUCorner two, GPUCorner three, GPUCorner four, GPUCorner five, GPUCorner six, GPUCorner seven, GPUCorner eight)
    //{
    //    int cellType = 0;

    //    //Setting cellType
    //    if (one.currentIntensity > 1)
    //        cellType |= 1;
    //    if (two.currentIntensity > 1)
    //        cellType |= 2;
    //    if (three.currentIntensity > 1)
    //        cellType |= 4;
    //    if (four.currentIntensity > 1)
    //        cellType |= 8;
    //    if (five.currentIntensity > 1)
    //        cellType |= 16;
    //    if (six.currentIntensity > 1)
    //        cellType |= 32;
    //    if (seven.currentIntensity > 1)
    //        cellType |= 64;
    //    if (eight.currentIntensity > 1)
    //        cellType |= 128;

    //    int edgeFlags = edgeTable[cellType];

    //    if (edgeFlags == 0)
    //    {
    //        //Entirely inside or outside the surface
    //        return;
    //    }

    //    if ((edgeTable[cellType] & 1) == 1)
    //    {
    //        cutEdgePoints[0] = Interpolation(one.pos, two.pos, one.currentIntensity, two.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 2) == 2)
    //    {
    //        cutEdgePoints[1] = Interpolation(two.pos, three.pos, two.currentIntensity, three.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 4) == 4)
    //    {
    //        cutEdgePoints[2] = Interpolation(three.pos, four.pos, three.currentIntensity, four.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 8) == 8)
    //    {
    //        cutEdgePoints[3] = Interpolation(four.pos, one.pos, four.currentIntensity, one.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 16) == 16)
    //    {
    //        cutEdgePoints[4] = Interpolation(five.pos, six.pos, five.currentIntensity, six.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 32) == 32)
    //    {
    //        cutEdgePoints[5] = Interpolation(six.pos, seven.pos, six.currentIntensity, seven.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 64) == 64)
    //    {
    //        cutEdgePoints[6] = Interpolation(seven.pos, eight.pos, seven.currentIntensity, eight.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 128) == 128)
    //    {
    //        cutEdgePoints[7] = Interpolation(eight.pos, five.pos, eight.currentIntensity, five.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 256) == 256)
    //    {
    //        cutEdgePoints[8] = Interpolation(one.pos, five.pos, one.currentIntensity, five.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 512) == 512)
    //    {
    //        cutEdgePoints[9] = Interpolation(two.pos, six.pos, two.currentIntensity, six.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 1024) == 1024)
    //    {
    //        cutEdgePoints[10] = Interpolation(three.pos, seven.pos, three.currentIntensity, seven.currentIntensity);
    //    }
    //    if ((edgeTable[cellType] & 2048) == 2048)
    //    {
    //        cutEdgePoints[11] = Interpolation(four.pos, eight.pos, four.currentIntensity, eight.currentIntensity);
    //    }


    //    //Triangulation
    //    cellBuffer.SetData(gpuCells);
    //    myComputeShader.SetBuffer(myComputeShader.FindKernel("GetVertices"), "allCells", cellBuffer);
    //    myComputeShader.SetBuffer(myComputeShader.FindKernel("GetVertices"), "vertices", vertexBuffer);
    //    myComputeShader.SetBuffer(myComputeShader.FindKernel("GetVertices"), "triangles", triangleBuffer);

    //    //myComputeShader.Dispatch(myComputeShader.FindKernel("GetVertices"), cellsPerSide[0] / 8, cellsPerSide[1] / 8, cellsPerSide[2] / 8);
    //    //Debug.Log(triangleBuffer.count);
    //    //vertexBuffer.GetData(newVerts);
    //    //triangleBuffer.GetData(newTris);

    //    //Create the triangles for this cell
    //    for (int i = 0; triTable[cellType, i] != -1; i += 3)
    //    {
    //        CreateTriangle(
    //            cutEdgePoints[triTable[cellType, i]],
    //            cutEdgePoints[triTable[cellType, i + 1]],
    //            cutEdgePoints[triTable[cellType, i + 2]]
    //            );
    //    }
    //}

    /// <summary>
    /// Standard version of triangulation. Uses bitwise operations to store which sort of mesh should be created in the given cell,
    /// and then interpolates between edges to create a mesh in that cell.
    /// </summary>
    /// <param name="nodes"></param>
    void TriangulateCell(CellCorner[] nodes)
    {
        int cellType = 0;

        //Setting cellType
        if (nodes[0].active)
            cellType |= 1;
        if (nodes[1].active)
            cellType |= 2;
        if (nodes[2].active)
            cellType |= 4;
        if (nodes[3].active)
            cellType |= 8;
        if (nodes[4].active)
            cellType |= 16;
        if (nodes[5].active)
            cellType |= 32;
        if (nodes[6].active)
            cellType |= 64;
        if (nodes[7].active)
            cellType |= 128;

        int edgeFlags = edgeTable[cellType];

        if (edgeFlags == 0)
        {
            //Entirely inside or outside the surface
            return;
        }

        if ((edgeTable[cellType] & 1) == 1)
        {
            cutEdgePoints[0] = Interpolation(nodes[0].pos, nodes[1].pos, nodes[0].currentIntensity, nodes[1].currentIntensity);
        }
        if ((edgeTable[cellType] & 2) == 2)
        {
            cutEdgePoints[1] = Interpolation(nodes[1].pos, nodes[2].pos, nodes[1].currentIntensity, nodes[2].currentIntensity);
        }
        if ((edgeTable[cellType] & 4) == 4)
        {
            cutEdgePoints[2] = Interpolation(nodes[2].pos, nodes[3].pos, nodes[2].currentIntensity, nodes[3].currentIntensity);
        }
        if ((edgeTable[cellType] & 8) == 8)
        {
            cutEdgePoints[3] = Interpolation(nodes[3].pos, nodes[0].pos, nodes[3].currentIntensity, nodes[0].currentIntensity);
        }
        if ((edgeTable[cellType] & 16) == 16)
        {
            cutEdgePoints[4] = Interpolation(nodes[4].pos, nodes[5].pos, nodes[4].currentIntensity, nodes[5].currentIntensity);
        }
        if ((edgeTable[cellType] & 32) == 32)
        {
            cutEdgePoints[5] = Interpolation(nodes[5].pos, nodes[6].pos, nodes[5].currentIntensity, nodes[6].currentIntensity);
        }
        if ((edgeTable[cellType] & 64) == 64)
        {
            cutEdgePoints[6] = Interpolation(nodes[6].pos, nodes[7].pos, nodes[6].currentIntensity, nodes[7].currentIntensity);
        }
        if ((edgeTable[cellType] & 128) == 128)
        {
            cutEdgePoints[7] = Interpolation(nodes[7].pos, nodes[4].pos, nodes[7].currentIntensity, nodes[4].currentIntensity);
        }
        if ((edgeTable[cellType] & 256) == 256)
        {
            cutEdgePoints[8] = Interpolation(nodes[0].pos, nodes[4].pos, nodes[0].currentIntensity, nodes[4].currentIntensity);
        }
        if ((edgeTable[cellType] & 512) == 512)
        {
            cutEdgePoints[9] = Interpolation(nodes[1].pos, nodes[5].pos, nodes[1].currentIntensity, nodes[5].currentIntensity);
        }
        if ((edgeTable[cellType] & 1024) == 1024)
        {
            cutEdgePoints[10] = Interpolation(nodes[2].pos, nodes[6].pos, nodes[2].currentIntensity, nodes[6].currentIntensity);
        }
        if ((edgeTable[cellType] & 2048) == 2048)
        {
            cutEdgePoints[11] = Interpolation(nodes[3].pos, nodes[7].pos, nodes[3].currentIntensity, nodes[7].currentIntensity);
        }

        //Create the triangles for this cell
        for (int i = 0; triTable[cellType, i] != -1; i += 3)
        {
            CreateTriangle(
                cutEdgePoints[triTable[cellType, i]],
                cutEdgePoints[triTable[cellType, i + 1]],
                cutEdgePoints[triTable[cellType, i + 2]]
                );
        }
    }

    /// <summary>
    /// Interpolates between two cell corners based on their densities.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="i1"></param>
    /// <param name="i2"></param>
    /// <returns></returns>
    Vector3 Interpolation(Vector3 v1, Vector3 v2, float i1, float i2)
    {
        Vector3 finalPoint;

        finalPoint = v1 + (1 - i1) * (v2 - v1) / (i2 - i1);

        finalPoint -= transform.position;

        //if (finalPoint == Vector3.zero)
        //finalPoint = v1;

        return finalPoint;
    }

    /// <summary>
    /// Calculates level of detail based on distance from the main camera
    /// </summary>
    /// <returns></returns>
    IEnumerator LevelOfDetail()
    {
        float distance, scaledDistance;

        while (true)
        {
            distance = Vector3.Distance(transform.position, mainCamera.position);
            scaledDistance = distance / hideDistance;

            currentDetailLevel = Mathf.Lerp(1, 0, scaledDistance) * priority;

            tickRate = Mathf.Lerp(0.015f, 0.2f, scaledDistance * 2);

            yield return levelOfDetailTickRate;
        }
    }

    /// <summary>
    /// Creates a triangle using three Vector3 positions
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    void CreateTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int index = vertList.Count;

        vertList.Add(v1);
        vertList.Add(v2);
        vertList.Add(v3);

        triangleList.Add(index);
        triangleList.Add(index+1);
        triangleList.Add(index+2);
    }

    /// <summary>
    /// Creates the edge table to be used in triangulation
    /// </summary>
    void FillEdgeTable()
    {
        edgeTable = new int[256]{
0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0 };
    }

    /// <summary>
    /// Creates the tri table to be used in triangulation
    /// </summary>
    void FillTriTable()
    {
        triTable = new int[256, 16]
{{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
{3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
{1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
{4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
{9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
{2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
{10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
{5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
{5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
{8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
{2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
{7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
{11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
{5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
{11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
{11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
{9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
{6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
{6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
{6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
{8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
{7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
{3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
{5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
{0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
{9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
{8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
{5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
{0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
{6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
{10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
{10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
{0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
{3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
{6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
{9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
{8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
{3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
{6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
{10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
{10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
{7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
{7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
{2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
{1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
{11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
{8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
{0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
{7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
{7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
{1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
{10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
{0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
{7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
{6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
{9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
{6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
{4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
{10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
{8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
{1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
{8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
{10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
{4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
{10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
{9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
{6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
{7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
{3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
{7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
{3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
{6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
{9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
{1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
{4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
{7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
{6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
{0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
{6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
{0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
{11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
{6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
{5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
{9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
{1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
{1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
{10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
{0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
{5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
{10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
{11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
{9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
{7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
{2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
{9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
{9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
{1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
{9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
{9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
{0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
{10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
{2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
{0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
{0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
{9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
{5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
{3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
{5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
{0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
{9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
{1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
{3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
{4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
{9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
{11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
{11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
{2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
{9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
{3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
{1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
{4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
{4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
{0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
{3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
{3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
{0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
{9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
{1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}};
    }

    //void SetUpEdgeConnections()
    //{
    //    edgeConnections = new int[12, 2] {
    //    { 0,1}, { 1,2}, { 2,3}, { 3,0},
    //    { 4,5}, { 5,6}, { 6,7}, { 7,4},
    //    { 0,4}, { 1,5}, { 2,6}, { 3,7} };
    //}
}
