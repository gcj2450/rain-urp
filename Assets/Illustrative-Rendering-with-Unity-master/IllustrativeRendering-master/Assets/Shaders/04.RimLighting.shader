﻿Shader "Custom/Illustrative Rendering/04.RimLighting"
{
	Properties
	{
		_SpecularMask ("Specular Mask", 2D) = "white" {}
		_RimMask ("Rim Mask", 2D) = "white" {}
		_RimPower ("Rim Power", Float) = 4
		_Krim ("Rim Exponent", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" 
		"LightMode"="UniversalForward"
		"RenderPipeline" = "UniversalPipeline"
		}
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			//#pragma target 3.0

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				half3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				half3 VdotR : TEXCOORD1;
				half3 VdotN : TEXCOORD2;
				half3 NdotU : TEXCOORD3;
			};

			sampler2D _SpecularMask;
			half4 _SpecularMask_ST;
			sampler2D _RimMask;
			half4 _RimMask_ST;
			half _RimPower;
			half _Krim;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _RimMask);

				half3 viewDir = normalize(GetWorldSpaceViewDir(v.vertex.xyz));
				half3 lightDir = normalize(_MainLightPosition.xyz);
				half3 worldNormal = TransformObjectToWorldNormal(v.normal);
				half3 reflectDir = reflect(-lightDir, worldNormal);
				o.VdotR = saturate(dot(viewDir, reflectDir));
				o.VdotN = saturate(dot(viewDir, worldNormal));

				half3 worldUp = half3(0, 1, 0);
				o.NdotU = dot(worldNormal, worldUp);

				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half4 ks = tex2D( _SpecularMask, i.uv);

				half fresnelRim = pow(1 - i.VdotN, _RimPower);
				half4 kr = tex2D(_RimMask, i.uv);
                half3 rimTerm = fresnelRim * kr * pow(i.VdotN, _Krim);

                half3 multiplePhongTerms = rimTerm;
                half3 dedicatedRimLighting = i.NdotU * fresnelRim * kr;

                half4 col;
                col.rgb = multiplePhongTerms + dedicatedRimLighting;
                col.a = 1;

				return col;
			}
				ENDHLSL
		}
	}
}
