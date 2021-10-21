Shader "ZHYShaders/fenglele"
{
	Properties
	{
		_BlendTex("BlendTex", 2D) = "white" {}
		_BlockMainTex("BlockMainTex", 2D) = "white" {}
		_BlockScale("BlockScale", Float) = 0.022

	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			// LOD 200

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
			#pragma exclude_renderers gles
			#include "UnityPBSLighting.cginc"

			#define TERRAIN_STANDARD_SHADER
			#define _NORMALMAP
			#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard

			//
			sampler2D _BlockMainTex;
		// float4 _BlockMainTex_ST;
		sampler2D _BlendTex;
		float _BlockScale;
		//

		struct Input
		{
			float2 uv_BlendTex;
			float2 uv_BlockMainTex;
			float3 worldPos;
		};

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// // Albedo comes from a texture tinted by color
			// fixed4 c = tex2D (_BlockMainTex, IN.uv_BlockMainTex) * _Color;
			// o.Albedo = c.rgb;
			// // Metallic and smoothness come from slider variables
			// o.Metallic = _Metallic;
			// o.Smoothness = _Glossiness;
			// o.Alpha = c.a;

			//Index
			float2 encodedIndices = tex2D(_BlendTex, IN.uv_BlendTex).xy;
			float2 twoVerticalIndices = floor((encodedIndices * 16.0));
			float2 twoHorizontalIndices = (floor((encodedIndices * 256.0)) - (16.0 * twoVerticalIndices));
			float4 decodedIndices;
			decodedIndices.x = twoHorizontalIndices.x;
			decodedIndices.y = twoVerticalIndices.x;
			decodedIndices.z = twoHorizontalIndices.y;
			decodedIndices.w = twoVerticalIndices.y;
			decodedIndices = floor(decodedIndices / 4) / 4;
			//

			//
			float blendRatio = tex2D(_BlendTex, IN.uv_BlendTex).z;
			float2 worldScale = (IN.worldPos.xz * _BlockScale);
			float2 worldUv = 0.234375 * frac(worldScale) + 0.0078125;
			float2 dx = clamp(0.234375 * ddx(worldScale), -0.0078125, 0.0078125);
			float2 dy = clamp(0.234375 * ddy(worldScale), -0.0078125, 0.0078125);
			float2 uv0 = worldUv.xy + decodedIndices.xy;
			float2 uv1 = worldUv.xy + decodedIndices.zw;
			// Sample the two texture
			float4 col0 = tex2D(_BlockMainTex, uv0, dx, dy);
			float4 col1 = tex2D(_BlockMainTex, uv1, dx, dy);
			// Blend the two textures
			float4 col = lerp(col0, col1, blendRatio);
			//
			o.Albedo = col.rgb;
			o.Alpha = col.a;
		}
		ENDCG
		}
			Fallback "Nature/Terrain/Diffuse"
}