#ifndef UNIVERSAL_FORWARD_LITANDMATCAPFORWARD_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LITANDMATCAPFORWARD_PASS_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"


//不添加宏命令会报错
// We either sample GI from baked lightmap or from probes.
// If lightmap: sampleData.xy = lightmapUV
// If probe: sampleData.xyz = L2 SH terms
#if defined(LIGHTMAP_ON) && defined(DYNAMICLIGHTMAP_ON)
    #define SAMPLE_GI_TWO(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(staticLmName, dynamicLmName, normalWSName)
#elif defined(DYNAMICLIGHTMAP_ON)
    #define SAMPLE_GI_TWO(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(0, dynamicLmName, normalWSName)
#elif defined(LIGHTMAP_ON)
    #define SAMPLE_GI_TWO(staticLmName, shName, normalWSName) SampleLightmap2(staticLmName, 0, normalWSName)
#else
    #define SAMPLE_GI_TWO(staticLmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif




struct LitAndMatcapVaryings
{
    float2 uv                       : TEXCOORD0;

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
    #endif

    float3 normalWS                 : TEXCOORD2;
    
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
    #endif
    
    float3 viewDirWS                : TEXCOORD4;

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
    #else
    half  fogFactor                 : TEXCOORD5;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
    #ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
    #endif

    
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};




half MainLightRealtimeShadow2(float4 shadowCoord)
{
    // #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    // return half(1.0);
    // #elif defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    // return SampleScreenSpaceShadowmap(shadowCoord);
    // #else
    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();
    return SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoord, shadowSamplingData, shadowParams, false);
    // #endif
}



//
// half MainLightShadow2(float4 shadowCoord, float3 positionWS, half4 shadowMask, half4 occlusionProbeChannels)
// {
//     half realtimeShadow = MainLightRealtimeShadow(shadowCoord);
//
//     #ifdef CALCULATE_BAKED_SHADOWS
//     half bakedShadow = BakedShadow(shadowMask, occlusionProbeChannels);
//     #else
//     half bakedShadow = half(1.0);
//     #endif
//
//     #ifdef MAIN_LIGHT_CALCULATE_SHADOWS
//     half shadowFade = GetMainLightShadowFade(positionWS);
//     // half shadowFade = 1.0;
//     #else
//     half shadowFade = half(1.0);
//     #endif
//
//     return MixRealtimeAndBakedShadows(realtimeShadow, bakedShadow, shadowFade);
// }
//



/**
 * \brief 采样灯光方向贴图
 * \param input 
 * \return 
 */
float4 DirectionMap(Varyings input)
{
    float4 direction =float4(0,0,0,1);
    #if defined(LIGHTMAP_ON)&&DIRLIGHTMAP_COMBINED
     direction = SAMPLE_TEXTURE2D_LIGHTMAP(unity_LightmapInd, samplerunity_Lightmap,input.staticLightmapUV);
    #endif
    return direction;
}

/**
 * \brief 通过灯光方向贴图求出灯光方向
 * \param directionMap 
 * \return 
 */
float3 MainLightDirMAP(float4 directionMap)
{
    float3 mainLightDir = float3(0,0,0);
    float4 lightDir = float4(directionMap *2 ) - 1 ;
    float mainLightFactor = length(lightDir.xyz);
    mainLightDir = normalize(lightDir.xyz / max(0.001, mainLightFactor));
    return mainLightDir;
}

/**
 * \brief 求matcap的uv
 * \return 
 */
float2 MatCapUV(float3 positionWS, float3 normalWS)
{
    // half3 N = input.normalWS.xyz;
    // half3 T = input.tangentWS.xyz;
    // float sgn = input.tangentWS.w;      // should be either +1 or -1
    // half3 B = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
				    
    // N = TransformTangentToWorld(surfaceData.normalTS, half3x3(T, B, N));
    // N = normalize(N);

    float3 e = normalize(mul(UNITY_MATRIX_V, float4( positionWS, 1.0))).xyz;
    float3 n = normalize(mul(UNITY_MATRIX_V, float4( normalWS, 0.0))).xyz;

    float3 r = reflect(e, n);
    float m = 2. * sqrt(
        pow(r.x, 2.) +
        pow(r.y, 2.) +
        pow(r.z + 1., 2.)
    );

    float2 capCoord = r.xy / m + 0.5;
    // half2 capCoord = r.xy*0.5 + 0.5;
    return capCoord;
}

