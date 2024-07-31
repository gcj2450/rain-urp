#ifndef REALTIMEEYE_LIGHTING_INCLUDE
#define REALTIMEEYE_LIGHTING_INCLUDE

// #include "SkinData.hlsl"
#include "./RealTimeEyeCommon.hlsl"

//根据UV的中心进行缩放，uv中心为0.5 0.5，会被整体缩放。
half2 ScaleUVsByCenter(half2 UVs, float Scale)
{
	return(UVs / Scale + (0.5).xx) - (0.5 / Scale).xx;
}

//根据UV的中心进行缩放，相对于ByCenter的区别，在于这个拉伸的效果，范围一直在0-1.
half2 ScaleUVFromCircle(half2 UV, float Scale)
{
	float2 UVcentered = UV - float2(0.5f, 0.5f);
	float UVlength = length(UVcentered);
	// UV on circle at distance 0.5 from the center, in direction of original UV
	float2 UVmax = normalize(UVcentered) * 0.5f;

	float2 UVscaled = lerp(UVmax, float2(0.f, 0.f), saturate((1.f - UVlength * 2.f) * Scale));
	return UVscaled + float2(0.5f, 0.5f);
}

//根据物体的折射率 物体法向，以及入射的方向，计算折射后的方向
float3 RefractDirection(float internalIoR, float3 WorldNormal, float3 incidentVector)
{
	float airIoR = 1.00029;
	float n = airIoR / internalIoR;
	float facing = dot(WorldNormal, incidentVector);
	float w = n * facing;
	float k = sqrt(1 + (w - n) * (w + n));
	float3 t = -normalize((w - k) * WorldNormal - n * incidentVector);
	return t;
}

//根据根据眼球的房水折射率计算出虹膜的折射后的uv和凹度
//UV 是被缩放后的uv
//IOR index of Refraction 默认的眼内晶状体的折射率
//IrisUVRadius 虹膜的uv半径
//IrisDepth 虹膜的深度 这个可以通过一张HDR的高度图获得
//EyeDirection 眼球的朝向 世界坐标系下
//WorldTangent 当前位置的切线 世界坐标系下
//IrisUV 输出虹膜的uv
//IrisConcavity 虹膜的凹度（被折射之后的深度）
void EyeRefraction(float2 UV, float3 NormalDir, float3 ViewDir, half IOR,
float IrisUVRadius, float IrisDepth, float3 EyeDirection, float3 WorldTangent,
out float2 IrisUV, out float IrisConcavity)
{
	IrisUV = float2(0.5, 0.5);
	IrisConcavity = 1.0;
	#ifndef SHADERGRAPH_PREVIEW
		// 获取视线被角膜折射后的方向
		float3 RefractedViewDir = RefractDirection(IOR, NormalDir, ViewDir);
		float cosAlpha = dot(ViewDir, EyeDirection);    // EyeDirection是眼睛正前方方向
		cosAlpha = lerp(0.325, 1, cosAlpha * cosAlpha); //视线与眼球方向的夹角
		RefractedViewDir = RefractedViewDir * (IrisDepth / cosAlpha);//虹膜深度越大，折射越强；视线与眼球方向夹角越大，折射越强

		//根据WorldTangent求出EyeDirection垂直的向量，也就是虹膜平面的Tangent和BiTangent方向,也就是uv的偏移方向
		float3 TangentDerive = normalize(WorldTangent - dot(WorldTangent, EyeDirection) * EyeDirection); //求出垂直于EyeDir的tangent
		float3 BiTangentDerive = normalize(cross(EyeDirection, TangentDerive)); //叉乘求出
		float RefractUVOffsetX = dot(RefractedViewDir, TangentDerive);//tangent是根据uv的u方向生成的，和入射角点乘
		float RefractUVOffsetY = dot(RefractedViewDir, BiTangentDerive); //binormal是和tangent还有EyeDir垂直的，刚好是v的方向
		float2 RefractUVOffset = float2(-RefractUVOffsetX, RefractUVOffsetY); //这里为什么取负，待测试
		float2 UVRefract = UV + IrisUVRadius * RefractUVOffset;
		//UVRefract = lerp(UV,UVRefract,IrisMask);
		IrisUV = (UVRefract - float2(0.5, 0.5)) / IrisUVRadius * 0.5 + float2(0.5, 0.5);
		IrisConcavity = length(UVRefract - float2(0.5, 0.5)) * IrisUVRadius;
	#endif
}

