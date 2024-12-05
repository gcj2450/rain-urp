#ifndef BASE_FORWARD_PASS_INCLUDED
#define BASE_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

// 引入输入定义
#include "BaseInput.hlsl"

// 顶点着色器
FragmentInput VertexMain(VertexInput v)
{
    FragmentInput output;

    // 转换顶点位置到剪辑空间
    output.positionCS = TransformObjectToHClip(v.positionOS);

    // 转换法线、切线和副切线到世界空间
    output.normalWS = TransformObjectToWorldNormal(v.normalOS);
    output.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
    output.bitangentWS = cross(output.normalWS, output.tangentWS) * v.tangentOS.w;

    // 转换位置到世界空间
    output.worldPos = TransformObjectToWorld(v.positionOS).xyz;

    // 传递 UV
    output.uv = v.uv;

    // 设置阴影坐标
    //TRANSFORM_SHADOW_COORDS(output.shadowCoord, v.positionOS);

    return output;
}

// 计算法线贴图法线（切线空间 -> 世界空间）
float3 GetNormalFromMap(FragmentInput input)
{
    float3 normalTS = UnpackNormal(tex2D(_BumpMap, input.uv)) * _BumpScale;
    float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
    return normalize(mul(normalTS, TBN));
}

// 自定义光照模型
float3 CustomLighting(float3 normal, float3 lightDir, float3 viewDir, float3 lightColor, float3 albedo, float metallic, float smoothness)
{
    // 环境光
    float3 ambient = lightColor * 0.1;

    // 漫反射
    float diff = max(0.0, dot(normal, lightDir));
    float3 diffuse = lightColor * albedo * diff;

    // 高光反射 (基于 GGX)
    float3 halfVector = normalize(lightDir + viewDir);
    float spec = pow(max(0.0, dot(normal, halfVector)), smoothness * 128.0);
    float3 specular = lightColor * spec * metallic;

    return ambient + diffuse + specular;
}

// 片段着色器
float4 FragmentMain(FragmentInput input) : SV_Target
{
    // 采样基础纹理
    float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float3 albedo = baseColor.rgb * _BaseColor.rgb;

    // 计算法线
    float3 normal = GetNormalFromMap(input);
    float4 SHADOW_COORDS = TransformWorldToShadowCoord(input.worldPos);
    // 获取主光源方向和颜色
    Light mainLight = GetMainLight(SHADOW_COORDS);
    float3 lightDir = normalize(mainLight.direction);
    float3 lightColor = mainLight.color;

    // 阴影衰减
    //float shadow = SHADOW_ATTENUATION(input);

    // 计算主光源光照
    float3 viewDir = SafeNormalize(input.normalWS);
    float3 lighting = CustomLighting(normal, lightDir, viewDir, lightColor/* * shadow*/, albedo, _Metallic, _Smoothness);

    //// 计算附加光源
    //for (int i = 0; i < _AdditionalLightsCount; ++i)
    //{
    //    Light light = GetAdditionalLight(i, input.worldPos);
    //    float3 additionalLightDir = normalize(light.direction);
    //    float3 additionalLightColor = light.color;
    //    lighting += CustomLighting(normal, additionalLightDir, viewDir, additionalLightColor, albedo, _Metallic, _Smoothness);
    //}

    return float4(lighting, baseColor.a);
}

#endif // BASE_FORWARD_PASS_INCLUDED
