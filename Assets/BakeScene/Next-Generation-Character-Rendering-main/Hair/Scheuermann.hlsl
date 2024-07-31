#pragma once

float3 ShiftTangent(VertexOutput vertexOutput, float shift)
{
    return normalize(vertexOutput.worldBitangentDir + vertexOutput.worldNormalDir * shift).xyz;
}

float AnisotropySpecular(VertexOutput vertexOutput, CusomLightingData lightingData, float width, float strength, float3 shiftedTangent)
{
    //With HDRP Anisotropy
    //float dotTH = dot(shiftedTangent, lightingData.H);
    //Without HDRP Anistropy

    //float3 lerpTangent = lerp(vertexOutput.worldBitangentDir.xyz, shiftedTangent, _Anisotropy);

    float3 H = (lightingData.worldLightDir + lightingData.worldViewDir) * rsqrt(max(2.0 * dot(lightingData.worldLightDir, lightingData.worldViewDir) + 2.0, FLT_EPS));

    float dotTH = dot(shiftedTangent, H);
    //float dotTH = dot(shiftedTangent, lightingData.H);

    float sinTH = max(0.01, sqrt(1 - pow(dotTH, 2)));
    float dirAtten = smoothstep(-1, 0, dotTH);
    return dirAtten * pow(sinTH, width * 10.0) * strength;
    //return dirAtten * pow(sinTH, width * 10.0);
}

//=========================https://www.clonefactor.com/wordpress/program/unity3d/shader/2542/
struct HighLight
{
    half4 color;
    half shift;
};

struct Specular
{
    half width;
    half power;
    half scale;
};

half HairHighLight(Specular specular, half3 T, half3 V, half3 L)
{
    half3 H = normalize(V + L);
    half HdotT = dot(T, H);
    half sinTH = sqrt(1 - HdotT * HdotT);
    half dirAtten = smoothstep(-specular.width, 0, HdotT);
    return dirAtten * saturate(pow(sinTH, specular.power)) * specular.scale;
}

half3 ShiftTangent(half3 T, half3 N, float shift)
{
    return normalize(T + shift * N);
}

half3 SpecularStrandLighting(HighLight primary, HighLight secondary, Specular specular, half shiftTex,
    half3 N, half3 TB, half3 V, half3 L)
{
    // TB := Tangent/Bitangent to define the direction of hair highlight specular
    half3 t1 = ShiftTangent(TB, N, primary.shift + shiftTex);
    half3 t2 = ShiftTangent(TB, N, secondary.shift + shiftTex);

    half3 highLight = half3(0.0, 0.0, 0.0);
    highLight += primary.color.rgb * primary.color.a * HairHighLight(specular, t1, V, L);
    highLight += secondary.color.rgb * secondary.color.a * HairHighLight(specular, t2, V, L);
    return highLight;
}

// Simple subsurface scattering approximation
// https://developer.amd.com/wordpress/media/2012/10/Scheuermann_HairSketchSlides.pdf
half3 KajiyaKayLightTerm(Light light, half3 N)
{
    return light.color * light.shadowAttenuation * light.distanceAttenuation *
        max(0.0, 0.75 * dot(N, light.direction.xyz) + 0.25);
}
