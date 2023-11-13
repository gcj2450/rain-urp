Shader "Custom/Illustrative Rendering/03.Specular"
{
	Properties
	{
		_SpecularMask ("Specular Mask", 2D) = "white" {}
		_Fspec ("Fresnel Specular Term", Float)  = 1
		_Kspec ("Specular Exponent Power", Float) = 1
	}
	SubShader
	{
		Tags { 
			"RenderType"="Opaque" 
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
			};

			sampler2D _SpecularMask;
			half4 _SpecularMask_ST;
			half _Fspec;
			half _Kspec;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = TransformObjectToHClip(v.vertex.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _SpecularMask);

				half3 worldNormal = TransformObjectToWorldNormal(v.normal);
				half3 lightDir = normalize(_MainLightPosition.xyz);
				half3 reflectDir = reflect(-lightDir, worldNormal);
				half3 viewDir = normalize(GetWorldSpaceViewDir(v.vertex.xyz));
				o.VdotR = saturate(dot(viewDir, reflectDir));

				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half4 ks = tex2D( _SpecularMask, i.uv);
                half3 specularTerm = _Fspec * pow(abs(i.VdotR), _Kspec);

                half4 col=half4(0,0,0,0);
                col.rgb = _MainLightColor.rgb * ks.xyz * specularTerm;
                col.a = 1;

				return col;
			}
				ENDHLSL
		}
	}
}
