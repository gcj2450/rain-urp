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
        float4 _BaseColor; // ������ɫ
        sampler2D _BaseMap;
		float4 _BaseMap_ST;
        sampler2D _BumpMap; // ������ͼ
        float _BumpScale; // ������ͼǿ��
        float _Metallic; // ������
        float4 _Smoothness;
        float4 _Diffuse;
        float4 _Specular;
        CBUFFER_END

        ENDHLSL
        
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            
            // ���ùؼ���
            #pragma shader_feature _AdditionalLights
            
            // ������Ӱ����ؼ���
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

      
            struct Attributes
            {
                float4 positionOS: POSITION;// ����λ�ã�Object Space��
                float3 normalOS: NORMAL;// ���㷨�ߣ�Object Space��
                float4 tangentOS: TANGENT;// �������ߣ�Object Space��
                float2 uv : TEXCOORD0; // UV ����
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION; // ����λ�ã�Clip Space��
                float3 positionWS: TEXCOORD0;
                float3 normalWS : NORMAL; // ���㷨�ߣ�World Space��
                float3 tangentWS : TANGENT; // ���ߣ�World Space��
                float3 bitangentWS : TANGENT1; // �����ߣ�World Space��
                float3 viewDirWS: TEXCOORD2;
                float2 uv : TEXCOORD1; // UV ����
            };


            Varyings vert(Attributes v)
            {
                Varyings o;
                // ��ȡ��ͬ�ռ���������Ϣ
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;
                
                // ��ȡ����ռ��·����������
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                
                o.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                o.tangentWS = NormalizeNormalPerVertex(normalInput.tangentWS);
                o.bitangentWS = NormalizeNormalPerVertex(normalInput.bitangentWS);

                o.viewDirWS = GetCameraPositionWS() - positionInputs.positionWS;
                o.uv.xy = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            
            /// lightColor����Դ��ɫ
            /// lightDirectionWS������ռ��¹��߷���
            /// lightAttenuation������˥��
            /// normalWS������ռ��·���
            /// viewDirectionWS������ռ����ӽǷ���
            half3 LightingBased(half3 lightColor, half3 lightDirectionWS, half lightAttenuation, half3 normalWS, half3 viewDirectionWS,float3 albedo)
            {
                // ���������������
                half NdotL = saturate(dot(normalWS, lightDirectionWS));
                half3 radiance = lightColor * (lightAttenuation * NdotL) * _Diffuse.rgb*albedo;
                // BlinnPhong�߹ⷴ��
                half3 halfDir = normalize(lightDirectionWS + viewDirectionWS);
                half3 specular = lightColor * pow(saturate(dot(normalWS, halfDir)), _Smoothness) * _Specular.rgb*_Metallic;
                
                return radiance + specular;
            }
            
            half3 LightingBased(Light light, half3 normalWS, half3 viewDirectionWS,float3 albedo)
            {
                // ע��light.distanceAttenuation * light.shadowAttenuation�������Ѿ�������˥������Ӱ˥�������˼���
                return LightingBased(light.color, light.direction, light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS,albedo);
            }
            
            half4 frag(Varyings i): SV_Target
            {
                half3 normalWS = NormalizeNormalPerPixel(i.viewDirWS);
                half3 viewDirWS = SafeNormalize(i.normalWS);
                
                  // ��ȡ��Ӱ����
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS.xyz);
    
                float4 baseColor = tex2D(_BaseMap, i.uv);
                float3 albedo = baseColor.rgb * _BaseColor.rgb;

                // ʹ��HLSL�ĺ�����ȡ����Դ����
                Light mainLight = GetMainLight(shadowCoord);
                half3 diffuse = LightingBased(mainLight, normalWS, viewDirWS,albedo);
                
                // ����������Դ
                #ifdef _AdditionalLights
                    uint pixelLightCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++ lightIndex)
                    {
                        // ��ȡ������Դ
                        Light light = GetAdditionalLight(lightIndex, i.positionWS);
                        diffuse += LightingBased(light, normalWS, viewDirWS,albedo);
                    }
                #endif
                
                

                half3 ambient = SampleSH(normalWS);
                return half4(ambient + diffuse, 1.0);
            }
            
            ENDHLSL
            
        }
        
        //���������Ӱ��Pass����ֱ��ͨ��ʹ��URP���õ�Pass����
        //UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        // or
        // ������Ӱ��Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            
            // ���ùؼ���
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
            
            // ��ȡ�ü��ռ��µ���Ӱ����
            float4 GetShadowPositionHClips(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                // ��ȡ��Ӱר�òü��ռ��µ�����
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                // �ж��Ƿ�����DirectXƽ̨��ת������
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
