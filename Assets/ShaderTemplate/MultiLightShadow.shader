Shader "URP/MultiLightShadow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _BumpMap ("Bump Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Range(0,1)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
        _Specular ("Specular", Color) = (1, 1, 1, 1)
        [Toggle(_AdditionalLights)] _AddLights ("AddLights", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor; // 基础颜色
        sampler2D _BaseMap;
		float4 _BaseMap_ST;
        sampler2D _BumpMap; // 法线贴图
        float _BumpScale; // 法线贴图强度
        float _Metallic; // 金属度
        float4 _Smoothness;
        float4 _Diffuse;
        float4 _Specular;
        CBUFFER_END

        ENDHLSL
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            
            // 设置关键字
            #pragma shader_feature _AdditionalLights
            
            // 接收阴影所需关键字
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

      
            struct Attributes
            {
                float4 positionOS: POSITION;// 顶点位置（Object Space）
                float3 normalOS: NORMAL;// 顶点法线（Object Space）
                float4 tangentOS: TANGENT;// 顶点切线（Object Space）
                float2 uv : TEXCOORD0; // UV 坐标
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION; // 顶点位置（Clip Space）
                float3 positionWS: TEXCOORD0;
                float3 normalWS : NORMAL; // 顶点法线（World Space）
                float3 tangentWS : TANGENT; // 切线（World Space）
                float3 bitangentWS : TANGENT1; // 副切线（World Space）
                float3 viewDirWS: TEXCOORD2;
                float2 uv : TEXCOORD1; // UV 坐标
            };


            Varyings vert(Attributes v)
            {
                Varyings o;
                // 获取不同空间下坐标信息
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;
                
                // 获取世界空间下法线相关向量
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                
                o.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                o.tangentWS = NormalizeNormalPerVertex(normalInput.tangentWS);
                o.bitangentWS = NormalizeNormalPerVertex(normalInput.bitangentWS);

                o.viewDirWS = GetCameraPositionWS() - positionInputs.positionWS;
                o.uv.xy = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            
            /// lightColor：光源颜色
            /// lightDirectionWS：世界空间下光线方向
            /// lightAttenuation：光照衰减
            /// normalWS：世界空间下法线
            /// viewDirectionWS：世界空间下视角方向
            half3 LightingBased(half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS,float3 albedo)
            {
                // 兰伯特漫反射计算
                half NdotL = saturate(dot(normalWS, lightDirectionWS));
                half3 radiance = lightColor * (lightAttenuation * NdotL) * _Diffuse.rgb*albedo;
                // BlinnPhong高光反射
                half3 halfDir = normalize(lightDirectionWS + viewDirectionWS);
                half3 specular = lightColor * pow(saturate(dot(normalWS, halfDir)), _Smoothness) * _Specular.rgb*_Metallic;
                
                return radiance + specular;
            }
            
            half3 LightingBased(Light light, half3 normalWS, half3 viewDirectionWS,float3 albedo)
            {
                // 注意light.distanceAttenuation * light.shadowAttenuation，这里已经将距离衰减与阴影衰减进行了计算
                return LightingBased(light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS,albedo);
            }
            
            half4 frag(Varyings i): SV_Target
            {
                half3 normalWS = NormalizeNormalPerPixel(i.viewDirWS);
                half3 viewDirWS = SafeNormalize(i.normalWS);
                
                  // 获取阴影坐标
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS.xyz);
    
                float4 baseColor = tex2D(_BaseMap, i.uv);
                float3 albedo = baseColor.rgb * _BaseColor.rgb;

                // 使用HLSL的函数获取主光源数据
                Light mainLight = GetMainLight(shadowCoord);
                half3 diffuse = LightingBased(mainLight, normalWS, viewDirWS,albedo);
                
                // 计算其他光源
                #ifdef _AdditionalLights
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++ lightIndex)
                    {
                        // 获取其他光源
                        Light light = GetAdditionalLight(lightIndex, i.positionWS);
                        diffuse += LightingBased(light, normalWS, viewDirWS,albedo);
                    }
                #endif
                
                

                half3 ambient = SampleSH(normalWS);
                return half4(ambient + diffuse, 1.0);
            }
            
            ENDHLSL
            
        }
        
        //下面计算阴影的Pass可以直接通过使用URP内置的Pass计算
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        // or
        // 计算阴影的Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            
            // 设置关键字
            #pragma shader_feature _ALPHATEST_ON
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection;
            
            struct Attributes
            {
                float4 positionOS: POSITION;
                float3 normalOS: NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS: SV_POSITION;
            };            
            
            // 获取裁剪空间下的阴影坐标
            float4 GetShadowPositionHClips(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                // 获取阴影专用裁剪空间下的坐标
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                // 判断是否是在DirectX平台翻转过坐标
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetShadowPositionHClips(input);
                return output;
            }

       
            half4 frag(Varyings input): SV_TARGET
            {
                return 0;
            }
            
            ENDHLSL
            
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
