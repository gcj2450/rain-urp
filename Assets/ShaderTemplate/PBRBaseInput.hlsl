#ifndef UNIVERSAL_FORWARD_BASEINPUT
#define UNIVERSAL_FORWARD_BASEINPUT


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
half4 _BaseMap_ST;
half4 _BaseColor;
half4 _EmissionColor;
half _MatCapBlend;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Cutoff;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_MetallicGlossMap);
SAMPLER(sampler_MetallicGlossMap);

TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);

// TEXTURE2D(_EmissionMap);
// SAMPLER(sampler_EmissionMap);
            
// TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap);

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
    #ifdef _NORMALMAP
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    #if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(n);
    #else
    return UnpackNormalScale(n, scale);
    #endif
    #else
    return half3(0.0h, 0.0h, 1.0h);
    #endif
}

half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
{
    //呼吸灯
    #ifdef _BREATHING_ON
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * max(1,((sin(_Time.y * _BreathingTime)+1) * _BreathingColor));
    #endif
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor ;
    
}


half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
    half alpha = albedoAlpha * color.a;
    #else
    half alpha = color.a;
    #endif

    #if defined(_ALPHATEST_ON)
    clip(alpha - cutoff);
    #endif

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

            
void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    
              
    half4 albedoAlpha = half4(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv));
    #if UNITY_COLORSPACE_GAMMA
    albedoAlpha= SRGBToLinear(albedoAlpha);
    #endif
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);            
    // outSurfaceData.alpha = albedoAlpha.a * _BaseColor.a;
                
    #if defined(_ALPHATEST_ON)
    clip(outSurfaceData.alpha - _Cutoff);
    #endif
                
                
    half4 specGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    #if UNITY_COLORSPACE_GAMMA
    specGloss= SRGBToLinear(specGloss);
    #endif
    half3 n = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                
    outSurfaceData.metallic = specGloss.g * _Metallic;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.normalTS = n;
    outSurfaceData.smoothness = 1- (specGloss.r *2.5* _Smoothness);//*2.5使unity内的效果和SP内效果接近
    #ifdef _EMISSION
        // outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
        outSurfaceData.emission = specGloss.b * _EmissionColor.rgb;
    #else
        outSurfaceData.emission = half(0);
    #endif
    
}



#endif
