Shader "Custom/CustomWaterTEST" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)

		_RimColor("Rim Color", Color) = (1,1,1,1)

		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_Scale("Scale", Float) = 0
		_Scale_BackandForth("Scale back and forth", Float) = 0

		_Speed("Speed", Float) = 0

		_Flow("Flow Direction and Speed", Vector) = (0, 0, 0)

		_BumpTex("BumpTex (RGB)", 2D) = "white" {}
	_BumpTex2("BumpTex (RGB)", 2D) = "white" {}
	_HeightMapTex("HeightMapTex (RGB)", 2D) = "white" {}

	_InvFade("Rim thing", Float) = 0
	}
		SubShader{
		Tags{ "Queue" = "Transparent" }
		LOD 200
		Cull Off
GrabPass{}

CGPROGRAM
#pragma surface surf Lambert vertex:vert alpha
#pragma target 3.0
sampler2D _GrabTexture;
	sampler2D _MainTex, _BumpTex, _BumpTex2, _HeightMapTex;

	float4 _Color, _HeightMapTex_ST;

	float _InvFade;
	sampler2D_float _CameraDepthTexture;
	float4 _CameraDepthTexture_TexelSize;

struct Input
{
	float4 grabUV;
	float2 uv_MainTex;

	float4 screenPos;
	float eyeDepth;
};



void vert(inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);
	float4 coord = v.texcoord;

	float2 c = TRANSFORM_TEX(coord, _HeightMapTex);

	coord.xy += _Time[0] * 0.1f;

	float speed = 0.15f;

	//coord.xyz += (_Time[1] * speed) * 0.4f;

	float elevation = tex2Dlod(_HeightMapTex, coord).r;// *_SinTime[3];// *_SinTime[2];

	elevation *= 0.0213f;

	v.vertex.y += (elevation);

	o.uv_MainTex = v.texcoord;
	float4 hpos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.grabUV = ComputeGrabScreenPos(hpos);
	//o.uv_MainTex = coord;
	//o.localPos = v.vertex;

	//o.waveAmount = clamp(v.vertex.y, 0, 0.9f);

	COMPUTE_EYEDEPTH(o.eyeDepth);
	//UNITY_TRANSFER_FOG(o, o.pos);
}

void surf(Input IN, inout SurfaceOutput o)
{
	fixed4 offset = (0, 0, 0, 0);
	fixed4 c = (tex2D(_MainTex, IN.uv_MainTex + offset)) * _Color;// * tex2D(_GrabTexture, ComputeGrabScreenPos(IN.pos.xy));
	o.Albedo = c.rgb;

	offset += _Time[1] / 8;
	o.Normal = (tex2D(_BumpTex, IN.uv_MainTex + offset));
	o.Normal = lerp(tex2D(_BumpTex2, IN.uv_MainTex - offset / 2), o.Normal, 0.5f);

	fixed4 grabOffset = (0,0,0,0);
	//grabOffset.x += (0.23f * o.Normal);
	//grabOffset.x -= 0.3f;
	grabOffset.y -= 0.23f * o.Normal;

	

	float rawZ = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos));
	float sceneZ = LinearEyeDepth(rawZ);
	float partZ = IN.eyeDepth;

	float fade = 0.0;

	if (rawZ > 0.0) // Make sure the depth texture exists
		fade = saturate((_InvFade + (_SinTime[3])/8) * (sceneZ - partZ));

	fade = fade;

	float maxFade = 0.95f, minFade = 0.81f;

	if (fade > maxFade)
		fade = maxFade;
	if (fade < minFade)
		fade = minFade;

	o.Emission = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(IN.grabUV + grabOffset));
	o.Alpha = 1.0;

	o.Albedo = float4(1, 1, 1, 1) - fade;
}
ENDCG
	}}