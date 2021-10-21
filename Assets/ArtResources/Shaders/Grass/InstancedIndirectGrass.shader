Shader "MobileDrawMeshInstancedIndirect/SingleGrass"
{
    Properties
    {

        _RenderTexture("Render Texture Height Map",2D) = "White"{}
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        _GroundColor("_GroundColor", Color) = (0.5,0.5,0.5)

        [Header(Grass Shape)]
        _GrassWidth("_GrassWidth", Float) = 1
        _GrassHeight("_GrassHeight", Float) = 1

        [Header(Wind)]
        _WindAIntensity("_WindAIntensity", Float) = 1.77
        _WindAFrequency("_WindAFrequency", Float) = 4
        _WindATiling("_WindATiling", Vector) = (0.1,0.1,0)
        _WindAWrap("_WindAWrap", Vector) = (0.5,0.5,0)

        _WindBIntensity("_WindBIntensity", Float) = 0.25
        _WindBFrequency("_WindBFrequency", Float) = 7.7
        _WindBTiling("_WindBTiling", Vector) = (.37,3,0)
        _WindBWrap("_WindBWrap", Vector) = (0.5,0.5,0)


        _WindCIntensity("_WindCIntensity", Float) = 0.125
        _WindCFrequency("_WindCFrequency", Float) = 11.7
        _WindCTiling("_WindCTiling", Vector) = (0.77,3,0)
        _WindCWrap("_WindCWrap", Vector) = (0.5,0.5,0)

        [Header(Lighting)]
        _RandomNormal("_RandomNormal", Float) = 0.15

        //make SRP batcher happy
        [HideInInspector]_PivotPosWS("_PivotPosWS", Vector) = (0,0,0,0)
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

        Pass
        {
            Cull off //use default culling because this shader is billboard 
            ZTest Lequal
            Blend SrcAlpha OneMinusSrcAlpha
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // -------------------------------------
            // Universal Render Pipeline keywords
            // When doing custom shaders you most often want to copy and paste these #pragmas
            // These multi_compile variants are stripped from the build depending on:
            // 1) Settings in the URP Asset assigned in the GraphicsSettings at build time
            // e.g If you disabled AdditionalLights in the asset then all _ADDITIONA_LIGHTS variants
            // will be stripped from build
            // 2) Invalid combinations are stripped. e.g variants with _MAIN_LIGHT_SHADOWS_CASCADE
            // but not _MAIN_LIGHT_SHADOWS are invalid and therefore stripped.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _  
            #pragma multi_compile _ _WIND
            #pragma multi_compile _ _INDIRECT
            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fog
            // -------------------------------------
			#pragma multi_compile_instancing
            


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
				float4 positionOS   : POSITION;
				float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                half3 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float3 _PivotPosWS;
                float2 _BoundSize;
                
                float _GrassWidth;
                float _GrassHeight;
                
                float _WindAIntensity;
                float _WindAFrequency;
                float2 _WindATiling;
                float2 _WindAWrap;

                float _WindBIntensity;
                float _WindBFrequency;
                float2 _WindBTiling;
                float2 _WindBWrap;

                float _WindCIntensity;
                float _WindCFrequency;
                float2 _WindCTiling;
                float2 _WindCWrap;
                
                half3 _BaseColor;
                half3 _GroundColor;

                half _RandomNormal;

                int _IsBillboard;

                float4 _RenderTexture_ST; 
                sampler2D _RenderTexture;
                float4 _HeightCameraPos[9];
                float4 _HeightCameraPosSingle;
                float4 _HeightCameraNFSH;//near far size
                float4x4 _CameraInvTRS;
                StructuredBuffer<float3> _AllInstancesTransformBuffer;
                StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBuffer;
                StructuredBuffer<uint> _VisibleInstanceOnlyTransformIDBufferBillboard;
            CBUFFER_END

            sampler2D _GrassBendingRT;

            half3 ApplySingleDirectLight(Light light, half3 N, half3 V, half3 albedo, half positionOSY)
            {
           //     return V;
                half3 H = normalize(light.direction + V);

                //direct diffuse 
                half directDiffuse = dot(N, light.direction) ; //half lambert, to fake grass SSS
#if _SPECULAR 
				//direct specular
				float directSpecular = saturate(dot(N, H));
				//pow(directSpecular,8)
				directSpecular *= directSpecular;
				directSpecular *= directSpecular;
				directSpecular *= directSpecular;
				//directSpecular *= directSpecular; //enable this line = change to pow(directSpecular,16)

				//add direct directSpecular to result
				directSpecular *= 0.1 * positionOSY;//only apply directSpecular to grass's top area, to simulate grass AO
#else
				float directSpecular = 0;
#endif

                half3 lighting = light.color * (light.shadowAttenuation * light.distanceAttenuation);
                half3 result = (albedo * directDiffuse + directSpecular) * lighting;
                return result; 
            }
			
			Varyings sim_vert(Attributes IN, uint instanceID : SV_InstanceID)
			{
				Varyings OUT;
				float3 perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];//we pre-transform to posWS in C# now

				OUT.positionCS = TransformWorldToHClip(IN.positionOS + perGrassPivotPosWS);
                
				half3 albedo = lerp(_GroundColor, _BaseColor, IN.positionOS.y);//you can use texture if you wish to
				half3 lightingResult = SampleSH(0) * albedo;

				//fog
				float fogFactor = ComputeFogFactor(OUT.positionCS.z);
				// Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
				// with a custom one.
				OUT.color = MixFog(lightingResult, fogFactor);

				return OUT;
			}

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                float3 perGrassPivotPosWS;
#if _INDIRECT
                if(_IsBillboard == 1){
                    perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBufferBillboard[instanceID]];
                 }else{
                     perGrassPivotPosWS = _AllInstancesTransformBuffer[_VisibleInstanceOnlyTransformIDBuffer[instanceID]];         
                 }
				perGrassPivotPosWS = perGrassPivotPosWS + float3(IN.uv.x, 0, IN.uv.y);
#else
                 UNITY_SETUP_INSTANCE_ID(IN);
                 VertexPositionInputs originalInput = GetVertexPositionInputs(float3(0, 0, 0));
                 float4 posWS = mul(_CameraInvTRS ,float4(originalInput.positionWS,0));
				 perGrassPivotPosWS = originalInput.positionWS + float3(IN.uv.x, 0, IN.uv.y);
#endif           
                //IMPOSE HEIGHT MAP
                int i = 0;
                for(i=0 ; i<9 ; i++){ 
                    float dis_x = abs(_HeightCameraPos[i].x-perGrassPivotPosWS.x);
                    float dis_z = abs(_HeightCameraPos[i].z-perGrassPivotPosWS.z);
                    if(dis_x<=_HeightCameraNFSH.z&&dis_z<=_HeightCameraNFSH.z){
                        break;
                    }
                }
                int shift_x = i%3;
                int shift_y = 2- i/3;
                float t1 = (2*_HeightCameraNFSH.z* shift_x+ perGrassPivotPosWS.x-(_HeightCameraPos[i].x-_HeightCameraNFSH.z))/(3*2*_HeightCameraNFSH.z);
                float t2 = (2*_HeightCameraNFSH.z* shift_y+ perGrassPivotPosWS.z-(_HeightCameraPos[i].z-_HeightCameraNFSH.z))/(3*2*_HeightCameraNFSH.z) ;
                float2 depthGrass = tex2Dlod(_RenderTexture, float4(t1,t2, 0, 0)).rg;
				float depth = depthGrass.x;
#if defined(UNITY_REVERSED_Z)
                    depth = 1.0 - depth;//it happens in gl-like and vulcan(mobile)
#endif
                float _min = _HeightCameraNFSH.w - _HeightCameraNFSH.y;
                float _max = _HeightCameraNFSH.w - _HeightCameraNFSH.x;
                float height;

				height =  _min + (_max - _min) * depth - 1.2; //opengl
                perGrassPivotPosWS.y = height;  
               
                float perGrassHeight = lerp(2,5,(sin(perGrassPivotPosWS.x*23.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55)) * _GrassHeight * (1.0 - depthGrass.y);

                //rotation(make grass LookAt() camera just like a billboard)
                //=========================================
                float3 cameraTransformRightWS = UNITY_MATRIX_V[0].xyz;//UNITY_MATRIX_V[0].xyz == world space camera Right unit vector
                float3 cameraTransformUpWS = float3(0,1,0); // UNITY_MATRIX_V[1].xyz;//UNITY_MATRIX_V[1].xyz == world space camera Up unit vector
                float3 cameraTransformForwardWS = -UNITY_MATRIX_V[2].xyz;//UNITY_MATRIX_V[2].xyz == -1 * world space camera Forward unit vector
                float3 randomRight = normalize(float3(sin(perGrassPivotPosWS.x*95.4643 + perGrassPivotPosWS.z),0.0,sin(perGrassPivotPosWS.x*9.4643 + perGrassPivotPosWS.z)));
                float3 positionOS;
                //Expand Billboard (billboard Left+right)
                if(_IsBillboard != 1){
                    positionOS = IN.positionOS.x * randomRight * _GrassWidth * (sin(perGrassPivotPosWS.x*95.4643 + perGrassPivotPosWS.z) * 0.45 + 0.55);//random width from posXZ, min 0.1
                }else{
                    positionOS.x = IN.positionOS.x *  _GrassWidth ;
                    positionOS.z = IN.positionOS.z *  _GrassWidth ;
                }
                //Expand Billboard (billboard Up)
                positionOS += IN.positionOS.y * cameraTransformUpWS;         
                //=========================================

                //BENDING STUFF
                //get "is grass stepped" data(bending) from RT
                float2 grassBendingUV = ((perGrassPivotPosWS.xz - _PivotPosWS.xz) / _BoundSize) * 0.5 + 0.5;//claculate where is this grass inside bound (can optimize to 2 MAD)
				float stepped = tex2Dlod(_GrassBendingRT, float4(grassBendingUV, 0, 0)).x;

                //bending by RT (hard code)
                float3 bendDir = cameraTransformForwardWS;
                bendDir.xz *= 0.5; //make grass shorter when bending, looks better
                bendDir.y = min(-0.5,bendDir.y);//prevent grass become too long if camera forward is / near parallel to ground
                positionOS = lerp(positionOS.xyz + bendDir * positionOS.y / -bendDir.y, positionOS.xyz, stepped * 0.85 + 0.1);//don't fully bend, will produce ZFighting

                if(_IsBillboard != 1){
                    //per grass height scale
                    positionOS.y *= perGrassHeight;
                }else{
                    positionOS.y *= _GrassHeight*5;
                }
                

                //camera distance scale (make grass width larger if grass is far away to camera, to hide smaller than pixel size triangle flicker)        
                float3 viewWS = _WorldSpaceCameraPos - perGrassPivotPosWS;
                float ViewWSLength = length(viewWS);
                positionOS += cameraTransformRightWS * IN.positionOS.x * max(0, ViewWSLength * 0.0225);
                
                //move grass posOS -> posWS
                float3 positionWS = positionOS + perGrassPivotPosWS;

                //wind animation (biilboard Left Right direction only sin wave)            
                float wind = 0;
#if _WIND
                wind += (sin(_Time.y * _WindAFrequency + perGrassPivotPosWS.x * _WindATiling.x + perGrassPivotPosWS.z * _WindATiling.y)*_WindAWrap.x+_WindAWrap.y) * _WindAIntensity; //windA
                wind += (sin(_Time.y * _WindBFrequency + perGrassPivotPosWS.x * _WindBTiling.x + perGrassPivotPosWS.z * _WindBTiling.y)*_WindBWrap.x+_WindBWrap.y) * _WindBIntensity; //windB
                wind += (sin(_Time.y * _WindCFrequency + perGrassPivotPosWS.x * _WindCTiling.x + perGrassPivotPosWS.z * _WindCTiling.y)*_WindCWrap.x+_WindCWrap.y) * _WindCIntensity; //windC
                wind *= IN.positionOS.y; //wind only affect top region, don't affect root region
                if(_IsBillboard){ wind = 0;}
                float3 windOffset = cameraTransformRightWS * wind; //swing using billboard left right direction
                positionWS.xyz += windOffset;
#endif
                //vertex position logic done, complete posWS -> posCS
                OUT.positionCS = TransformWorldToHClip(positionWS);

                /////////////////////////////////////////////////////////////////////
                //lighting & color
                /////////////////////////////////////////////////////////////////////

                //lighting data
                Light mainLight;
#if _MAIN_LIGHT_SHADOWS
                mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS)); // TransformWorldToShadowCoord(positionWS)      LEAD TO BLINKING
