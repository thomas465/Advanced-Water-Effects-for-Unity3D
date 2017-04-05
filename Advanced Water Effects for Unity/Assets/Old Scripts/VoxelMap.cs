using UnityEngine;
using System.Collections;

public class VoxelMap : MonoBehaviour
{
    //public float size = 2.0f;

    //public int voxelResolution = 8;
    //public int chunkResolution = 2;

    //public VoxelGrid voxelGridPrefab;

    //float chunkSize, voxelSize, halfSize;

    //VoxelGrid[] chunks;

    //public GameObject metaball;

    //public float rate = 5;

    //// Use this for initialization
    //void Awake()
    //{
    //    if (chunks == null)
    //    {
    //        halfSize = size * 0.5f;
    //        chunkSize = size / chunkResolution;
    //        voxelSize = chunkSize / voxelResolution;

    //        chunks = new VoxelGrid[chunkResolution * chunkResolution];

    //        int i = 0;

    //        for (int y = 0; y < chunkResolution; y++)
    //        {
    //            for (int x = 0; x < chunkResolution; x++)
    //            {
    //                CreateChunk(x, y, i);
    //                i++;
    //            }
    //        }

    //    }
    //}

    //void CreateChunk(float x, float y, int i)
    //{
    //    VoxelGrid newVoxel = Instantiate(voxelGridPrefab) as VoxelGrid;
    //    newVoxel.transform.parent = transform;
    //    newVoxel.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize, 0);

    //    newVoxel.Initialize(voxelResolution, size);

    //    chunks[i] = newVoxel;
    //}

    //// Update is called once per frame
    //void Update()
    //{
    //    //if(Input.GetButtonDown("Fire1") || Random.Range(0.0f, 1.0f)>0.8f-rate/5)
    //    //{
    //    //    float range = 1.5f;
    //    //    Vector3 metaballPos = new Vector3(Random.Range(-range, range), 2, 0);
    //    //    metaballPos += transform.position;
    //    //    Instantiate(metaball, metaballPos, transform.rotation);
    //    //    //Debug.Break();
    //    //}
    //}

    //public void Disable()
    //{
    //    if(chunks==null)
    //    {
    //        Awake();
    //    }


    //    for(int i=0; i<chunks.Length; i++)
    //    {
    //        for(int j=0; j<chunks[i].voxels.Length; j++)
    //        {
    //            chunks[i].voxels[j].active = false;
    //            chunks[i].voxels[j].density = 0;
    //        }
    //    }

    //    gameObject.SetActive(false);

    //}

    //public void Create(Vector3 pos, Vector3 dir)
    //{
    //    transform.position = pos + dir * 0.015f;
    //    transform.rotation = Quaternion.LookRotation(dir);

    //    gameObject.SetActive(true);
    //}
}
