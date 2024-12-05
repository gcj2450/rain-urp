#ifndef UNIVERSAL_PBR_INPUT_INCLUDED
#define UNIVERSAL_PBR_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _EmissionMap_ST;
    float4 _BumpMap_ST;
    float4 _MetallicGlossMap_ST;
    float4 _OcclusionMap_ST;
    float4 _DetailAlbedoMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;
    half4 _RimColor;
    half4 _RimDirection;
    half4 _ReflectionColor;
    // half4 _BreathingColor;
    half _Cutoff;
    half _Smoothness;
    half _Metallic;
    half _BumpScale;
    half _OcclusionStrength;
    half _Surface;
    half _Contrast;
    half _Saturation;
    half _RimIntensity;
    half _RimAmount;
    half _RimContrast;
    half _ReflectionAmount;
    half _BreathingTime;
    float4 _BaseMap_TexelSize;
    float4 _BaseMap_MipInfo;
    float4 _ReflectionMap_HDR;

    half _PlaneReflectionValue;

CBUFFER_END

//使用平面反射
#if defined(_OPEN_PLANNAR_REFLECTION)
TEXTURE2D(_PlannarReflectTex);
SAMPLER(sampler_PlannarReflectTex);
#endif

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
// TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
TEXTURECUBE(_ReflectionMap);    SAMPLER(sampler_ReflectionMap);

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                            //
///////////////////////////////////////////////////////////////////////////////
half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    half alpha = albedoAlpha * color.a;

    #if defined(_ALPHATEST_ON)
        clip(alpha - cutoff);
    #endif

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

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
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * (((sin(_Time.y * _BreathingTime)+1) * emissionColor));
    #endif
    return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor ;
    
}
half3 SampleEmission(float emission,half3 emissionColor ,half3 albedo)
{
    //呼吸灯
    #ifdef _BREATHING_ON
    return emission * ((sin(_Time.y * _BreathingTime)+1) * emissionColor)*albedo;
    #endif
    return emission * emissionColor *albedo;
    
}


half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    specGloss.r *= _Metallic;
    specGloss.a *= _Smoothness;

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
    #if defined(SHADER_API_GLES)
        return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    #else
        half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
        return LerpWhiteTo(occ, _OcclusionStrength);
    #endif
}

half3 Contrast(half3 color){
    //设置对比度
    color.rgb -= 0.5;
    color.rgb *= _Contrast;
    color.rgb += 0.5;
    return color;
}

half3 Saturation(half3 color){
    //设置饱和度
    half3 gray = dot(color, half3(0.299, 0.587, 0.114));
    return lerp(gray, color, _Saturation);
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    // SRGBToLinear()
    #if UNITY_COLORSPACE_GAMMA
    albedoAlpha = SRGBToLinear(albedoAlpha);
    #endif
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    albedoAlpha.rgb = Contrast(albedoAlpha.rgb);
    albedoAlpha.rgb = Saturation(albedoAlpha.rgb);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    float2 m_uv = (uv.xy * _MetallicGlossMap_ST.xy) +_MetallicGlossMap_ST.zw;
    half4 specGloss = SampleMetallicSpecGloss(m_uv, albedoAlpha.a);
    #if UNITY_COLORSPACE_GAMMA
    specGloss= SRGBToLinear(specGloss);
    #endif
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
    outSurfaceData.smoothness = specGloss.a;
    float2 b_uv = (uv.xy * _BumpMap_ST.xy) +_BumpMap_ST.zw;
    outSurfaceData.normalTS = SampleNormal(b_uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    float2 o_uv = (uv.xy * _OcclusionMap_ST.xy) +_OcclusionMap_ST.zw;
    outSurfaceData.occlusion = SampleOcclusion(o_uv);
    float2 e_uv = (uv.xy * _EmissionMap_ST.xy) +_EmissionMap_ST.zw;
    // outSurfaceData.emission = SampleEmission(e_uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    outSurfaceData.emission = SampleEmission(specGloss.b, _EmissionColor.rgb,albedoAlpha.rgb);

    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);

}

#endif