//眼睛的brdf
half3 EyeBxDF(
	half3 DiffuseColor, half3 SpecularColor, float Roughness, half3 N, half3 V, half3 L,
	half IrisMask, half3 IrisNormal, half3 CausticNormal,
	half3 LightColor, float Shadow, float3 DiffuseShadow, Texture2D SSSLUT,
	SamplerState sampler_SSSLUT, half3 ForgeL, half ForgeLightSize
	)
{
	float3 H = normalize(V + L);
	float NoH = saturate(dot(N, H));
	float NoV = saturate(abs(saturate(dot(N, V))) + 1e-5);
	float NoL = saturate(dot(N, L));
	float VoH = saturate(dot(V, H));
	
	//漫反射部分
	//虹膜（里层鸾尾状物体）
	float IrisNoL = saturate(dot(IrisNormal, L));
	float Power = lerp(12, 1, IrisNoL);
	float Caustic = 0.3 + (0.8 + 0.2 * (Power + 1)) * pow(saturate(dot(CausticNormal, L)), Power);//焦散
	IrisNoL = IrisNoL * Caustic;

	//巩膜（眼白）
	float3 ScleraNoL = SAMPLE_TEXTURE2D(SSSLUT, sampler_SSSLUT, half2(dot(N, L) * 0.5 + 0.5, 0.5)).rgb;
	float3 NoL_Diff = lerp(ScleraNoL, IrisNoL, IrisMask);
	float3 DiffIrradiance = LightColor * PI * DiffuseShadow * NoL_Diff;
	half3 DiffuseLighting = Diffuse_Lambert(DiffuseColor) * DiffIrradiance;
	// #if defined(_DIFFUSE_OFF)
	// 	DiffuseLighting = float3(0, 0, 0);
	// #endif

	//高光
	//巩膜及角膜（外层透明薄膜层）
	float3 SpecIrradiance = LightColor * PI * Shadow * NoL;
	half3 SpecularLighting = SpecularGGX(Roughness, SpecularColor, NoH, NoV, NoL, VoH) * SpecIrradiance;
	float F = F_Schlick_UE4(half3(0.04, 0.04, 0.04), VoH).r * IrisMask;
	float Fcc = 1.0 - F;
	DiffuseLighting *= Fcc;
	
	#ifdef _FORGE_SPECULER_ON
	H = normalize(V + ForgeL);
	NoH = saturate(dot(N, H));
	// NoV = saturate(abs(saturate(dot(N, V))) + 1e-5);
	NoL = saturate(dot(N, ForgeL));
	VoH = saturate(dot(V, H));
	half3 SpecularLighting2 = SpecularGGX(Roughness, SpecularColor, NoH, NoV, NoL, VoH) * SpecIrradiance;
	return DiffuseLighting + saturate(SpecularLighting) + saturate(SpecularLighting2 * ForgeLightSize);
	#else
	return DiffuseLighting + saturate(SpecularLighting);
	#endif
}

