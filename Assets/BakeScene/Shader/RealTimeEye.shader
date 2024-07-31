Shader "NewXiRang_URP/Character/RealTimeEye"
{
    Properties
    {
        // [HideInInspector] _AlphaCutoff ("Alpha Cutoff ", Range(0, 1)) = 0.5
        [Header(Base Params)]
        _ScalebyCenter ("基于中心的缩放", Float) = 1
        _IrisRadius ("虹膜半径", Range(0, 0.5)) = 0
        _PupilScale ("瞳孔范围", Range(0, 2)) = 1
        _LimbusScale ("角膜缘范围", Float) = 2
        _LimbusPow ("角膜缘对比度", Float) = 5
        _IOR ("折射率", Float) = 1.45
        _IrisDepthScale ("虹膜深度", Float) = 1
        _MidPlaneHeightMap ("距离眼球中间平面距离贴图", 2D) = "white" { }
        _EyeDirection ("眼球朝向贴图", 2D) = "bump" { }
        _SSSLUT ("SSSLUT", 2D) = "white" { }
        [HideInInspector]_EnvRotation ("EnvRotation", Range(0, 360)) = 0
        
        [Header(Sclera)]
        _ScleraMap ("巩膜贴图", 2D) = "white" { }
        _ScleraBrightness ("巩膜强度", Float) = 1
        _ScleraColor("巩膜颜色", color) = (1,1,1)
        _ScleraNormalMap ("巩膜法向贴图", 2D) = "bump" { }
        _ScleraNormalUVScale ("巩膜法向缩放", Float) = 1
        _ScleraNormalStrength ("巩膜法向强度", Float) = 1
        _ScleraRoughness ("巩膜粗糙度", Range(0, 1)) = 0.25
        _ScleraSpecular ("巩膜高光", Range(0, 1)) = 0.25
        
        [Header(Cornea)]
        _CorneaSpecular ("角膜高光", Range(0, 1)) = 0.5
        _CorneaRoughness ("角膜粗糙度", Range(0, 1)) = 0.5
        
        [Header(Iris)]
        _IrisColorMap ("虹膜颜色贴图", 2D) = "white" { }
        _IrisBrightness ("虹膜颜色强度", Float) = 1
        _IrisColor ("虹膜颜色", Color) = (1, 1, 1, 1)
        _IrisNormalMap ("虹膜法向贴图", 2D) = "bump" { }
        _IrisNormalUVScale ("虹膜法向缩放", Float) = 1
        _IrisNormalStrength ("虹膜法向强度", Float) = 1
        _IrisConcavityScale ("虹膜凹度缩放", Range(0, 4)) = 0
        _IrisConcavityPow ("虹膜凹度强度", Range(0.1, 0.5)) = 0
        
        [Header(Speculer)]
        [Toggle(_FORGE_SPECULER_ON)]_ForgeSpeculer("打开假高光", float) = 0
        _ForgeLight("假高光位置(W没用)", Vector) =  (1, 1, 1, 0)
        _ForgeLightSize("假高光大小", Range(0.1, 10)) =  1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "5.0" }
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull[_Cull]

            HLSLPROGRAM

            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            //假高光
            #pragma shader_feature _FORGE_SPECULER_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            #include "./RealTimeEyeLighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half _ScalebyCenter;
            half _IrisRadius;
            half _PupilScale;
            half _LimbusScale;
            half _LimbusPow;
            half _IOR;
            half _IrisDepthScale;
            half _ScleraBrightness;
            half _ScleraNormalUVScale;
            half _ScleraNormalStrength;
            half _ScleraRoughness;
            half _ScleraSpecular;
            half _CorneaSpecular;
            half _CorneaRoughness;
            half _IrisBrightness;
            half _IrisNormalUVScale;
            half _IrisNormalStrength;
            half _IrisConcavityScale;
            half _IrisConcavityPow;
            half _ForgeLightSize;
            half3 _IrisColor;
            half3 _ScleraColor;
            half4 _ForgeLight;
            CBUFFER_END

            struct SkinData
            {
                float3 DiffuseColor; //漫反射颜色
                float3 SpecularColor; //高光颜色
                float Lobe0Roughness; //基础粗糙 85%
                float Lobe1Roughness; //次要粗糙 15%
                float LobeMix; //两个粗糙的混合度 默认85% 为第一层的粗糙度的占比
                float3 N; //法向
                float3 N_Blur; //模糊之后的法向
                float3 positionWS;
                float3 V; //视线方向，世界坐标系下
                Texture2D SSSLUT; //SSS贴图 固定贴图
                SamplerState sampler_SSSLUT; //SSS贴图采样器
                float Curvature; //曲率
                float ClearCoat; //涂层强度
                float ClearCoatRoughness; //涂层的粗糙度
                float3 ClearCoatNormal; //涂层的法向
                float Occlusion; //环境光遮蔽，烘焙ao和ssao取最小值
                float EnvRotation; //环境贴图旋转，一般不会修改，默认为0，值范围0-360
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;    // xyz: tangent, w: sign
                float4 shadowCoord : TEXCOORD4;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MidPlaneHeightMap);         SAMPLER(sampler_MidPlaneHeightMap);
            TEXTURE2D(_EyeDirection);              SAMPLER(sampler_EyeDirection);
            TEXTURE2D(_SSSLUT);                    SAMPLER(sampler_SSSLUT);
            TEXTURE2D(_ScleraMap);                 SAMPLER(sampler_ScleraMap);
            TEXTURE2D(_ScleraNormalMap);          SAMPLER(sampler_ScleraNormalMap);
            TEXTURE2D(_IrisColorMap);              SAMPLER(sampler_IrisColorMap);
            TEXTURE2D(_IrisNormalMap);             SAMPLER(sampler_IrisNormalMap);

            // Used in Standard (Physically Based) shader
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv = input.texcoord;
                output.normalWS = normalInput.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
                output.positionWS = vertexInput.positionWS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.positionCS = vertexInput.positionCS;

                return output;
            }

            half3 EyeLighting(
                half3 BaseColor, half Specular, half Roughness, half3 SurfaceNormalWS, half IrisMask, half3 IrisNormalWS,
                half3 causticNormalWS, half3 positionWS, half3 viewDirWS, half Occlusion, half EnvRotation = 0
                )
            {
                half3 SpecularColor = Specular * 0.08;
                //直接光漫反射和镜面反射
                half3 DirectLightColor;
                DirectLighting(BaseColor, SpecularColor, Roughness, positionWS, SurfaceNormalWS,
                viewDirWS, IrisMask, IrisNormalWS, causticNormalWS, _SSSLUT, sampler_SSSLUT, DirectLightColor, _ForgeLight.xyz,_ForgeLightSize);

                //间接光漫反射和镜面反射
                half3 IndirectLightColor;
                IndirectLighting(BaseColor, SpecularColor, Roughness, positionWS, SurfaceNormalWS, viewDirWS, Occlusion, EnvRotation, IndirectLightColor);

                // return DirectLightColor + IndirectLightColor;
                return DirectLightColor ;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                SkinData skinData;

                UNITY_SETUP_INSTANCE_ID(input);
                //---------------输入数据-----------------
                float2 UV = input.uv; //
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                //眼球uv的整体缩放
                half2 eyeUV = ScaleUVsByCenter(UV, _ScalebyCenter);

                //虹膜的贴图
                half3 scleraColor = SAMPLE_TEXTURE2D(_ScleraMap, sampler_ScleraMap, eyeUV).rgb * _ScleraBrightness *_ScleraColor;

                //IrisMask 虹膜的mask
                half dis = distance(eyeUV, half2(0.5, 0.5)); //uv和中心uv的距离
                half irisMask = smoothstep(0, 1, 1.0 - (dis - _IrisRadius + 0.045) / 0.045); //进行边缘虚化

                //进行IrisDepth 虹膜深度的计算
                half midPlaneHeight = SAMPLE_TEXTURE2D(_MidPlaneHeightMap, sampler_MidPlaneHeightMap, UV).r; //当前位置距离中心平面的距离
                half2 irisEdgeuv = half2(_ScalebyCenter * _IrisRadius + 0.5, 0.5); //虹膜边缘uv
                half irisEdgeHeight = SAMPLE_TEXTURE2D(_MidPlaneHeightMap, sampler_MidPlaneHeightMap, irisEdgeuv).r; //虹膜边缘距离中心平面距离
                half irisDepth = max(0, midPlaneHeight - irisEdgeHeight) * _IrisDepthScale; //角膜距离虹膜的距离

                //计算法向
                float sgn = input.tangentWS.w;      // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

                //眼球的朝向
                half3 eyeDir = UnpackNormal(SAMPLE_TEXTURE2D(_EyeDirection, sampler_EyeDirection, UV));
                half3 eyeDirWS = normalize(TransformTangentToWorld(eyeDir, tangentToWorld));

                //求出被角膜折射后的虹膜的uv以及深度
                half2 IrisUV;
                half IrisConcavity;
                EyeRefraction(eyeUV, input.normalWS, viewDirWS, _IOR, _IrisRadius, irisDepth, eyeDirWS, input.tangentWS.xyz, IrisUV, IrisConcavity);

                //通过折射后的uv计算出虹膜颜色
                half2 irisCircleUV = ScaleUVFromCircle(IrisUV, _PupilScale);
                half3 irisColor = SAMPLE_TEXTURE2D(_IrisColorMap, sampler_IrisColorMap, irisCircleUV).rgb * _IrisBrightness * _IrisColor;
                
                //计算角膜缘的位置
                half limbusLen = length((irisCircleUV - half2(0.5, 0.5)) * _LimbusScale);
                half limbusPow = saturate(1.0 - pow(saturate(limbusLen), _LimbusPow));
                irisColor *= limbusPow;
                
                //基础色
                half3 baseColor = lerp(scleraColor, irisColor, irisMask);

                //高光
                half specular = lerp(_ScleraSpecular, _CorneaSpecular, irisMask);

                //粗糙度
                half roughness = lerp(_ScleraRoughness, _CorneaRoughness, irisMask);

                //法向
                half2 scleraUV = ScaleUVsByCenter(UV, _ScleraNormalUVScale);
                half3 scleraNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_ScleraNormalMap, sampler_ScleraNormalMap, scleraUV), _ScleraNormalStrength);

                half3 surfaceNormal = lerp(scleraNormal, half3(0, 0, 1), irisMask);
                half3 surfaceNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surfaceNormal, tangentToWorld));

                //焦散效果
                half caustic = pow(abs(IrisConcavity * _IrisConcavityScale), _IrisConcavityPow) * irisMask;
                half3 causticNormal = normalize(lerp(eyeDirWS, -surfaceNormalWS, caustic));
                half3 causticNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(causticNormal, tangentToWorld));
                
                //虹膜的法向
                half2 irisNormalUV = ScaleUVFromCircle(IrisUV, _IrisNormalUVScale);
                half3 irisNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_IrisNormalMap, sampler_IrisNormalMap, irisNormalUV), _IrisNormalStrength);
                irisNormal = BlendNormal(irisNormal, eyeDir);
                half3 irisNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(irisNormal, tangentToWorld));

                //屏幕空间AO，需要开启renderFeature
                float2 ScreenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half ao;
                GetSSAO(ScreenUV, ao);

                //计算光照
                half3 color = EyeLighting(baseColor, specular, roughness, surfaceNormalWS, irisMask, irisNormalWS,
                causticNormalWS, input.positionWS, viewDirWS, ao);

                return half4(color, 1.0);
            }
            ENDHLSL

        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL

        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM

            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL

        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM

            // #pragma exclude_renderers gles gles3 glcore
            // #pragma target 4.5
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL

        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
