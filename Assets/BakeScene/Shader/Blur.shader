Shader "NewXiRang_URP/UI/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Iteration("Iteration",Range(1, 10)) = 1
        _BlurRadius("BlurRadius",float) = 1
//        _BlurSize ("Blur Size", Range(0, 10)) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" 
                "Queue"="Transparent"
                "IgnoreProjector"="True"
                "RenderType"="Transparent"
                "PreviewType"="Plane"
                "CanUseSpriteAtlas"="True"
            }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        LOD 100
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            

            // Texture2D _MainTex; SamplerState Sampler_MainText;
            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            half _Iteration;
            half _BlurRadius;

            float Rand(float2 n)
            {
	            return sin(dot(n, half2(1233.224, 1743.335)));
            }
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half random = Rand(i.uv);
                half2 randomOffset = float2(0.0, 0.0);
                half4 finalColor = half4(0.0, 0.0, 0.0, 0.0);
             //    for (int k = 0; k < int(_Iteration); k ++)
	            // {
		           //  random = frac(43758.5453 * random + 0.61432);;
		           //  randomOffset.x = (random - 0.5) * 2.0;
		           //  random = frac(43758.5453 * random + 0.61432);
		           //  randomOffset.y = (random - 0.5) * 2.0;
		           //  
		           //  finalColor += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, half2(i.uv + randomOffset * _BlurRadius));
	            // }

                
                float blurSize = _BlurRadius;
                [unroll]
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        float2 offset = (float2(x, y)) * blurSize ;
                        finalColor +=  SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + offset);
                    }
                }
                finalColor /= 20;
                
                // finalColor.rgb = finalColor.rgb/_Iteration;
                finalColor.a = 1;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
