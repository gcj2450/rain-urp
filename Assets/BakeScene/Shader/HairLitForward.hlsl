#ifndef UNIVERSAL_FORWARD_HAIRLIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_HAIRLIT_PASS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"


//这些田间的参数需要放到Cbuffer里面才能使用URP的合并

//CBUFFER_START(UnityPerMaterial)

float _BackDistortion;
float _FrontInte;
float4 _ScattColor;

float4 _Specular1Color;
float _Specular1;
float _SpecOffset1;
float _SpecNoise1;
float _SpecularExponent1;

float4 _Specular2Color;
float _Specular2;
float _SpecOffset2;
float _SpecNoise2;
float _SpecularExponent2;

float4 _ShiftMap_ST;

float4 _AddHairskewingNoise_ST;

float _BackIntansity;
float _AddHairAlpha;
float _AddHairskewing;

//CBUFFER_END

TEXTURE2D(_TranslucentAreaMap);
SAMPLER(sampler_TranslucentAreaMap);

TEXTURE2D(_ShiftMap);
SAMPLER(sampler_ShiftMap);

TEXTURE2D(_AddHairskewingNoise);
SAMPLER(sampler_AddHairskewingNoise);

//判断灯光层
Light GetDirectionLight(InputData inputData)
{
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    Light dirLight = GetMainLight();

    if (IsMatchingLightLayer(dirLight.layerMask, meshRenderingLayers))
    {
        return dirLight;
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers) && light.distanceAttenuation > 0.999999)
        {
            dirLight = light;
            return dirLight;
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);

        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers) && light.distanceAttenuation > 0.999999)
        {
            dirLight = light;
            return dirLight;
        }
    LIGHT_LOOP_END
    #endif

    return dirLight;
}


inline float SubsurfaceScattering(float3 vDir, float3 lDir, float3 nDir, float distorion)
{
    float3 backDir = nDir * distorion + lDir;
    backDir = normalize(backDir);
    float result = saturate(dot(vDir, -backDir));
    return result;
}


Varyings BSDFPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
    fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    #endif
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
    #endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    #ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    #else
    output.fogFactor = fogFactor;
    #endif

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    output.positionCS = vertexInput.positionCS;
    output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    return output;
}


Varyings BSDFPassAddVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    //括出来一部分，用GetVertexPositionInputs方法里面TransformWorldToHClip不行
    input.positionOS.xyz += input.normalOS * _AddHairskewing/100;
    output.positionCS = TransformObjectToHClip( input.positionOS);
    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    return output;
}


half4 BSDFFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_PARALLAXMAP)
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
        half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
    #endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
    #endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    surfaceData.smoothness = 0.01;
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);


    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif
    // float4 shadowMask;
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    Light light = GetDirectionLight(inputData);
    float3 lightDirWS = light.direction;
    half3 N = input.normalWS.xyz;
    half3 T = input.tangentWS.xyz;
    half3 V = SafeNormalize(input.viewDirWS);
    half3 H = SafeNormalize(lightDirWS + V);
    half3 B = SafeNormalize(input.tangentWS.z * cross(input.normalWS.xyz, input.tangentWS.xyz));
    float2 anisoNoise_uv = TRANSFORM_TEX(input.uv, _ShiftMap);
    // half anisoNoise = SAMPLE_TEXTURE2D(ShiftMap, sampler_ShiftMap, anisoNoise_uv).r;
    half anisoNoise = SAMPLE_TEXTURE2D(_ShiftMap, sampler_ShiftMap, anisoNoise_uv).r;


    //KK高光1
    float3 t1 = ShiftTangent(B, N, (anisoNoise + _SpecOffset1) * _SpecNoise1);
    half3 KKSpecular1 = color.rbg * max(
        D_KajiyaKay(t1, H, _SpecularExponent1) * _Specular1 * _Smoothness * _Specular1Color, 0);
    //KK高光2
    float3 t2 = ShiftTangent(B, N, (anisoNoise + _SpecOffset2) * _SpecNoise2);
    half3 KKSpecular2 = color.rbg * max(
        D_KajiyaKay(t2, H, _SpecularExponent2) * _Specular2 * _Smoothness * _Specular2Color, 0);


    //  投射颜色
    float translucentArea = SAMPLE_TEXTURE2D(_TranslucentAreaMap, sampler_TranslucentAreaMap, input.uv);
    float backd = SubsurfaceScattering(inputData.viewDirectionWS, light.direction, inputData.normalWS,
                                       _BackDistortion * 10);
    float frontd = SubsurfaceScattering(inputData.viewDirectionWS, -light.direction, inputData.normalWS,
                                        _BackDistortion * 10);

    frontd = frontd * translucentArea * _FrontInte;
    
    float3 sssColor = lerp(float3(0, 0, 0), light.color * _ScattColor, frontd);
    
    // color.rgb = color.rgb + KKSpecular1 + KKSpecular2 + sssColor;//效果不好不要了
    
    color.rgb = color.rgb + KKSpecular1 + KKSpecular2 ;


    color.a = OutputAlpha(color.a, _Surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}
