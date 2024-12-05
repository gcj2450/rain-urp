Shader "Custom/URPAdvancedShader"
{
    Properties {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        _BumpMap ("Bump Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Range(0,1)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass {
            Name "CustomForwardLit"
            HLSLPROGRAM

            #pragma vertex VertexMain
            #pragma fragment FragmentMain
            #include "BaseInput.hlsl"
            #include "BaseForwardPass.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
