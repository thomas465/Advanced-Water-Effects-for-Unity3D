using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingSquaresManager : MonoBehaviour {

    [System.Serializable]
    class Node
    {
        public float intensity;
        public Vector3 pos;
    }

    [System.Serializable]
    struct Cell
    {
        public Node[] myNodes;
        //public int nodes;
    }

    [SerializeField]
    List<Cell> allCells;
    List<Node> allNodes;

    public ComputeShader marchingSquaresShader;
    ComputeBuffer cellBuffer;

    //Const
    const int nodeSize = sizeof(float) + (sizeof(float) * 3);
    const int cellSize = (nodeSize * 4) + (sizeof(int));

    //Arrays
    Cell[] cellArray = new Cell[0];

    //Mesh stuff
    List<Vector3> newVertices;
    List<int> newTris;
    Mesh myMesh;

    // Use this for initialization
    void Start () {
        allNodes = new List<Node>();
        allCells = new List<Cell>();

        cellBuffer = new ComputeBuffer(1, cellSize);

        //MeshRenderer r = GetComponent<MeshRenderer>();
        MeshFilter mF = GetComponent<MeshFilter>();

        myMesh = new Mesh();
        myMesh.name = "Animated Decal";
        mF.mesh = myMesh;

        newVertices = new List<Vector3>();
        newTris = new List<int>();

        StartCoroutine("HeavyCalculations");
    }

    void OnDestroy()
    {
        StopAllCoroutines();

        if (cellBuffer != null)
        {
            cellBuffer.Release();
        }
    }
	
	// Update is called once per frame
	void Update () {

    }

    Node CreateNode(Vector3 pos, Vector3 dir)
    {
        RaycastHit rH;
        Node newNode = new Node();

        Debug.DrawLine(pos, pos + dir * 15, Color.red, 4);

        if (Physics.Raycast(pos, dir, out rH, 15, LayerMask.GetMask("Default")))
        {
            if (!IsExistingNode(rH.point, out newNode))
            {
                newNode.pos = pos;
                allNodes.Add(newNode);
            }
            else
            {
                //Debug.Log("No need!");
            }
        }
        else
        {
            newNode.pos = pos;
        }

        return newNode;
    }

    public void CreateCell(Vector3 pos, Vector3 right, Vector3 up, Vector3 forward)
    {
        if (!IsExistingCell(pos))
        {
            Cell c = new Cell();
            c.myNodes = new Node[4];

            c.myNodes[0] = CreateNode(pos, forward);
            c.myNodes[1] = CreateNode(pos + right, forward);
            c.myNodes[2] = CreateNode(pos - up, forward);
            c.myNodes[3] = CreateNode(pos + right - up, forward);

            Debug.DrawLine(c.myNodes[1].pos, c.myNodes[3].pos, Color.gray, 2);
            Debug.DrawLine(c.myNodes[1].pos, c.myNodes[2].pos, Color.red, 2);

            c.myNodes[0].intensity = 1;

            allCells.Add(c);
        }
    }

    public void MetaballCollision(Collision col, MetaballScript metaball)
    {
        marchingSquaresShader.SetVector("metaballPos", metaball.transform.position);
        marchingSquaresShader.SetFloat("metaballRadius", metaball.myInfo.radius);
        marchingSquaresShader.SetFloat("metaballVelocity", metaball.GetVelocity().magnitude);

        marchingSquaresShader.SetBuffer(marchingSquaresShader.FindKernel("MetaballImpact"), "allCells", cellBuffer);

        marchingSquaresShader.Dispatch(marchingSquaresShader.FindKernel("MetaballImpact"), 8, 8, 1);
    }

    void CreateMesh()
    {
        newTris.Clear();
        newVertices.Clear();

        myMesh.Clear();

        for(int i=0; i<allCells.Count; i++)
        {
            TriangulateCell(allCells[i]);
        }

        myMesh.vertices = newVertices.ToArray();
        myMesh.triangles = newTris.ToArray();
    }

    void TriangulateCell(Cell c)
    {
        int cellType = 0;

        if (c.myNodes[0].intensity >= 1)
            cellType |= 1;
        if (c.myNodes[1].intensity >= 1)
            cellType |= 2;
        if (c.myNodes[2].intensity >= 1)
            cellType |= 4;
        if (c.myNodes[3].intensity >= 1)
            cellType |= 8;

        switch(cellType)
        {
            case 0:
                //No mesh at all here
                break;
            case 1:
                AddTriangle(c.myNodes[0].pos, Interpolate(c.myNodes[0], c.myNodes[1]), Interpolate(c.myNodes[0], c.myNodes[2]));
                break;
            case 2:
                AddTriangle(c.myNodes[1].pos, Interpolate(c.myNodes[1], c.myNodes[0]), Interpolate(c.myNodes[1], c.myNodes[3]));
                break;
            case 3:
                AddTriangle(c.myNodes[0].pos, Interpolate(c.myNodes[0], c.myNodes[2]), c.myNodes[1].pos);
                AddTriangle(c.myNodes[1].pos, Interpolate(c.myNodes[1], c.myNodes[3]), c.myNodes[0].pos);
                break;
            case 4:
                AddTriangle(c.myNodes[2].pos, Interpolate(c.myNodes[0], c.myNodes[2]), Interpolate(c.myNodes[2], c.myNodes[3]));
                break;
            case 5:

                break;
            case 6:

                break;
            case 7:

                break;
            case 8:
                AddTriangle(c.myNodes[4].pos, Interpolate(c.myNodes[4], c.myNodes[1]), Interpolate(c.myNodes[4], c.myNodes[2]));
                break;
            case 9:

                break;
            case 10:

                break;
            case 11:

                break;
            case 12:

                break;
            case 13:

                break;
            case 14:

                break;
            case 15:

                break;
            case 16:

                break;
        }
    }

    Vector3 Interpolate(Node corner1, Node corner2)
    {
        Vector3 pos = Vector3.Lerp(corner1.pos, corner2.pos, 0.5f);

        return pos;
    }

    void AddTriangle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        pos1 -= transform.position;
        pos2 -= transform.position;
        pos3 -= transform.position;

        newTris.Add(newVertices.Count);
        newTris.Add(newVertices.Count+1);
        newTris.Add(newVertices.Count+2);

        newVertices.Add(pos1);
        newVertices.Add(pos2);
        newVertices.Add(pos3);

        Debug.DrawLine(pos1, pos2, Color.cyan);
        Debug.DrawLine(pos1, pos3, Color.cyan);
        Debug.DrawLine(pos3, pos2, Color.cyan);

        //Debug.Break();
    }

    IEnumerator HeavyCalculations()
    {
        while (true)
        {
            
            marchingSquaresShader.Dispatch(marchingSquaresShader.FindKernel("GraduallyDisappear"), 8, 8, 1);

            if (cellBuffer.count > 1)
            {
                cellArray = new Cell[cellBuffer.count];
                cellBuffer.GetData(cellArray);
                Debug.Log(cellArray[0].myNodes[0].intensity);
            }

            if (myMesh)
                CreateMesh();

            yield return new WaitForSeconds(0.05f);
        }
    }

    void OnDrawGizmos()
    {
        if (allNodes != null)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                Gizmos.DrawSphere(allNodes[i].pos, 0.05f);
            }
        }
    }

    bool IsExistingNode(Vector3 pos, out Node existingNode)
    {
        float threshold = 0.25f;

        for (int i = 0; i < allNodes.Count; i++)
        {
            float dist = Vector3.Distance(pos, allNodes[i].pos);

            if (dist <= threshold)
            {
                existingNode = allNodes[i];
                return true;
            }
        }

        existingNode = new Node();
        return false;
    }

    bool IsExistingCell(Vector3 pos)
    {
        float threshold = 0;

        for (int i = 0; i < allCells.Count; i++)
        {
            float dist = Vector3.Distance(pos, allCells[i].myNodes[0].pos);

            if (dist <= threshold)
            {
                //existingCell = allCells[i];
                return true;
            }
        }

        //existingCell = new Cell();
        return false;
    }

    bool GetClosestNode(Vector3 pos, out Node node)
    {
        float curDist = 9999;
        node = new Node();

        for(int i=0; i<allNodes.Count; i++)
        {
            float dist = Vector3.Distance(pos, allNodes[i].pos);

            if(dist < curDist)
            {
                curDist = dist;
                node = allNodes[i];
            }
        }

        return false;
    }
}