half4 BSDFAddFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_PARALLAXMAP)
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
        half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
    #endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
    #endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    surfaceData.smoothness = 0.01;
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);


    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif
    // float4 shadowMask;
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    Light light = GetDirectionLight(inputData);
    float3 lightDirWS = light.direction;
    half3 N = input.normalWS.xyz;
    half3 T = input.tangentWS.xyz;
    half3 V = SafeNormalize(input.viewDirWS);
    half3 H = SafeNormalize(lightDirWS + V);
    half3 B = SafeNormalize(input.tangentWS.z * cross(input.normalWS.xyz, input.tangentWS.xyz));
    float2 anisoNoise_uv = TRANSFORM_TEX(input.uv, _ShiftMap);
    // half anisoNoise = SAMPLE_TEXTURE2D(ShiftMap, sampler_ShiftMap, anisoNoise_uv).r;
    half anisoNoise = SAMPLE_TEXTURE2D(_ShiftMap, sampler_ShiftMap, anisoNoise_uv).r;


    //KK高光1
    float3 t1 = ShiftTangent(B, N, (anisoNoise + _SpecOffset1) * _SpecNoise1);
    half3 KKSpecular1 = color.rbg * max(
        D_KajiyaKay(t1, H, _SpecularExponent1) * _Specular1 * _Smoothness * _Specular1Color, 0);
    //KK高光2
    float3 t2 = ShiftTangent(B, N, (anisoNoise + _SpecOffset2) * _SpecNoise2);
    half3 KKSpecular2 = color.rbg * max(
        D_KajiyaKay(t2, H, _SpecularExponent2) * _Specular2 * _Smoothness * _Specular2Color, 0);


    //  投射颜色
    float translucentArea = SAMPLE_TEXTURE2D(_TranslucentAreaMap, sampler_TranslucentAreaMap, input.uv);
    float backd = SubsurfaceScattering(inputData.viewDirectionWS, light.direction, inputData.normalWS,
                                       _BackDistortion * 10);
    float frontd = SubsurfaceScattering(inputData.viewDirectionWS, -light.direction, inputData.normalWS,
                                        _BackDistortion * 10);

    frontd = frontd * translucentArea * _FrontInte;
    // color.rgb = color.rgb + KKSpecular1 + KKSpecular2 + sssColor;//效果不好不要了
    
    color.rgb = color.rgb + KKSpecular1 + KKSpecular2 ;


    color.a = OutputAlpha(color.a  ,_Surface );
    color.a*=_AddHairAlpha;
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;

}

half4 BSDFFragmentBack(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_PARALLAXMAP)
    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
    #else
        half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
        half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
    #endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
    #endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    surfaceData.smoothness = 0.01;
    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);


    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif
    // float4 shadowMask;
    half4 color = UniversalFragmentPBR(inputData, surfaceData);

    color.rgb = color.rgb * _BackIntansity;

    color.a = OutputAlpha(color.a, _Surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    return color;
}


#endif
