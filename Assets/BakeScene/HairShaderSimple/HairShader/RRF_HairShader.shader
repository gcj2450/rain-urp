Shader "RRF_HumanShaders/HairShader1/RRF_HairShader" {
    Properties {
        _BaseColorGloss ("BaseColorGloss", Color) = (0.5,0.5,0.5,1)
        _ShineColor_ToneBoost ("ShineColor_ToneBoost", Color) = (0.5,0.5,0.5,1)
        _HairsNormal ("HairsNormal", 2D) = "bump" {}
        _AlphaClip ("AlphaClip", 2D) = "white" {}
        _NormalShift ("NormalShift", Color) = (0,0.5,0,0)
        _HairSpecCol_Mixing ("HairSpecCol_Mixing", Float ) = 1.25
        _HairToneVariation ("HairToneVariation", 2D) = "white" {}
        _Gloss_Variation ("Gloss_Variation", Float ) = 0.5
        _TipColor_Mix ("TipColor_Mix", Color) = (0.5,0.5,0.5,1)
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        sampler2D _HairsNormal;
        float4 _HairsNormal_ST;
        sampler2D _AlphaClip;
        float4 _AlphaClip_ST;
        sampler2D _HairToneVariation;
        float4 _HairToneVariation_ST;

        half4 _BaseColorGloss, _ShineColor_ToneBoost,
            _NormalShift,_TipColor_Mix;

        half _HairSpecCol_Mixing;
        half _Gloss_Variation;
        half _Cutoff;
        CBUFFER_END
        ENDHLSL

        Pass {
            //Name "FORWARD"
            Tags {
                "LightMode" = "UniversalForward"
            }
            Cull Off
            
            Offset 0, 1
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #define _GLOSSYENV 1

            #define UNITY_INV_PI 0.31830988618f
#define UNITY_PI 3.14159265359f

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            //#include "UnityCG.cginc"
            //#include "AutoLight.cginc"
            //#include "UnityPBSLighting.cginc"
            //#include "UnityStandardBRDF.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            //#pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 3.0
            /*uniform float4 _NormalShift;
            uniform float4 _BaseColorGloss;
            uniform sampler2D _HairsNormal; uniform float4 _HairsNormal_ST;
            uniform sampler2D _AlphaClip; uniform float4 _AlphaClip_ST;
            uniform float4 _ShineColor_ToneBoost;
            uniform float _HairSpecCol_Mixing;
            uniform sampler2D _HairToneVariation; uniform float4 _HairToneVariation_ST;
            uniform float _Gloss_Variation;
            uniform float4 _TipColor_Mix;*/
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 bitangentDir : TEXCOORD4;
                //LIGHTING_COORDS(5,6)
                //UNITY_FOG_COORDS(7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = TransformObjectToWorldNormal(v.normal);
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.bitangentDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _MainLightColor.rgb;
                o.pos = TransformObjectToHClip( v.vertex.xyz );
                //UNITY_TRANSFER_FOG(o,o.pos);
                //TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }

            half3 EnergyConservationBetweenDiffuseAndSpecular(
                half3 albedo, half3 specColor, out half oneMinusReflectivity
            ) {
                oneMinusReflectivity = 1 - max(max(specColor.r, specColor.g), specColor.b);;
#if !UNITY_CONSERVE_ENERGY
                return albedo;
#elif UNITY_CONSERVE_ENERGY_MONOCHROME
                return albedo * oneMinusReflectivity;
#else
                return albedo * (half3(1, 1, 1) - specColor);
#endif
            }


            half SmithJointGGXVisibilityTerm(half NdotL, half NdotV, half roughness)
            {
                half a = roughness;
                half lambdaV = NdotL * (NdotV * (1 - a) + a);
                half lambdaL = NdotV * (NdotL * (1 - a) + a);
                return 0.5f / (lambdaV + lambdaL + 1e-5f);
            }

            float GGXTerm(float NdotH, float roughness)
            {
                float a2 = roughness * roughness;
                float d = (NdotH * a2 - NdotH) * NdotH + 1.0f;
                return UNITY_INV_PI * a2 / (d * d + 1e-7f);
            }

            half3 Pow5(half3 x)
            {
                return x * x * x * x * x;
            }

            half3 FresnelTerm(half3 F0, half cosA)
            {
                half t = Pow5(1 - cosA);   // ala Schlick interpoliation
                return F0 + (1 - F0) * t;
            }

            half3 FresnelLerp(half3 F0, half3 F90, half cosA)
            {
                half t = Pow5(1 - cosA);   // ala Schlick interpoliation
                return lerp(F0, F90, t);
            }

            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {

                float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.posWorld);
                Light mainLight = GetMainLight(SHADOW_COORDS);

                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
                i.normalDir = normalize(i.normalDir);
                i.normalDir *= faceSign;
                float3x3 tangentTransform = float3x3( i.tangentDir, i.bitangentDir, i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 _HairsNormal_var = UnpackNormal(tex2D(_HairsNormal,TRANSFORM_TEX(i.uv0, _HairsNormal)));
                float3 node_5355_nrm_base = _NormalShift.rgb + float3(0,0,1);
                float3 node_5355_nrm_detail = _HairsNormal_var.rgb * float3(-1,-1,1);
                float3 node_5355_nrm_combined = node_5355_nrm_base*dot(node_5355_nrm_base, node_5355_nrm_detail)/node_5355_nrm_base.z - node_5355_nrm_detail;
                float3 node_5355 = node_5355_nrm_combined;
                float3 normalLocal = normalize((node_5355*node_5355*node_5355));
                float3 normalDirection = normalize(mul( normalLocal, tangentTransform )); // Perturbed normals
                float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
                float4 _AlphaClip_var = tex2D(_AlphaClip,TRANSFORM_TEX(i.uv0, _AlphaClip));
                clip(_AlphaClip_var.r - 0.5);
                float3 lightDirection = normalize(_MainLightPosition.xyz);
                float3 lightColor = _MainLightColor.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = mainLight.distanceAttenuation;
                float3 attenColor = attenuation * _MainLightColor.xyz;
                float Pi = 3.141592654;
                float InvPi = 0.31830988618;
///////// Gloss:
                float4 _HairToneVariation_var = tex2D(_HairToneVariation,TRANSFORM_TEX(i.uv0, _HairToneVariation));
                float gloss = (_BaseColorGloss.a*(_HairToneVariation_var.r*_Gloss_Variation));
                float perceptualRoughness = 1.0 - (_BaseColorGloss.a*(_HairToneVariation_var.r*_Gloss_Variation));
                float roughness = perceptualRoughness * perceptualRoughness;
                float specPow = exp2( gloss * 10.0 + 1.0 );
/////// GI Data:
              /*  UnityLight light;
                #ifdef LIGHTMAP_OFF
                    light.color = lightColor;
                    light.dir = lightDirection;
                    light.ndotl = LambertTerm (normalDirection, light.dir);
                #else
                    light.color = half3(0.f, 0.f, 0.f);
                    light.ndotl = 0.0f;
                    light.dir = half3(0.f, 0.f, 0.f);
                #endif
                UnityGIInput d;
                d.light = light;
                d.worldPos = i.posWorld.xyz;
                d.worldViewDir = viewDirection;
                d.atten = attenuation;
                #if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
                    d.boxMin[0] = unity_SpecCube0_BoxMin;
                    d.boxMin[1] = unity_SpecCube1_BoxMin;
                #endif
                #if UNITY_SPECCUBE_BOX_PROJECTION
                    d.boxMax[0] = unity_SpecCube0_BoxMax;
                    d.boxMax[1] = unity_SpecCube1_BoxMax;
                    d.probePosition[0] = unity_SpecCube0_ProbePosition;
                    d.probePosition[1] = unity_SpecCube1_ProbePosition;
                #endif
                d.probeHDR[0] = unity_SpecCube0_HDR;
                d.probeHDR[1] = unity_SpecCube1_HDR;
                Unity_GlossyEnvironmentData ugls_en_data;
                ugls_en_data.roughness = 1.0 - gloss;
                ugls_en_data.reflUVW = viewReflectDirection;
                UnityGI gi = UnityGlobalIllumination(d, 1, normalDirection, ugls_en_data );
                lightDirection = gi.light.dir;
                lightColor = gi.light.color;*/
////// Specular:
                float NdotL = saturate(dot( normalDirection, lightDirection ));
                float LdotH = saturate(dot(lightDirection, halfDirection));
                float3 specularColor = (_ShineColor_ToneBoost.rgb*((_HairToneVariation_var.rgb*(_ShineColor_ToneBoost.rgb*_ShineColor_ToneBoost.a))*clamp(_HairSpecCol_Mixing,0,10)));
                float specularMonochrome;
                float3 diffuseColor = lerp(_BaseColorGloss.rgb,_TipColor_Mix.rgb,((1.0 - i.uv0.g)*_TipColor_Mix.a)); // Need this for specular when using metallic
                diffuseColor = EnergyConservationBetweenDiffuseAndSpecular(diffuseColor, specularColor, specularMonochrome);
                specularMonochrome = 1.0-specularMonochrome;
                float NdotV = abs(dot( normalDirection, viewDirection ));
                float NdotH = saturate(dot( normalDirection, halfDirection ));
                float VdotH = saturate(dot( viewDirection, halfDirection ));
                float visTerm = SmithJointGGXVisibilityTerm( NdotL, NdotV, roughness );
                float normTerm = GGXTerm(NdotH, roughness);
                float specularPBL = (visTerm*normTerm) * UNITY_PI;
                #ifdef UNITY_COLORSPACE_GAMMA
                    specularPBL = sqrt(max(1e-4h, specularPBL));
                #endif
                specularPBL = max(0, specularPBL * NdotL);
                #if defined(_SPECULARHIGHLIGHTS_OFF)
                    specularPBL = 0.0;
                #endif
                half surfaceReduction;
                #ifdef UNITY_COLORSPACE_GAMMA
                    surfaceReduction = 1.0-0.28*roughness*perceptualRoughness;
                #else
                    surfaceReduction = 1.0/(roughness*roughness + 1.0);
                #endif
                specularPBL *= any(specularColor) ? 1.0 : 0.0;
                float3 directSpecular = attenColor*specularPBL*FresnelTerm(specularColor, LdotH);
                half grazingTerm = saturate( gloss + specularMonochrome );

               
                half3 Indiffuse = SampleSH(i.normalDir);

                float3 worldLightDir = mainLight.direction;
                float3 reflectDir = normalize(reflect(-worldLightDir, i.normalDir));

                half4 envCol = SAMPLE_TEXTURECUBE(unity_SpecCube0, samplerunity_SpecCube0, reflectDir);
                half3 envHDRCol = DecodeHDREnvironment(envCol, unity_SpecCube0_HDR);


                float3 indirectSpecular = envHDRCol;// (gi.indirect.specular);
                indirectSpecular *= FresnelLerp (specularColor, grazingTerm, NdotV);
                indirectSpecular *= surfaceReduction;
                float3 specular = (directSpecular + indirectSpecular);
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                half fd90 = 0.5 + 2 * LdotH * LdotH * (1-gloss);
                float nlPow5 = Pow5(1-NdotL);
                float nvPow5 = Pow5(1-NdotV);
                float3 directDiffuse = ((1 +(fd90 - 1)*nlPow5) * (1 + (fd90 - 1)*nvPow5) * NdotL) * attenColor;
                float3 indirectDiffuse = float3(0,0,0);
                indirectDiffuse += UNITY_LIGHTMODEL_AMBIENT.rgb; // Ambient Light
                diffuseColor *= 1-specularMonochrome;
                float3 diffuse = (directDiffuse + indirectDiffuse) * diffuseColor;
/// Final Color:
                float3 finalColor = diffuse + specular;
                half4 finalRGBA = half4(finalColor,1);
                //UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
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
                o.uv = TRANSFORM_TEX(v.uv, _AlphaClip);
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                #if _ALPHATEST_ON
                half4 col = tex2D(_AlphaClip, i.uv);
                clip(col.a - 0.001);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
    //CustomEditor "ShaderForgeMaterialInspector"
}