//眼球的直接光照函数
void DirectLighting(
	float3 DiffuseColor, float3 SpecularColor, float Roughness, float3 WorldPos,
	half3 WorldNormal, half3 ViewDir, half IrisMask, half3 IrisNormal, half3 CausticNormal,
	Texture2D SSSLUT, SamplerState sampler_SSSLUT, out float3 DirectLightColor, half3 ForgeLight,
	half ForgeLightSize
	)
{
	DirectLightColor = float3(0.5, 0.5, 0);
	#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
		float4 clipPos = TransformWorldToHClip(WorldPos);
		float4 ShadowCoord = ComputeScreenPos(clipPos);
	#else
		float4 ShadowCoord = TransformWorldToShadowCoord(WorldPos);
	#endif
	float4 ShadowMask = float4(1.0, 1.0, 1.0, 1.0);
	//--------ֱ直接光照--------
	half3 N = WorldNormal;
	half3 V = ViewDir;
	//主光
	half3 DirectLighting_MainLight = half3(0, 0, 0);
	{
		Light light = GetMainLight(ShadowCoord, WorldPos, ShadowMask);
		half3 L = light.direction;
		half3 ForgeL = normalize(ForgeLight);
		half3 LightColor = light.color;
		float Shadow = saturate(light.shadowAttenuation + 0.2);
		half3 DiffuseShadow = lerp(half3(0.11, 0.025, 0.012), half3(1, 1, 1), Shadow);//hard code;

		#ifdef _FORGE_SPECULER_ON
		DirectLighting_MainLight = EyeBxDF(DiffuseColor, SpecularColor, Roughness, N, V, L,
		IrisMask, IrisNormal, CausticNormal, LightColor, Shadow, DiffuseShadow, SSSLUT, sampler_SSSLUT, ForgeL,ForgeLightSize);
		#else
		DirectLighting_MainLight = EyeBxDF(DiffuseColor, SpecularColor, Roughness, N, V, L,
		IrisMask, IrisNormal, CausticNormal, LightColor, Shadow, DiffuseShadow, SSSLUT, sampler_SSSLUT, half3(0,0,0),0);
		#endif
	}
	//附加光
	half3 DirectLighting_AddLight = half3(0, 0, 0);
	#if defined(_ADDITIONAL_LIGHTS)
		int pixelLightCount = GetAdditionalLightsCount();
		for (int lightIndex = 0; lightIndex < pixelLightCount; ++lightIndex)
		{
			Light light = GetAdditionalLight(lightIndex, WorldPos, ShadowMask);
			half3 L = light.direction;
			half3 LightColor = light.color;
			float Shadow = saturate(light.shadowAttenuation + 0.2) * light.distanceAttenuation;
			half3 DiffuseShadow = lerp(half3(0.11, 0.025, 0.012), half3(1, 1, 1), Shadow);//hard code;
			DirectLighting_AddLight += EyeBxDF(DiffuseColor, SpecularColor, Roughness, N, V, L,
			IrisMask, IrisNormal, CausticNormal, LightColor, Shadow, DiffuseShadow, SSSLUT, sampler_SSSLUT,half3(0,0,0),0);
		}
	#endif
	
	DirectLightColor = DirectLighting_MainLight + DirectLighting_AddLight;
}

//眼球的间接光函数
void IndirectLighting(float3 DiffuseColor, float3 SpecularColor, float Roughness, half3 WorldPos, half3 WorldNormal, half3 ViewDir,
half Occlusion, half EnvRotation, out float3 IndirectLightColor)
{
	IndirectLightColor = float3(0, 0, 0);
	float3 N = WorldNormal;
	float3 V = ViewDir;
	float NoV = saturate(abs(dot(N, V)) + 1e-5);
	half DiffuseAO = Occlusion;
	half SpecualrAO = GetSpecularOcclusion(NoV, Pow2(Roughness), Occlusion);
	half3 DiffOcclusion = AOMultiBounce(DiffuseColor, DiffuseAO);
	half MainLightShadow = clamp(GetMainLightShadow(WorldPos), 0.3, 1.0);
	half3 SpecOcclusion = AOMultiBounce(SpecularColor, SpecualrAO * MainLightShadow);

	//-------------SH---------
	half3 IrradianceSH = SampleSH(N);// Diffuse Lambert中的PI已经Bake进了SH中，因此不需要除以PI
	half3 IndirectDiffuse = DiffuseColor * IrradianceSH * DiffOcclusion;
	#if defined(_SH_OFF)
		IndirectDiffuse = float3(0, 0, 0);
	#endif
	//-------------IBL-------------
	half3 R = reflect(-V, N);
	R = RotateDirection(R, EnvRotation);
	half3 EnvSpecularLobe = SpecularIBL(R, WorldPos, Roughness, SpecularColor, NoV);
	half3 IndirectSpecular = EnvSpecularLobe * SpecOcclusion;
	#if defined(_IBL_OFF)
		IndirectSpecular = float3(0, 0, 0);
	#endif

	IndirectLightColor = IndirectDiffuse + IndirectSpecular;
}

#endif