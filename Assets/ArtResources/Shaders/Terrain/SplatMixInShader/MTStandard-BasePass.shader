Shader "MT/Standard-BasePass" 
{
	Properties{

		// [HideInInspector]不在Inspector面板显示这个属性

		// 定义Textures
		// Unity中Properties名称以下划线开头，首字母大写命名
		// name ("display name", 2D) = "defaulttexture" {}
		// "defaulttexture" = "grey": (R,G,B,A) = 0.5, 0.5, 0.5, 0.5
		// "defaulttexture" = "bump": (R,G,B,A) = 0.5, 0.5, 1, 0.5

		[HideInInspector]_Control("AlphaMap", 2D) = "" {} 
		[HideInInspector]_Splat0("Layer 0 (R)", 2D) = "grey" {}
		[HideInInspector]_Normal0("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector]_NormalScale0("NormalScale 0", Float) = 1.0 // 这是干啥的？

		[HideInInspector]_Splat1("Layer 1 (G)", 2D) = "grey" {}
		[HideInInspector]_Normal1("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector]_NormalScale1("NormalScale 1", Float) = 1.0
		[HideInInspector]_Splat2("Layer 2 (B)", 2D) = "grey" {}
		[HideInInspector]_Normal2("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector]_NormalScale2("NormalScale 2", Float) = 1.0
		[HideInInspector]_Splat3("Layer 3 (A)", 2D) = "grey" {}
		[HideInInspector]_Normal3("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector]_NormalScale3("NormalScale 3", Float) = 1.0
		[HideInInspector][Gamma] _Metallic0("Metallic 0", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic1("Metallic 1", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0
		[HideInInspector][Gamma] _Metallic3("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Smoothness0("Smoothness 0", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness1("Smoothness 1", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness2("Smoothness 2", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _Smoothness3("Smoothness 3", Range(0.0, 1.0)) = 1.0
	}
	SubShader{
		// Tags告诉引擎如何以及何时将其渲染
		Tags{
			// Queue控制渲染顺序，这样所有透明的Shader能保证在所有不透明的Shader后渲染
			// Geometry (default)：大多数物体使用此队列，用来渲染不透明的物体
			// Background=1000，Geometry=2000，AlphaTest=2450，Transparent=3000，Overlay=4000
			"Queue" = "Geometry-100" // 为什么这么用？
			"RenderType" = "Opaque" // opaque不透明
		}

		CGPROGRAM
#pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer addshadow fullforwardshadows
//#pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
#pragma multi_compile_fog // needed because finalcolor oppresses fog code generation.
#pragma target 3.0
		// needs more than 8 texcoords
#pragma exclude_renderers gles
#include "UnityPBSLighting.cginc"

//#pragma multi_compile __ _NORMALMAP

#define TERRAIN_STANDARD_SHADER
#define _NORMALMAP
#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
#include "TerrainSplatmapCommon.cginc"

		half _Metallic0;
		half _Metallic1;
		half _Metallic2;
		half _Metallic3;

		half _Smoothness0;
		half _Smoothness1;
		half _Smoothness2;
		half _Smoothness3;

		void surf(Input IN, inout SurfaceOutputStandard o) {
			half4 splat_control;
			half weight;
			fixed4 mixedDiffuse; 
			half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
			// SplatmapMix：基于地形图示执行反照率和法线的混合
			SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, o.Normal); 
			o.Albedo = mixedDiffuse.rgb; // Albedo 反射率
			o.Alpha = weight;
			o.Smoothness = mixedDiffuse.a;
			o.Metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));
		}
		ENDCG
	}
	Fallback "Nature/Terrain/Diffuse"
}