/**
 * \brief 重新映射范围
 * \param x 输入
 * \param t1 原始最小
 * \param t2 原始最大
 * \param s1 新最小
 * \param s2 新最大
 * \return 
 */
half remap(half x, half t1, half t2, half s1, half s2)
{
    return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
}

// matcap的方式计算，间接光就不极端投影了
half3 SampleLightmap2(float2 staticLightmapUV, float2 dynamicLightmapUV, half3 normalWS)
{
    #ifdef UNITY_LIGHTMAP_FULL_HDR
    bool encodedLightmap = false;
    #else
    bool encodedLightmap = true;
    #endif

    half4 decodeInstructions = half4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0h, 0.0h);

    // The shader library sample lightmap functions transform the lightmap uv coords to apply bias and scale.
    // However, universal pipeline already transformed those coords in vertex. We pass half4(1, 1, 0, 0) and
    // the compiler will optimize the transform away.
    half4 transformCoords = half4(1, 1, 0, 0);

    float3 diffuseLighting = 0;

    // #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    diffuseLighting = SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
        TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
        LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, normalWS, encodedLightmap, decodeInstructions);
    // #elif defined(LIGHTMAP_ON)
    // diffuseLighting = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, transformCoords, encodedLightmap, decodeInstructions);
    // #endif

    
    // #if defined(DYNAMICLIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    // diffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
    //     TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
    //     dynamicLightmapUV, transformCoords, normalWS, false, decodeInstructions);
    // #elif defined(DYNAMICLIGHTMAP_ON)
    // diffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
    //     dynamicLightmapUV, transformCoords, false, decodeInstructions);
    // #endif
    
    return diffuseLighting;
}



half3 LightingPhysicallyBased2(BRDFData brdfData, BRDFData brdfDataClearCoat,
    half3 lightColor, half3 lightDirectionWS, half lightAttenuation,
    half3 normalWS, half3 viewDirectionWS,
    half clearCoatMask, bool specularHighlightsOff, half2 matcapUV)
{
    //添加matcap对光照的接管
#if defined _NOLIGHT_ON
    half4 mc = SAMPLE_TEXTURE2D(_MatCapMap2, sampler_MatCapMap2, matcapUV);
    half NdotL = saturate(dot(normalWS, normalize(float3(1.0,_MatCapSpecularHeight,1.0)))) * _MatCapMap2Strength;
    half3 radiance = mc.xyz *  NdotL;

#else
    half NdotL = saturate(dot(normalWS, lightDirectionWS));
    half3 radiance = lightColor * (lightAttenuation * NdotL);
#endif
    
    half3 brdf = brdfData.diffuse;
    #ifndef _SPECULARHIGHLIGHTS_OFF
    [branch] if (!specularHighlightsOff)
    {
    #if defined _NOLIGHT_ON
        brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, normalize(float3(1.0,_MatCapSpecularHeight,1.0)), viewDirectionWS);
    #else
        brdf += brdfData.specular * DirectBRDFSpecular(brdfData, normalWS, lightDirectionWS, viewDirectionWS);
    #endif
        // #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
        // // Clear coat evaluates the specular a second timw and has some common terms with the base specular.
        // // We rely on the compiler to merge these and compute them only once.
        // half brdfCoat = kDielectricSpec.r * DirectBRDFSpecular(brdfDataClearCoat, normalWS, lightDirectionWS, viewDirectionWS);
        //
        // // Mix clear coat and base layer using khronos glTF recommended formula
        // // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_materials_clearcoat/README.md
        // // Use NoV for direct too instead of LoH as an optimization (NoV is light invariant).
        // half NoV = saturate(dot(normalWS, viewDirectionWS));
        // // Use slightly simpler fresnelTerm (Pow4 vs Pow5) as a small optimization.
        // // It is matching fresnel used in the GI/Env, so should produce a consistent clear coat blend (env vs. direct)
        // half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - NoV);
        //
        // brdf = brdf * (1.0 - clearCoatMask * coatFresnel) + brdfCoat * clearCoatMask;
        // #endif // _CLEARCOAT
    }
    #endif // _SPECULARHIGHLIGHTS_OFF

    // return brdf;
    return brdf * radiance;
}