#else
                mainLight = GetMainLight();
#endif
                half3 randomAddToN = (_RandomNormal* sin(perGrassPivotPosWS.x * 82.32523 + perGrassPivotPosWS.z) + wind * -0.25) * cameraTransformRightWS;//random normal per grass 
                //default grass's normal is pointing 100% upward in world space, it is an important but simple grass normal trick
                //-apply random to normal else lighting is too uniform
                //-apply cameraTransformForwardWS to normal because grass is billboard
                half3 N = normalize(half3(0,1,0) + randomAddToN - cameraTransformForwardWS*0.5);

                half3 V = viewWS / ViewWSLength;
                half3 albedo ;
                albedo = lerp(_GroundColor,_BaseColor, IN.positionOS.y);
                 
                //indirect
                half3 lightingResult = SampleSH(0) * albedo; 

                //main direct light
                lightingResult += ApplySingleDirectLight(mainLight, N, V, albedo, positionOS.y);

                // Additional lights loop
#if _ADDITIONAL_LIGHTS

                // Returns the amount of lights affecting the object being renderer.
                // These lights are culled per-object in the forward renderer
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    // Similar to GetMainLight, but it takes a for-loop index. This figures out the
                    // per-object light index and samples the light buffer accordingly to initialized the
                    // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
                    Light light = GetAdditionalLight(0, positionWS);

                    // Same functions used to shade the main light.
                    lightingResult += ApplySingleDirectLight(light, N, V, albedo, positionOS.y);
                }
#endif

                //fog
                float fogFactor = ComputeFogFactor(OUT.positionCS.z);
                // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
                // with a custom one.
                OUT.color = MixFog(lightingResult, fogFactor);
                return OUT;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                half alpha = 1.0;
                return half4(IN.color ,alpha);
            }
            ENDHLSL
        }
        
      
        //copy pass, change LightMode to ShadowCaster will make grass cast shadow
        //copy pass, change LightMode to DepthOnly will make grass render into _CameraDepthTexture
    }
}