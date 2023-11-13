Shader "Custom/URPStandard"
{
	Properties
	{
		_MainColor("Main Color", Color) = (1,1,1,0)
		_MainTeture("Main Teture", 2D) = "white" {}
		_RoughnessMap("RoughnessMap", 2D) = "white" {}
		_Metallic("Metallic", Range(0 , 1)) = 0.2
		_Gloss("Gloss", Range(0 , 1)) = 0.3
		_NormalIntensity("NormalIntensity", Range(0.001 , 2)) = 1
		_NormalMap("Normal Map", 2D) = "bump" {}
		[HDR]_Emission("Emission", Color) = (0,0,0,0)

		_ShadowMainColor("ShadowMainColor", color) = (0,0,0,1)

			//调整Shadow的接受阴影的XYZ位置，不影响投射出去的位置调整
			//_ShadowCoordAddX("ShadowCoordAddX", Range(0,0.1)) = 0
			//_ShadowCoordAddY("ShadowCoordAddY", Range(0,0.1)) = 0
			//_ShadowCoordAddZ("ShadowCoordAddZ", Range(0,0.1)) = 0
	}

		SubShader
		{
			LOD 100
			Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
			Cull Off
			AlphaToMask Off

			Pass
			{
				Name "Forward"
				Tags { "LightMode" = "UniversalForward" }

				HLSLPROGRAM

			//接受物体投射出来的阴影
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

			//增加点光照明效果
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS _ADDITIONAL_OFF
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

			//软阴影
			#pragma multi_compile _ _SHADOWS_SOFT

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 clipPos : SV_POSITION;
				float4 shadowCoord : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				float4 uv : TEXCOORD7;
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _NormalMap_ST;
			float4 _MainColor;
			float4 _MainTeture_ST;
			float4 _Emission;
			float4 _RoughnessMap_ST;
			float _NormalIntensity;
			float _Metallic;
			float _Gloss;
			float4 _ShadowMainColor;

			CBUFFER_END


			sampler2D _NormalMap;
			sampler2D _MainTeture;
			sampler2D _RoughnessMap;

			//float _ShadowCoordAddX;
			//float _ShadowCoordAddY;
			//float _ShadowCoordAddZ;

			v2f vert(a2v v)
			{
				v2f o = (v2f)0;//初始化 o;
				//v2f o;

				o.uv.xy = v.texcoord.xy;//UV
				o.uv.zw = 0;

				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float3 positionVS = TransformWorldToView(positionWS);
				float4 positionCS = TransformWorldToHClip(positionWS);

				VertexNormalInputs normalInput = GetVertexNormalInputs(v.normal, v.tangent);

				o.tSpace0 = float4(normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4(normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4(normalInput.bitangentWS, positionWS.z);

				half3 vertexLight = VertexLighting(positionWS, normalInput.normalWS);

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs a2v = (VertexPositionInputs)0;
				a2v.positionWS = positionWS;
				a2v.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord(a2v);
				#endif

				o.clipPos = positionCS;

				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				float3 WorldNormal = normalize(i.tSpace0.xyz);
				float3 WorldTangent = i.tSpace1.xyz;
				float3 WorldBiTangent = i.tSpace2.xyz;

				float3 WorldPosition = float3(i.tSpace0.w,i.tSpace1.w,i.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz - WorldPosition;

				float4 ShadowCoords = float4(0, 0, 0, 0);

				//调整Shadow的接受阴影的XYZ位置，不影响投射出去的位置调整
				//float4 ShadowcooordAdd = float4(_ShadowCoordAddX,_ShadowCoordAddY,_ShadowCoordAddZ,0);
				//ShadowCoords = TransformWorldToShadowCoord( WorldPosition ) + ShadowcooordAdd;

				ShadowCoords = TransformWorldToShadowCoord(WorldPosition);

				WorldViewDirection = SafeNormalize(WorldViewDirection);

				float2 uv_NormalMap = i.uv.xy * _NormalMap_ST.xy + _NormalMap_ST.zw;
				float3 tex2DNode13 = UnpackNormalScale(tex2D(_NormalMap, uv_NormalMap), 1);
				float2 appendResult30 = (float2(tex2DNode13.r, tex2DNode13.g));
				float dotResult36 = dot(appendResult30, appendResult30);
				float3 appendResult40 = (float3((_NormalIntensity * appendResult30) , sqrt((1 - saturate(dotResult36)))));
				float3 NormalMap47 = appendResult40;
				float3 normalizeResult147 = normalize(BlendNormal(WorldNormal, NormalMap47));
				float3 normalizeResult145 = normalize(_MainLightPosition.xyz);
				float dotResult146 = dot(normalizeResult147, normalizeResult145);
				float halfLambert95 = ((dotResult146 * 0.5) + 0.5);
				float2 uv_MainTeture = i.uv.xy * _MainTeture_ST.xy + _MainTeture_ST.zw;
				float4 MainColor50 = (_MainColor * tex2D(_MainTeture, uv_MainTeture));

				float2 uv_RoughnessMap = i.uv.xy * _RoughnessMap_ST.xy + _RoughnessMap_ST.zw;

				float3 Albedo = MainColor50.rgb;
				float3 Normal = NormalMap47;
				float3 Emission = _Emission.rgb;
				float3 Specular = 0.5;
				float Metallic = _Metallic;
				float Smoothness = (_Gloss * tex2D(_RoughnessMap, uv_RoughnessMap).g);
				float Occlusion = 1;
				float Alpha = 1;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				inputData.normalWS = TransformTangentToWorld(Normal, half3x3(WorldTangent, WorldBiTangent, WorldNormal));

				float3 SH = SampleSH(inputData.normalWS.xyz);

				inputData.bakedGI = SAMPLE_GI(i.lightmapUVOrVertexSH.xy, SH, inputData.normalWS);

				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.clipPos);
				inputData.shadowMask = SAMPLE_SHADOWMASK(i.lightmapUVOrVertexSH.xy);

				//改变阴影的颜色，求出阴影部分和非阴影部分，分别表示为0 和 1；
				float4 SHADOW_COORDS = TransformWorldToShadowCoord(inputData.positionWS);
				Light mainLight = GetMainLight(SHADOW_COORDS);
				half shadow_coord = mainLight.shadowAttenuation;

				half4 color = UniversalFragmentPBR(
					inputData,
					Albedo,
					Metallic,
					Specular,
					Smoothness,
					Occlusion,
					Emission,
					Alpha);

				float4 ColorResult = lerp(_ShadowMainColor * color, color, shadow_coord);

				//return color;//不需自定义阴影颜色的选项
				return ColorResult;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 clipPos : SV_POSITION;
			};

			float3 _LightDirection;

			v2f vert(a2v v)
			{
				v2f o;

				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float3 normalWS = TransformObjectToWorldDir(v.normal);

				//float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
				//===================以下范围内代码等效上面的一行代码；===================
				float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
				float scale = invNdotL * _ShadowBias.y;

				// normal bias is negative since we want to apply an inset normal offset
				positionWS = normalWS * scale.xxx + positionWS;

				float4 clipPos = mul(UNITY_MATRIX_VP, float4(positionWS, 1));
				//=====================================================================
				o.clipPos = clipPos;
				return o;
			}

			half4 frag(v2f i) : SV_TARGET
			{
				return 0;
			}
			ENDHLSL
		}


		}
}

