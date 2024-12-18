﻿Shader "URP/Shell"
{
	Properties
	{
		_BaseMap("Base Map", 2D) = "white" {}
    	_FurMap("Fur Map", 2D) = "white" {}
    	[IntRange] _ShellAmount("Shell Amount", Range(1, 100)) = 16
    	_ShellStep("Shell Step", Range(0.0, 0.01)) = 0.0001
    	_AlphaCutout("Alpha Cutout", Range(0.0, 1.0)) = 0.1
		_Occlusion("Occlusion", float) = 1.0
		_RimLightPower("Rim Light Power", Range(0.0, 20.0)) = 6.0
    	_RimLightIntensity("Rim Light Intensity", Range(0.0, 1.0)) = 0.5
		_BaseMove("Base Move", Vector) = (0.0, -0.0, 0.0, 3.0)
    	_WindFreq("Wind Freq", Vector) = (0.5, 0.7, 0.9, 1.0)
    	_WindMove("Wind Move", Vector) = (0.2, 0.3, 0.2, 1.0)
	}

	SubShader
	{
        Tags 
		{
            "IgnoreProjector"="True"
            "RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
        }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" "Queue" = "Opaque"}

			Cull Off

			HLSLPROGRAM

			#pragma vertex Vertex
			#pragma geometry geom 
			#pragma fragment Frag

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT

			#include "HLSLSupport.cginc"

			#pragma target 4.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "ShellBase.hlsl"

			ENDHLSL
		}

		Pass
		{
		    Name "ShadowCaster"
		    Tags { "LightMode" = "ShadowCaster" }

		    ZWrite On
		    ZTest LEqual
		    ColorMask 0

		    HLSLPROGRAM
		    #pragma exclude_renderers gles gles3 glcore
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		    #include "ShellBase.hlsl"
		    #pragma vertex Vertex
		    #pragma require geometry
		    #pragma geometry geom 
		    #pragma fragment FragShadow
		    ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
	}

	FallBack "VertexLit"
}