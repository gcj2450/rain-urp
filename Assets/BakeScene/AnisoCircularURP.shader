
Shader "Hair Shader/mobile/AnisoCircularURP" 
{
	Properties 
	{
		_MainTex ("Diffuse (RGB) Alpha (A)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
		_SpecularMultiplier ("Specular Multiplier", float) = 100.0
        _SpecularColor ("Specular Color1", Color) = (1,1,1,1)
		_AnisoOffset ( "Anisotropic Highlight Offset", Range(-1,1)) = 0.0
        _Cutoff ("Alpha Cut-Off Threshold", float) = 0.5
		_Gloss ( "Gloss Multiplier", float) = 128.0
		_Atten("Atten", float) = 1.0
		_Multiplier ( "Multiplier", float) = 1.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
	}
	
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="TransparentCutout" "RenderPipeline" = "UniversalPipeline"}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		CBUFFER_START(UnityPerMaterial)
		sampler2D _MainTex;
		float4 _MainTex_ST;
		half _AnisoOffset,_SpecularMultiplier,_Gloss;
		half4 _SpecularColor, _Color;
		half _Atten;
		half _Cutoff;
		half _Multiplier;
		CBUFFER_END
		ENDHLSL

		Pass
		{
			AlphaTest LEqual [_Cutoff]

			Blend SrcAlpha OneMinusSrcAlpha

			Cull [_Cull]

			ZWrite On

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#pragma target 3.0

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct appdata_full {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;  
				float3 worldNormal : TEXCOORD2;  
				float4 vertex : SV_POSITION;
				//float4 shadowCoord : TEXCOORD3;
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				//UNITY_INITIALIZE_OUTPUT(v2f,o);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				o.worldNormal = TransformObjectToWorldNormal(v.normal);
				//o.shadowCoord = TransformWorldToShadowCoord(mul(unity_ObjectToWorld, v.vertex).xyz); 
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				//float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);
				//half shadow = MainLightRealtimeShadow(i.shadowCoord);
				//half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

				half4 albedo = tex2D(_MainTex, i.uv);
				clip(albedo.a -_Cutoff);
				
				half4 finalColor = half4(0, 0, 0, albedo.a);
				finalColor.rgb += (albedo.rgb * _Color.rgb) * _MainLightColor.rgb;
				//finalColor.rgb = lerp(finalColor.rgb * ambient.rgb, finalColor.rgb, shadow);
				return finalColor;
			};
			ENDHLSL
		}

		Pass
		{
			Tags { "LightMode" = "UniversalForward" }
			ZWrite Off
			Cull [_Cull]
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM

			#pragma multi_compile  LIGHTMAP_ON
			#define _MAIN_LIGHT_SHADOWS
            //#define _SHADOWS_SOFT
            //#define _ALPHATEST_ON
			//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _SHADOWS_SOFT
            //#pragma shader_feature _ALPHATEST_ON

			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#pragma target 3.0



			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct appdata_full {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				float4 texcoord3 : TEXCOORD3;
				half4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
		
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;  
				float3 worldNormal : TEXCOORD2;  
				float4 vertex : SV_POSITION;
				float4 shadowCoord : TEXCOORD3;
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				//UNITY_INITIALIZE_OUTPUT(v2f,o);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

				/*VertexNormalInputs normalInput = GetVertexNormalInputs(v.normal, v.tangent);
				o.normalWS = normalInput.normalWS;*/

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				o.worldNormal = TransformObjectToWorldNormal(v.normal);
				o.shadowCoord = TransformWorldToShadowCoord(mul(unity_ObjectToWorld, v.vertex).xyz); 
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 albedo = tex2D(_MainTex, i.uv);
				float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);
				Light mainLight = GetMainLight(SHADOW_COORDS);
				half3 worldNormal = normalize(i.worldNormal);	
				//half3 worldLightDir = normalize(_MainLightPosition.xyz); 
				half3 worldLightDir = normalize(mainLight.direction);
				half NdotL = saturate(dot(worldNormal, worldLightDir)); 

				half aniso = max(0, sin(radians((NdotL + _AnisoOffset) * 180)));
				
				aniso = pow( aniso, _Gloss);
				aniso = aniso * _SpecularMultiplier;

				half shadow = MainLightRealtimeShadow(SHADOW_COORDS);
				half3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);

				float Ramp_light=dot( worldLightDir, i.worldNormal)*0.5+0.5;

				float3 SH = SampleSH(i.worldNormal);

				/*half3 bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, i.normalWS);
				half3 bakedGI = SamplLightmap(lightmapUV, normalWS);*/

				half4 c;
				c.rgb = ((albedo.rgb* _Color.rgb) + (mainLight.shadowAttenuation *mainLight.distanceAttenuation * _Atten * mainLight.color.rgb * NdotL)+ (aniso * _SpecularColor.rgb)) ;
				c.a = albedo.a;
				c.rgb = _Multiplier * lerp(c.rgb * ambient.rgb, c.rgb, shadow) * SH;
				return c;
			};
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

	FallBack "Transparent/Cutout/VertexLit"
}