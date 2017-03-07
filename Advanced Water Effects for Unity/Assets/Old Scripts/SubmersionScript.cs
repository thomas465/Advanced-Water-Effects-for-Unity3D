using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SubmersionScript : MonoBehaviour
{

    public Renderer[] allMeshRenderers;
    public List<Material> allMaterials;
    public List<Mesh> allMeshes;
    public Texture2D[] wetMaps;

    MeshCollider col;

    int sizeDivide = 6;

    float updateTimer = 0;
    float updateIntervalLength = 0.15f;

    // Use this for initialization
    void Start()
    {
        allMeshRenderers = GetComponentsInChildren<Renderer>();
        allMeshes = new List<Mesh>();

        for (int i = 0; i < allMeshRenderers.Length; i++)
        {
            for (int j = 0; j < allMeshRenderers[i].materials.Length; j++)
            {
                allMaterials.Add(allMeshRenderers[i].materials[j]);
            }

            if (allMeshRenderers[i].GetComponent<SkinnedMeshRenderer>())
            {
                allMeshes.Add(allMeshRenderers[i].GetComponent<SkinnedMeshRenderer>().sharedMesh);
            }
        }

        //wetMaps = new Texture2D[allMaterials.Count];



        for (int i = 0; i < wetMaps.Length; i++)
        {
            //wetMaps[i] = new Texture2D(allMaterials[i].mainTexture.width / sizeDivide, allMaterials[i].mainTexture.height / sizeDivide);

            for (int y = 0; y < wetMaps[i].height / sizeDivide; y++)
            {
                for (int x = 0; x < wetMaps[i].width / sizeDivide; x++)
                {
                    //wetMaps[i].SetPixel(x, y, Color.white);
                }

                //wetMaps[i].Apply();
            }

            Color[] set = new Color[250 * 350];

            for (int c = 0; c < set.Length; c++)
            {
                set[c].r = 1;
            }

            //wetMaps[i].SetPixels(0, 0, 45, 45, set);
        }

        for (int i = 0; i < allMaterials.Count; i++)
        {
            //allMaterials[i].SetTexture("_WetMap", wetMaps[i]);
        }
    }



    // Update is called once per frame
    void Update()
    {
        //Debug.Log(allMeshes[0].vertices[0]);

        for (int m = 0; m < allMeshes.Count; m++)
        {
            Color[] vertColours = new Color[allMeshes[m].vertexCount];

            for (int v = 0; v < allMeshes[m].vertices.Length; v += 1)
            {
                Vector3 vertexPos = allMeshes[m].vertices[v];

                if (transform.transform.TransformPoint(vertexPos).y < 6)
                    vertColours[v].b = 1;

                vertColours[v] = Color.Lerp(vertColours[v], Color.black, 1 * Time.deltaTime);
            }

            allMeshes[m].colors = vertColours;
        }


        if (updateTimer > 0)
        {
            updateTimer -= Time.deltaTime;
        }
        else
        {
            updateTimer = updateIntervalLength;



            //for (int i = 0; i < wetMaps.Length; i++)
            //{
            //    //Drying
            //    Color[] fullBlock = wetMaps[i].GetPixels();

            //    for (int f = 0; f < fullBlock.Length; f++)
            //        fullBlock[f] -= Color.white * Time.deltaTime;

            //    wetMaps[i].SetPixels(fullBlock);

            //    //for (int y = 0; y < wetMaps[i].height; y++)
            //    //{
            //    //    for (int x = 0; x < wetMaps[i].width; x++)
            //    //    {
            //    //        Color newColor = wetMaps[i].GetPixel(x, y);

            //    //        newColor = Color.Lerp(newColor, Color.black, 0.5f * Time.deltaTime);
            //    //       // newColor -= Color.black * Time.deltaTime * 10;

            //    //        //if (newColor.r < 0)
            //    //            //newColor = Color.black;

            //    //        wetMaps[i].SetPixel(x, y, newColor);
            //    //    }
            //    //}


            //    //wetMaps[i].SetPixel((int)Time.timeSinceLevelLoad, (int)Time.timeSinceLevelLoad, Color.cyan);          
            //    wetMaps[i].Apply();
            //}
        }
    }
}