half3 LightingPhysicallyBased2(BRDFData brdfData, BRDFData brdfDataClearCoat, Light light, half3 normalWS,
    half3 viewDirectionWS, float4 shadowCoord, half clearCoatMask, bool specularHighlightsOff, half2 matcapUV)
{
    // return half3(1,0,0);
    return LightingPhysicallyBased2(brdfData, brdfDataClearCoat, light.color, light.direction,
        light.distanceAttenuation * light.shadowAttenuation, normalWS, viewDirectionWS, clearCoatMask,
        specularHighlightsOff, matcapUV );
}






void InitializeInputDataBackSpecular(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#elif defined(_MATCAPMAP_ON)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
    //原来的处理逻辑会判断不同的照明形式进行间接光照采样。
    inputData.bakedGI = SAMPLE_GI_TWO(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    //取消掉宏命令会导致报错但是能运行
    // inputData.bakedGI = SampleLightmap2(input.staticLightmapUV, 0, inputData.normalWS);
#endif

//使用matcap接管间接光照
#if defined (_MATCAPMAP_ON)||(_NOLIGHT_ON)
    

    half2 capCoord = MatCapUV(inputData.positionWS, inputData.normalWS);

    half4 mc = SAMPLE_TEXTURE2D(_MatCapMap, sampler_MatCapMap, capCoord) * _MatCapMapStrength * _MatCapMapColor;
        
    half alwaysLightAttenuation = MainLightRealtimeShadow2(inputData.shadowCoord);
    alwaysLightAttenuation = remap(smoothstep(0, 1, alwaysLightAttenuation), 0, 1, _MatCapMapShadowStrength, 1);

    
    
    // inputData.bakedGI = mc.xyz;
    
    // inputData.bakedGI =  mc.xyz * alwaysLightAttenuation;
    inputData.bakedGI =  lerp(mc.xyz * _MatCapMapShadowColor, mc.xyz, alwaysLightAttenuation);
    
#endif
    
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
}


////////////////////////////////////////////////////////////////////////////////
/// PBR lighting...
////////////////////////////////////////////////////////////////////////////////
half4 UniversalFragmentPBRBackSpecular(InputData inputData, SurfaceData surfaceData)
{
    #if defined(_SPECULARHIGHLIGHTS_OFF)
    bool specularHighlightsOff = true;
    #else
    bool specularHighlightsOff = false;
    #endif
    BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    InitializeBRDFData(surfaceData, brdfData);

    #if defined(DEBUG_DISPLAY)
    half4 debugColor;

    if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor))
    {
        return debugColor;
    }
    #endif

    // Clear-coat calculation...
    BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLightLayer();
    
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    mainLight.direction = normalize(mainLight.direction.xyz + half3(0,_MainLightHeight,0));

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    
    //获得matcapUV
    half2 matcapUV = MatCapUV(inputData.positionWS, inputData.normalWS);
    
    LightingData lightingData = CreateLightingData(inputData, surfaceData);
    
    lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
                                              inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                              inputData.normalWS, inputData.viewDirectionWS);
    
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    {
        lightingData.mainLightColor = LightingPhysicallyBased2(brdfData, brdfDataClearCoat,
                                                              mainLight,
                                                              inputData.normalWS, inputData.viewDirectionWS,
                                                              inputData.shadowCoord,
                                                              surfaceData.clearCoatMask, specularHighlightsOff,matcapUV);
    }
    
    
    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();
    
    #if USE_CLUSTERED_LIGHTING
    for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    }
    #endif
    
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
    
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        {
            lightingData.additionalLightsColor += LightingPhysicallyBased(brdfData, brdfDataClearCoat, light,
                                                                          inputData.normalWS, inputData.viewDirectionWS,
                                                                          surfaceData.clearCoatMask, specularHighlightsOff);
        }
    LIGHT_LOOP_END
    #endif
    
    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * brdfData.diffuse;
    #endif
    
    // return float4( inputData.bakedGI, surfaceData.alpha);
    // return float4( mainLight.distanceAttenuation.xxx, surfaceData.alpha);
    // return float4( brdfData.specular, surfaceData.alpha);
    return CalculateFinalColor(lightingData, surfaceData.alpha);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitAndMatcapVertex(Attributes input)
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

    return output;
}


// Used in Standard (Physically Based) shader
half4 LitAndMatcapFragment(Varyings input) : SV_Target
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

    InputData inputData;
    InitializeInputDataBackSpecular(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

    #ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
    #endif
    

    half4 color = UniversalFragmentPBRBackSpecular(inputData, surfaceData);


    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);
    
    // color.rgb = MainLightRealtimeShadow2(inputData.shadowCoord) ;
    //matcap
    
    return color;
}


#endif
