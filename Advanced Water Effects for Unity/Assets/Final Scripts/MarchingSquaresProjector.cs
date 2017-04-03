using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cancelled approach using projectors rather than directly changing the grid
/// </summary>
public class MarchingSquaresProjector : MonoBehaviour {

 //   [SerializeField]
 //   RenderTexture rt;

 //   public Material myMaterial;
 //   public Projector myProj;

	//// Use this for initialization
	//void Start () {
 //       rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
 //       rt.name = "MarchingSquaresTexture (" + gameObject.name + ")";
 //       rt.wrapMode = TextureWrapMode.Clamp;
 //       rt.Create();

 //       GetComponent<Camera>().targetTexture = rt;

 //       myMaterial.SetTexture("_ShadowTex", rt);
 //       myMaterial.mainTexture = rt;
 //       myProj.material = myMaterial;
 //   }
	
	//// Update is called once per frame
	//void Update () {
		
	//}
}
