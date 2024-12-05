#ifndef DITHERPASS
#define DITHERPASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


#define DITHERNEAR 1.0
#define DITHERGAR 2.5


inline float Dither4x4Bayer(int x, int y)
{
    const float dither[16] =
    {
        1, 9, 3, 11,
       13, 5, 15, 7,
        4, 12, 2, 10,
       16, 8, 14, 6
    };
    int r = y * 4 + x;
    return dither[r] / 16; // same # of instructions as pre-dividing due to compiler magic
}


void DitherCustom(half4 screenPos, half3 positionWS)
{
    //screenPos是屏幕坐标vertexPositionInput.positionNDC获取
    //* _ScreenParams.xy是为了在每个像素上做clip如果觉得过于密集可以降低
    half2 scenPos = screenPos.xy / screenPos.w * _ScreenParams.xy;
    float dither = Dither4x4Bayer(fmod(scenPos.x, 4), fmod(scenPos.y, 4));
    half dis = distance(GetCameraPositionWS(), positionWS);
    dis = smoothstep(DITHERNEAR, DITHERGAR, dis);
    clip(dis - dither);
}

#endif