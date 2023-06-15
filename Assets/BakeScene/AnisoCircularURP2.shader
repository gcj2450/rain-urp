
Shader "Hair Shader/mobile/AnisoCircularURP2" 
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
		_Atten ( "Atten", float) = 1.0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2
	}
	
	SubShader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="TransparentCutout" "RenderPipeline" = "UniversalPipeline"}

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half4 _Color;

			half _Cutoff;
			
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
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				//UNITY_INITIALIZE_OUTPUT(v2f,o);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				o.worldNormal = TransformObjectToWorldNormal(v.normal);  
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 albedo = tex2D(_MainTex, i.uv);
				clip(albedo.a -_Cutoff);
				
				half4 finalColor = half4(0, 0, 0, albedo.a);
				finalColor.rgb += (albedo.rgb * _Color.rgb) * _MainLightColor.rgb;
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
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#pragma target 3.0

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half _AnisoOffset,_SpecularMultiplier,_Gloss;
			half4 _SpecularColor, _Color;
			half _Atten;
			half _Cutoff;
			
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
			};

			v2f vert (appdata_full v)
			{
				v2f o;
				//UNITY_INITIALIZE_OUTPUT(v2f,o);
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				o.worldNormal = TransformObjectToWorldNormal(v.normal);  
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 albedo = tex2D(_MainTex, i.uv);
				//Light mainLight = GetMainLight();
				half3 worldNormal = normalize(i.worldNormal);	
				half3 worldLightDir = normalize(_MainLightPosition.xyz);
				half NdotL = saturate(dot(worldNormal, worldLightDir)); 

				half aniso = max(0, sin(radians((NdotL + _AnisoOffset) * 180)));
				
				aniso = pow( aniso, _Gloss);
				aniso = aniso * _SpecularMultiplier;
				
				half4 c;
				c.rgb = ((albedo.rgb* _Color.rgb) + (aniso * _SpecularColor.rgb)) + (_Atten * _MainLightColor.rgb * NdotL);
				c.a = albedo.a;
				
				return c;
			};
			ENDHLSL
		}

	}

	FallBack "Transparent/Cutout/VertexLit"
}