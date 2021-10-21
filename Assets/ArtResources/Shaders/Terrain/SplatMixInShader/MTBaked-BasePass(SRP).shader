Shader "MT/Baked-BasePass"
{
	Properties
	{
		_Control("Control (RGBA)", 2D) = "red" {}
		_Splat0("Layer 0 (R)", 2D) = "grey" {}
		_Splat1("Layer 1 (G)", 2D) = "grey" {}
		_Splat2("Layer 2 (B)", 2D) = "grey" {}
		_Splat3("Layer 3 (A)", 2D) = "grey" {}
		_Normal3("Normal 3 (A)", 2D) = "bump" {}   
		_Normal2("Normal 2 (B)", 2D) = "bump" {}
		_Normal1("Normal 1 (G)", 2D) = "bump" {}
		_Normal0("Normal 0 (R)", 2D) = "bump" {}
		_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
	}
	SubShader
	{
		Tags { 
			"Queue" = "Geometry-100" 
			"RenderType" = "Opaque" 
			"IgnoreProjector" = "True" 
			"RenderPipeline" = "UniversalPipeline" 
			}
		LOD 100

		Pass
		{
			Name "BakedBase"
			Tags{ "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			// Lighting include is needed because of GI
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/BakedLitInput.hlsl"


CBUFFER_START(UnityPerMaterial)
			float4 _Control_ST;
			float4 _Control_TexelSize;
			half4 _SpecColor;
			half4 _Splat0_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST;
CBUFFER_END

			TEXTURE2D(_Control);    SAMPLER(sampler_Control);
			TEXTURE2D(_Splat0);     SAMPLER(sampler_Splat0);
			TEXTURE2D(_Splat1);
			TEXTURE2D(_Splat2);
			TEXTURE2D(_Splat3);

			struct Attributes
			{
				float4 positionOS       : POSITION;
				float2 texcoord         : TEXCOORD0;
				float2 lightmapUV       : TEXCOORD1;
				float3 normalOS         : NORMAL;
				float4 tangentOS        : TANGENT;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 uvMainAndLM              : TEXCOORD0; // xy: control, zw: lightmap
				float4 uvSplat01                : TEXCOORD1; // xy: splat0, zw: splat1
				float4 uvSplat23                : TEXCOORD2; // xy: splat2, zw: splat3
				half4 fogFactorAndVertexLight   : TEXCOORD3; // x: fogFactor, yzw: vertex light
				half3 normalWS                  : TEXCOORD4;

				float4 clipPos : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

				output.normalWS = TransformObjectToWorldNormal(input.normalOS);

				output.fogFactorAndVertexLight.x = ComputeFogFactor(vertexInput.positionCS.z);
				output.fogFactorAndVertexLight.yzw = VertexLighting(vertexInput.positionWS, output.normalWS.xyz);
				output.clipPos = vertexInput.positionCS;

				output.uvMainAndLM.xy = input.texcoord;
				output.uvMainAndLM.zw = input.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;

				output.uvSplat01.xy = TRANSFORM_TEX(input.texcoord, _Splat0);
				output.uvSplat01.zw = TRANSFORM_TEX(input.texcoord, _Splat1);
				output.uvSplat23.xy = TRANSFORM_TEX(input.texcoord, _Splat2);
				output.uvSplat23.zw = TRANSFORM_TEX(input.texcoord, _Splat3);

				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
				output.normalWS = normalInput.normalWS;

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 splatUV = (input.uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
				half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

				half3 diffAlbedo[4];

				diffAlbedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, input.uvSplat01.xy);
				diffAlbedo[1] = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat0, input.uvSplat01.zw);
				diffAlbedo[2] = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat0, input.uvSplat23.xy);
				diffAlbedo[3] = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat0, input.uvSplat23.zw);

				half3 color = 0.0h;
				color += diffAlbedo[0] * splatControl.rrr;
				color += diffAlbedo[1] * splatControl.ggg;
				color += diffAlbedo[2] * splatControl.bbb;
				color += diffAlbedo[3] * splatControl.aaa;

				half3 normalWS = input.normalWS;
				normalWS = NormalizeNormalPerPixel(normalWS);

#if LIGHTMAP_ON
				color *= SampleLightmap(input.uvMainAndLM.zw, normalWS);
#endif
				color = MixFog(color, input.fogFactorAndVertexLight.x);
				/*half alpha = diffuseAlpha.a * _BaseColor.a;
				half4 specular = SampleSpecularSmoothness(splatUV, alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
				half smoothness = specular.a;
				half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specular, smoothness, emission, alpha);*/
				return half4(color, 1);
			}
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Unlit"
}
