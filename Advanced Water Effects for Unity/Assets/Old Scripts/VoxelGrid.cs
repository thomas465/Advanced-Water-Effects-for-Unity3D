using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[SelectionBase]
public class VoxelGrid : MonoBehaviour {

    public Material waterMat;

    public int resolution;


    [SerializeField]
    public class Voxel
    {
        public bool active;
        public Vector3 pos, xEdgePos, yEdgePos;
        public float density;

        public Voxel(int x, int y, float size)
        {
            pos.x = (x + 0.5f) * size;
            pos.y = (y + 0.5f) * size;

            xEdgePos = pos;
            xEdgePos.x += size * 0.5f;

            yEdgePos = pos;
            yEdgePos.y += size * 0.5f;

            density = 0;
        }
    }

    [SerializeField]
    public Voxel[] voxels;

    public GameObject voxelPrefab;

    float voxelSize;

    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;

    public void Initialize(int res, float size)
    {
        resolution = res;

        voxelSize = size / res;
        voxels = new Voxel[res * res * res];

        int i = 0;


            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    CreateVoxel(x, y, 0, i);
                    i++;
                }
            }
    }

    void Triangulate()
    {
        vertices.Clear();
        triangles.Clear();
        mesh.Clear();

        TriangulateCellRows();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        //GetComponent<Renderer>().material = waterMat;
    }

    void TriangulateCellRows()
    {
        int cells = resolution - 1;

        int i = 0;

        //for (int z = 0; z < cells; z++)
        {
            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    TriangulateCell(voxels[i], voxels[i + 1], voxels[i + resolution], voxels[i + resolution + 1], voxels[i+resolution*resolution], voxels[i+(resolution*resolution)+1], voxels[i + (resolution * resolution) + resolution], voxels[i + (resolution * resolution) + 1]);
                    i++;
                }
            }

        }
    }

    void OnDrawGizmos()
    {
        for(int i=0; i<voxels.Length; i++)
        {
            if(voxels[i]!=null)
            Gizmos.DrawSphere(voxels[i].pos, 0.2f);
        }
    }
    
    void TriangulateCell(Voxel a, Voxel b, Voxel c, Voxel d, Voxel a2, Voxel b2, Voxel c2, Voxel d2)
    {
        int cellType = 0;

        if (a.active)
            cellType |= 1;
        if (b.active)
            cellType |= 2;
        if (c.active)
            cellType |= 4;
        if (d.active)
            cellType |= 8;

        if (a2!=null)
        {
            if (a2.active)
                cellType |= 16;
            if (b2.active)
                cellType |= 32;
            if (c2.active)
                cellType |= 64;
            if (d2.active)
                cellType |= 128;
        }

        //Debug.Log(cellType);

        //Placing triangles
        switch (cellType)
        {
            case 0:
                return;
                
            //SINGLE CORNER CASES
            case 1:
                AddTriangle(a.pos, a.yEdgePos, a.xEdgePos);
                break;
            case 2:
                AddTriangle(b.pos, a.xEdgePos, b.yEdgePos);
                break;
            case 4:
                AddTriangle(c.pos, c.xEdgePos, a.yEdgePos);
                break;
            case 8:
                AddTriangle(d.pos, b.yEdgePos, c.xEdgePos);
                break;

            //CASES INVOLVING SQUARES
            case 3:
                AddQuad(a.pos, a.yEdgePos, b.yEdgePos, b.pos);
                break;
            case 5:
                AddQuad(a.pos, c.pos, c.xEdgePos, a.xEdgePos);
                break;
            case 10:
                AddQuad(a.xEdgePos, c.xEdgePos, d.pos, b.pos);
                break;
            case 12:
                AddQuad(a.yEdgePos, c.pos, d.pos, b.yEdgePos);
                break;
            case 15:
                AddQuad(a.pos, c.pos, d.pos, b.pos);
                break;

            //PENTAGON STUFF
            case 7:
                AddPentagon(a.pos, c.pos, c.xEdgePos, b.yEdgePos, b.pos);
                break;
            case 11:
                AddPentagon(b.pos, a.pos, a.yEdgePos, c.xEdgePos, d.pos);
                break;
            case 13:
                AddPentagon(c.pos, d.pos, b.yEdgePos, a.xEdgePos, a.pos);
                break;
            case 14:
                AddPentagon(d.pos, b.pos, a.xEdgePos, a.yEdgePos, c.pos);
                break;

            case 6:
                AddTriangle(b.pos, a.xEdgePos, b.yEdgePos);
                AddTriangle(c.pos, c.xEdgePos, a.yEdgePos);
                break;

            case 9:
                AddTriangle(a.pos, a.yEdgePos, a.xEdgePos);
                AddTriangle(d.pos, b.yEdgePos, c.xEdgePos);
                break;
        }
    }

    void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int vertexIndex = vertices.Count;

        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int vertexIndex = vertices.Count;

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

    void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
    {
        int vertexIndex = vertices.Count;

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

    void CreateVoxel(int x, int y, int z, int i)
    {
        GameObject newVoxel = Instantiate(voxelPrefab) as GameObject;
        newVoxel.transform.parent = transform;
        newVoxel.transform.localPosition = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
        newVoxel.transform.localScale = Vector3.one * voxelSize * 0.4f;

        voxels[i] = new Voxel(x, y, voxelSize);
        newVoxel.GetComponent<VoxelScript>().myInfo = voxels[i];
    }

    void MoveVoxels()
    {
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
            }
        }
    }

	// Use this for initialization
	void Start () {
        vertices = new List<Vector3>();
        triangles = new List<int>();

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "VoxelGrid Mesh";
       
	}
	
	// Update is called once per frame
	void Update () {
        Triangulate();
	}
}
