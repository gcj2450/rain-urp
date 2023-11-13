Shader "Custom/TF2" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_RampTex("Ramp Tex (RGB)", 2D) = "white" {}
		_DiffuseCubeMap("Diffuse Convolution Cubemap", Cube) = ""{}
		_Amount("Diffuse Amount", Range(-10,10)) = 1

		_RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower("Rim Power", Range(0.5,8.0)) = 3.0

		_SpecularColor("Specular Color", Color) = (1,1,1,1)
		_SpecularPower("Specular Power", Range(-50, 50)) = 1
		_SpecularFresnel("Specular Fresnel Value", Range(0,1)) = 0.28
	}
		SubShader
		{
		Tags {
				"RenderType" = "Opaque"
				"LightMode" = "UniversalForward"
				"RenderPipeline" = "UniversalPipeline"
		}
		LOD 200

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
		sampler2D _MainTex;
		float4 _MainTex_ST;

		sampler2D _RampTex;
		float4 _RampTex_ST;
		half  _SpecularPower;
		half4 _SpecularColor;
		samplerCUBE _DiffuseCubeMap;
		float _Amount;
		float4 _RimColor;
		float _RimPower;
		float _RimFresnel;
		float _SpecularFresnel;

		CBUFFER_END
		ENDHLSL

		Pass
			{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile  LIGHTMAP_ON
			#define _MAIN_LIGHT_SHADOWS

			////接受物体投射出来的阴影

		 //  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

		 //  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

		 //  //软阴影

		 //  #pragma multi_compile _ _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"



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
				float4 shadowCoord : TEXCOORD4;
				/*float3 tangentWS : TEXCOORD4;
				float3 bitangentWS : TEXCOORD5;*/
				float4 viewDirWS : TEXCOORD6;
				//float  fogCoord : TEXCOORD7;    //TODO: combined to other texcoord 

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float CalcFresnel(float3 viewDir, float3 h, float fresnelValue)
			{
				float fresnel = pow(1.0 - dot(viewDir, h), 5.0);
				fresnel += fresnelValue * (1.0 - fresnel);
				return fresnel;
			}

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
				OUT.shadowCoord = TransformWorldToShadowCoord(mul(unity_ObjectToWorld, IN.positionOS).xyz);
				/*OUT.tangentWS = tbn.tangentWS;
				OUT.bitangentWS = tbn.bitangentWS;*/

				OUT.uv1 = IN.lightmapUV;

				//OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
				return OUT;
			}

			float4 LightingNoLight(half4 albedo,half3 normDir, half3 lightDir, half3 viewDir, half3 atten)
			{
				//Specular term
				float3 halfVector = normalize(lightDir + viewDir);
				float3 specBase = pow(saturate(dot(halfVector, normDir)), _SpecularPower);// *s.Specular * specFresnel;
				float fresnel = 1.0 - dot(viewDir, halfVector);
				fresnel = pow(fresnel, 5.0);
				fresnel += _SpecularFresnel * (1.0 - fresnel);

				//float3 finalSpec = _SpecularColor * spec* fresnel;;
				float3 finalSpec = specBase * fresnel * _MainLightColor.rgb;

				//wrapped diffuse term
				half NdotL = dot(normDir, lightDir);
				float halfLambert = NdotL * 0.5 + 0.5;
				half3 ramp = tex2D(_RampTex, float2(halfLambert, halfLambert)).rgb;

				//ambientCube term
				float3 ambientCube = texCUBE(_DiffuseCubeMap, normDir).rgb * _Amount;

				half4 c;
				//c.rgb = finalSpec * _MainLightColor.rgb;
				c.rgb = albedo *( _MainLightColor.rgb * ramp * (atten * 2) * ambientCube + finalSpec * _MainLightColor.rgb);
				//c.rgb = finalSpec * _MainLightColor.rgb;
				//c.rgb =  s.Normal;
				//c.rgb =  finalSpec;
				c.a = albedo.a;
				return c;
			}

			float4 frag(Varyings IN) : SV_Target
			{
				half4 baseCol = tex2D(_MainTex, IN.uv);
				
				//light dir
				float4 SHADOW_COORDS = TransformWorldToShadowCoord(IN.positionWS);
				Light light = GetMainLight(SHADOW_COORDS);
				half3 lightDirWS = normalize(light.direction);
				//view dir
				half3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - IN.positionWS);
				//return LightingNoLight(baseCol, IN.normalWS, lightDirWS, viewDirWS, light.shadowAttenuation);

				//wrapped diffuse term
				half NdotL = dot(IN.normalWS, lightDirWS);
				float halfLambert = NdotL * 0.5 + 0.5;

				//Specular term
				float3 halfVector = normalize(lightDirWS + viewDirWS);
				float3 specBase = pow(max(0, dot(halfVector, IN.normalWS)), _SpecularPower);

				//float3 specBase = pow(saturate(dot(halfVector, IN.normalWS)), _SpecularPower);// *s.Specular * specFresnel;
				float fresnel = 1.0 - dot(viewDirWS, halfVector);
				fresnel = pow(fresnel, 5.0);
				fresnel += _SpecularFresnel * (1.0 - fresnel);

				//float3 finalSpec = _SpecularColor * spec* fresnel;;
				float3 finalSpec = specBase * fresnel * _MainLightColor.rgb;


				half3 ramp = tex2D(_RampTex, float2(halfLambert, halfLambert)).rgb;

				//half3 viewIndependentLight = baseCol * _MainLightColor.rgb * ramp;

				//ambientCube term
				float3 ambientCube = texCUBE(_DiffuseCubeMap, IN.normalWS).rgb * _Amount;

				//Ambient
				float3 SH = SampleSH(IN.normalWS);
				half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
				half shadow = MainLightRealtimeShadow(SHADOW_COORDS);

				//rim light term
				half rim = 1.0 - saturate(dot(normalize(viewDirWS), IN.normalWS));
				half3 emission = _RimColor.rgb * pow(rim, _RimPower) * 0.5;
				half4 c;

				//c.rgb = finalSpec * _MainLightColor.rgb;
				c.rgb = _MainLightColor.rgb* baseCol.rgb* (ramp* (light.shadowAttenuation * 2)* ambientCube + ambient + finalSpec + emission);
				
				//c.rgb = lerp(c.rgb * ambient.rgb, c.rgb, shadow) * SH;
				//c.rgb = c.rgb *shadow * SH;

				//c.rgb +=  emission;
				//c.rgb = finalSpec * _MainLightColor.rgb;
				//c.rgb =  IN.normalWS;
				//c.rgb =  finalSpec;
				c.a = baseCol.a;
				return c;
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