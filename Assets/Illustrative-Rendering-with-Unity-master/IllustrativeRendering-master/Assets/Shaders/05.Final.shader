Shader "Custom/Illustrative Rendering/05.Final"
{
	Properties
	{
		[Header(Main Map)]
		_MainTex("Albedo", 2D) = "white" {}

		[Toggle(_NORMALMAP)] _NormalMapToggle("Normal Mapping", Float) = 0
		//Normal control
		_BumpScale("Normal Scale", Range(0, 1)) = 1.0
		_BumpMap("Normal Map", 2D) = "bump"  {}

		[Header(RampTex)]
		_RampTex("RampTex", 2D) = "white" {}
		_WarpedScale("Warped Scale", Float) = 1

		[Header(Specular)]
		_SpecularColor("Specular Color", Color) = (1,1,1,1)
		_SpecularMask("Specular Mask", 2D) = "white" {}
		_SpecularFresnel("Specular Fresnel Value", Float) = 1
		_SpecularPower("Specular Power", Range(0.1, 128)) = 1

		[Header(Rim)]
		_RimMask("Rim Mask", 2D) = "white" {}
		_RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower("Rim Power", Range(0.1, 8)) = 4
		_FresnelRimPower("Fresnel Rim Power", Range(0.1, 8)) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" "LightMode" = "UniversalForward"
			"RenderPipeline" = "UniversalPipeline"
			}

					HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
		TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);
		TEXTURE2D(_SpecularMask); SAMPLER(sampler_SpecularMask);
		TEXTURE2D(_RimMask); SAMPLER(sampler_RimMask);
		//TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

		CBUFFER_START(UnityPerMaterial)
			half4 _MainTex_ST;

			half4 _BumpMap_ST;
			float _BumpScale;

			half4 _RampTex_ST;
			half _WarpedScale;

			half4 _SpecularMask_ST;
			float4 _SpecularColor;
			half _SpecularFresnel;
			half _SpecularPower;

			half4 _RimMask_ST;
			float4 _RimColor;
			half _RimPower;
			half _FresnelRimPower;
		CBUFFER_END


		ENDHLSL


			Pass
			{
				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma shader_feature _NORMALMAP

				#pragma multi_compile  LIGHTMAP_ON
				#define _MAIN_LIGHT_SHADOWS

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 tangentOS : TANGENT;
				float2 uv : TEXCOORD0;
				float2 lightmapUV : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float3 normalWS : TEXCOORD3;
				//float4 shadowCoord : TEXCOORD4;
				float3 tangentWS : TEXCOORD4;
				float3 bitangentWS : TEXCOORD5;
				float4 viewDirWS : TEXCOORD6;
				float  fogCoord : TEXCOORD7;    //TODO: combined to other texcoord 

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

				

				Varyings vert(Attributes IN)
				{
					Varyings OUT = (Varyings)0;

					UNITY_SETUP_INSTANCE_ID(IN);
					UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

					OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
					OUT.positionCS = TransformWorldToHClip(OUT.positionWS);

					OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

					VertexNormalInputs tbn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
					OUT.normalWS = tbn.normalWS;
					//OUT.shadowCoord = TransformWorldToShadowCoord(mul(unity_ObjectToWorld, IN.positionOS).xyz);
					OUT.tangentWS = tbn.tangentWS;
					OUT.bitangentWS = tbn.bitangentWS;

					OUT.uv1 = IN.lightmapUV;

					OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
					return OUT;
				}

				half4 frag(Varyings IN) : SV_Target
				{
					half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv); 

#ifdef _NORMALMAP 
					//half3 normalTS = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv).rgb * _BumpScale;
					 half3 normalTS = SampleNormal(IN.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
					IN.normalWS = TransformTangentToWorld(normalTS, half3x3(IN.tangentWS.xyz, IN.bitangentWS.xyz, IN.normalWS.xyz));
#endif
					float4 SHADOW_COORDS = TransformWorldToShadowCoord(IN.positionWS);
					Light mainLight = GetMainLight(SHADOW_COORDS);
					half3 lightDir = normalize(mainLight.direction);

					float NdotL = dot(IN.normalWS, lightDir);

					half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
					half3 reflectDir = reflect(lightDir, IN.normalWS);
					float VdotR = saturate(dot(viewDir, reflectDir));
					float VdotN = saturate(dot(viewDir, IN.normalWS));

					half3 worldUp = half3(0, 1, 0);
					float NdotU =saturate( dot(IN.normalWS, worldUp));


					//View Independent Lighting
					
					half halfLambert = pow(0.5 * NdotL + 0.5, 2);
					half2 warpedUV = float2(halfLambert, halfLambert);

					//half3 diffuseWarping =tex2D(_RampTex, warpedUV).rgb * _WarpedScale;
					half3 diffuseWarping = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, warpedUV).rgb * _WarpedScale; 

					half3 viewIndependentLight = baseCol.rgb * mainLight.color.rgb * diffuseWarping;

					//View Dependent Lighting
					//Multiple Phong Terms
					
					half fresnelRim = pow(1 - VdotN, _FresnelRimPower);
					half4 kr = SAMPLE_TEXTURE2D(_RimMask, sampler_RimMask, IN.uv);  //tex2D(_RimMask, IN.uv);

					half3 specularTerm = _SpecularFresnel * pow(VdotR, _SpecularPower);
					//half3 rimTerm = fresnelRim * kr * pow(VdotR, _RimPower) * _RimColor;
					half3 rimTerm = fresnelRim * pow(VdotR, _RimPower) * _RimColor.rgb;

					half4 ks = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, IN.uv);  //tex2D(_SpecularMask, IN.uv);

					half3 multiplePhongTerms = mainLight.color.rgb * ks.rgb * max(specularTerm, rimTerm);


					//Dedicated Rim Lighting
					half3 dedicatedRimLighting = NdotU * fresnelRim * kr.rgb;

					half3 viewDependentLight = _SpecularColor.rgb* (multiplePhongTerms + dedicatedRimLighting);


					//Ambient:SH=ambient;
					float3 SH = SampleSH(IN.normalWS);
					half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
					half shadow = MainLightRealtimeShadow(SHADOW_COORDS);

					//Final Result
					half4 finalColor=0;
					finalColor.rgb = viewIndependentLight + viewDependentLight;
					finalColor.rgb *= mainLight.shadowAttenuation;
					//finalColor.rgb = lerp(finalColor.rgb * ambient.rgb, finalColor.rgb, shadow) * SH;
					finalColor.a = 1;

					return finalColor;
				}
					ENDHLSL
			}
			Pass
			{
				Name "ShadowCaster"
				Tags{ "LightMode" = "ShadowCaster" }
				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

				struct a2v {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float3 normal : NORMAL;
				};
				struct v2f {
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				float3 _LightDirection;
				float4 _ShadowBias;
				half4 _MainLightShadowParams;
				float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
				{
					float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
					float scale = invNdotL * _ShadowBias.y;
					// normal bias is negative since we want to apply an inset normal offset
					positionWS = lightDirection * _ShadowBias.xxx + positionWS;
					positionWS = normalWS * scale.xxx + positionWS;
					return positionWS;
				}
				v2f vert(a2v v)
				{
					v2f o = (v2f)0;
					float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
					half3 normalWS = TransformObjectToWorldNormal(v.normal);
					worldPos = ApplyShadowBias(worldPos, normalWS, _LightDirection);
					o.vertex = TransformWorldToHClip(worldPos);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}
				half4 frag(v2f i) : SV_Target
				{
					#if _ALPHATEST_ON
					half4 col = tex2D(_MainTex, i.uv);
					clip(col.a - 0.001);
					#endif
					return 0;
				}
				ENDHLSL
			}
		}
}
