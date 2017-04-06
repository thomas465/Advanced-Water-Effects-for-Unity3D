//UNUSED!!
Shader "Unlit/MetaballShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

		//Structured buffers
		//StructeredBuffer<short> metaballPowers;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = v.normal;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2f input[3], inout TriangleStream<v2f> OutputStream)
			{
				v2f test = (v2f)0;
				float3 normal = normalize(cross(input[1].vertex.xyz - input[0].vertex.xyz, input[2].vertex.xyz - input[0].vertex.xyz));

				for (int i = 0; i < 3; i++)
				{
					test.normal = normal;
					test.vertex = input[i].vertex;
					test.uv = input[i].uv;
					OutputStream.Append(test);
				}
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
			// apply fog
			UNITY_APPLY_FOG(i.fogCoord, col);
			return col;
		}
		ENDCG
	}
	}
}
