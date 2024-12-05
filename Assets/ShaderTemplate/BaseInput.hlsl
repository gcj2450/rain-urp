#ifndef BASE_INPUT_INCLUDED
#define BASE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
// 材质属性定义
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor; // 基础颜色
//sampler2D _BaseMap; // 基础纹理
sampler2D _BumpMap; // 法线贴图
float _BumpScale; // 法线贴图强度
float _Metallic; // 金属度
float _Smoothness; // 光滑度
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// 顶点输入结构
struct VertexInput
{
    float4 positionOS : POSITION; // 顶点位置（Object Space）
    float3 normalOS : NORMAL; // 顶点法线（Object Space）
    float4 tangentOS : TANGENT; // 顶点切线（Object Space）
    float2 uv : TEXCOORD0; // UV 坐标
};

// 片段输入结构
struct FragmentInput
{
    float4 positionCS : SV_POSITION; // 顶点位置（Clip Space）
    float3 normalWS : NORMAL; // 顶点法线（World Space）
    float3 tangentWS : TANGENT; // 切线（World Space）
    float3 bitangentWS : TANGENT1; // 副切线（World Space）
    float3 worldPos : TEXCOORD1; // 世界空间位置
    float3 viewDir : TEXCOORD2; // 视线方向（世界空间）
    float2 uv : TEXCOORD0; // UV 坐标
    float2 shadowCoord: TEXCOORD3; // 阴影坐标
    //SHADOW_COORDS(TEXCOORD1)                  // 阴影坐标
};

#endif // BASE_INPUT_INCLUDED
