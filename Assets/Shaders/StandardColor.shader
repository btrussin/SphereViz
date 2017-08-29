Shader "Custom/StandardColor"
{
	Properties
	{

	}
	SubShader
	{
		//Tags { "RenderType"="Opaque" }
		
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc" // for _LightColor0

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				fixed4 color : COLOR0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 diff : COLOR0;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0 * v.color;
				//o.diff.w = 1.0;
				//o.diff = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return i.diff;
			}
			ENDCG
		}

		Pass{
				Tags {"LightMode" = "ForwardAdd"}
				Blend One One

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc" // for _LightColor0

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					fixed4 color : COLOR0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					fixed4 diff : COLOR0;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					half3 worldNormal = UnityObjectToWorldNormal(v.normal);
					half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
					o.diff = nl * _LightColor0 * v.color;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					return i.diff;
				}
				ENDCG

		}
	}
}
