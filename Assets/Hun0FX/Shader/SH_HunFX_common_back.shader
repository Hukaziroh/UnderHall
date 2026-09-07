// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "HunFX/SH_HunFX_common_back"
{
	Properties
	{
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		[HDR]_color1("color1", Color) = (2,2,2,1)
		[HDR]_color2("color2", Color) = (0,0,0,1)
		_ColorIntensity("ColorIntensity", Float) = 1
		_MainTexture("MainTexture", 2D) = "white" {}
		_MainTex_uv("MainTex_uv", Vector) = (0,0,1,1)
		_MainTex_speed("MainTex_speed", Vector) = (0,0,0,0)
		_DissolveTex("DissolveTex", 2D) = "white" {}
		_DissolveTex_uv("DissolveTex_uv", Vector) = (0,0,1,1)
		_DissolveTex_speed("DissolveTex_speed", Vector) = (0,0,0,0)
		_Smoothstep("Smoothstep", Vector) = (0,1,0,0)
		_VOTex("VOTex", 2D) = "white" {}
		_VOTex_uv("VOTex_uv", Vector) = (0,0,1,1)
		_VOTex_speed("VOTex_speed", Vector) = (0,0,0,0)
		_VOintensity("VOintensity", Float) = 0
		[Toggle(_USEFRESNEL_ON)] _UseFresnel("UseFresnel", Float) = 0
		_Mask("Mask", 2D) = "white" {}
		_Mask_udrl("Mask_udrl", Vector) = (0,0,0,0)
		[Toggle(_USEPOSXSCROLL_ON)] _UsePosXScroll("UsePosXScroll", Float) = 0
		_SubDissolveTex("SubDissolveTex", 2D) = "white" {}
		[Toggle]_UseSubDissTex("UseSubDissTex", Float) = 0
		_SubDissolveTex_multi("SubDissolveTex_multi", Float) = 1
		_SubDissolveTex_add("SubDissolveTex_add", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			Cull Front
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR
				#pragma shader_feature_local _USEPOSXSCROLL_ON
				#pragma shader_feature_local _USEFRESNEL_ON


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					float3 ase_normal : NORMAL;
					float4 ase_texcoord1 : TEXCOORD1;
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					float4 ase_texcoord3 : TEXCOORD3;
					float4 ase_texcoord4 : TEXCOORD4;
					float4 ase_texcoord5 : TEXCOORD5;
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform sampler2D _VOTex;
				uniform float2 _VOTex_speed;
				uniform float4 _VOTex_uv;
				uniform float _VOintensity;
				uniform float4 _color2;
				uniform float4 _color1;
				uniform sampler2D _MainTexture;
				uniform float2 _MainTex_speed;
				uniform float4 _MainTex_uv;
				uniform float _ColorIntensity;
				uniform float2 _Smoothstep;
				uniform sampler2D _DissolveTex;
				uniform float2 _DissolveTex_speed;
				uniform float4 _DissolveTex_uv;
				uniform float _UseSubDissTex;
				uniform sampler2D _SubDissolveTex;
				uniform float4 _SubDissolveTex_ST;
				uniform float _SubDissolveTex_multi;
				uniform float _SubDissolveTex_add;
				uniform sampler2D _Mask;
				uniform float4 _Mask_ST;
				uniform float4 _Mask_udrl;


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					float2 texCoord26 = v.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult28 = (float2(_VOTex_uv.x , _VOTex_uv.y));
					float2 appendResult29 = (float2(_VOTex_uv.z , _VOTex_uv.w));
					float2 panner22 = ( 1.0 * _Time.y * _VOTex_speed + ( ( texCoord26 + appendResult28 ) * appendResult29 ));
					
					float3 ase_worldPos = mul(unity_ObjectToWorld, float4( (v.vertex).xyz, 1 )).xyz;
					o.ase_texcoord4.xyz = ase_worldPos;
					float3 ase_worldNormal = UnityObjectToWorldNormal(v.ase_normal);
					o.ase_texcoord5.xyz = ase_worldNormal;
					
					o.ase_texcoord3 = v.ase_texcoord1;
					
					//setting value to unused interpolator channels and avoid initialization warnings
					o.ase_texcoord4.w = 0;
					o.ase_texcoord5.w = 0;

					v.vertex.xyz += ( ( tex2Dlod( _VOTex, float4( panner22, 0, 0.0) ).r * v.ase_normal ) * _VOintensity );
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 texCoord13 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float4 texCoord59 = i.ase_texcoord3;
					texCoord59.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult63 = (float2(0.0 , texCoord59.y));
					float2 appendResult62 = (float2(texCoord59.y , 0.0));
					#ifdef _USEPOSXSCROLL_ON
					float2 staticSwitch60 = appendResult62;
					#else
					float2 staticSwitch60 = appendResult63;
					#endif
					float2 appendResult18 = (float2(_MainTex_uv.x , _MainTex_uv.y));
					float2 appendResult19 = (float2(_MainTex_uv.z , _MainTex_uv.w));
					float2 panner15 = ( 1.0 * _Time.y * _MainTex_speed + ( ( ( texCoord13 + staticSwitch60 ) + appendResult18 ) * appendResult19 ));
					float4 tex2DNode1 = tex2D( _MainTexture, panner15 );
					float4 lerpResult9 = lerp( _color2 , _color1 , tex2DNode1.r);
					float4 texCoord14 = i.ase_texcoord3;
					texCoord14.xy = i.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
					float4 break6 = ( ( lerpResult9 * ( _ColorIntensity + texCoord14.z ) ) * i.color );
					float2 texCoord35 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 appendResult37 = (float2(_DissolveTex_uv.x , _DissolveTex_uv.y));
					float2 appendResult38 = (float2(_DissolveTex_uv.z , _DissolveTex_uv.w));
					float2 panner31 = ( 1.0 * _Time.y * _DissolveTex_speed + ( ( ( staticSwitch60 + texCoord35 ) + appendResult37 ) * appendResult38 ));
					float2 uv_SubDissolveTex = i.texcoord.xy * _SubDissolveTex_ST.xy + _SubDissolveTex_ST.zw;
					float smoothstepResult40 = smoothstep( _Smoothstep.x , _Smoothstep.y , ( ( tex2D( _DissolveTex, panner31 ).g - (( _UseSubDissTex )?( ( ( tex2D( _SubDissolveTex, uv_SubDissolveTex ).r * _SubDissolveTex_multi ) + _SubDissolveTex_add ) ):( 0.0 )) ) - texCoord14.x ));
					float temp_output_7_0 = ( tex2DNode1.a * ( i.color.a * smoothstepResult40 ) );
					float3 ase_worldPos = i.ase_texcoord4.xyz;
					float3 ase_worldViewDir = UnityWorldSpaceViewDir(ase_worldPos);
					ase_worldViewDir = normalize(ase_worldViewDir);
					float3 ase_worldNormal = i.ase_texcoord5.xyz;
					float fresnelNdotV47 = dot( ase_worldNormal, ase_worldViewDir );
					float fresnelNode47 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV47, 5.0 ) );
					#ifdef _USEFRESNEL_ON
					float staticSwitch48 = ( temp_output_7_0 * fresnelNode47 );
					#else
					float staticSwitch48 = temp_output_7_0;
					#endif
					float2 uv_Mask = i.texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
					float2 texCoord72 = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float smoothstepResult76 = smoothstep( 0.0 , _Mask_udrl.y , texCoord72.y);
					float smoothstepResult77 = smoothstep( 0.0 , _Mask_udrl.x , ( 1.0 - texCoord72.y ));
					float smoothstepResult78 = smoothstep( 0.0 , _Mask_udrl.w , texCoord72.x);
					float smoothstepResult79 = smoothstep( 0.0 , _Mask_udrl.z , ( 1.0 - texCoord72.x ));
					float4 appendResult8 = (float4(break6.r , break6.g , break6.b , ( ( staticSwitch48 * tex2D( _Mask, uv_Mask ).r ) * ( ( smoothstepResult76 * smoothstepResult77 ) * ( smoothstepResult78 * smoothstepResult79 ) ) )));
					

					fixed4 col = appendResult8;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	CustomEditor "ASEMaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.TextureCoordinatesNode;59;-3088,288;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;61;-3024,544;Inherit;False;Constant;_Float0;Float 0;17;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;62;-2784,416;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;-2768,544;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;36;-2080,688;Inherit;False;Property;_DissolveTex_uv;DissolveTex_uv;7;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,-0.51,1,2;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;60;-2576,432;Inherit;False;Property;_UsePosXScroll;UsePosXScroll;17;0;Create;True;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;35;-2336,576;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;37;-1888,688;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-1984,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;34;-1824,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;38;-1856,800;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;65;-1536,976;Inherit;True;Property;_SubDissolveTex;SubDissolveTex;18;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;68;-1536,1200;Inherit;False;Property;_SubDissolveTex_multi;SubDissolveTex_multi;20;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1680,560;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;39;-1680,768;Inherit;False;Property;_DissolveTex_speed;DissolveTex_speed;8;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;69;-1200,1056;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-1232,1200;Inherit;False;Property;_SubDissolveTex_add;SubDissolveTex_add;21;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;17;-1904,48;Inherit;False;Property;_MainTex_uv;MainTex_uv;4;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;31;-1472,624;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;70;-1024.716,1079.287;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-2352,-96;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;18;-1712,48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;32;-1280,624;Inherit;True;Property;_DissolveTex;DissolveTex;6;0;Create;True;0;0;0;False;0;False;-1;None;6f8ab9b860f34974a934aa60df009c70;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;66;-944,880;Inherit;False;Property;_UseSubDissTex;UseSubDissTex;19;0;Create;True;0;0;0;False;0;False;0;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;85;-2000,-80;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;20;-1648,-80;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;19;-1680,160;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;67;-720,640;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;14;-656,880;Inherit;False;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-1475.706,-95.92148;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;16;-1504,128;Inherit;False;Property;_MainTex_speed;MainTex_speed;5;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;41;-160,768;Inherit;False;Property;_Smoothstep;Smoothstep;9;0;Create;True;0;0;0;False;0;False;0,1;0,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleSubtractOpNode;50;-304,640;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;15;-1296,-16;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;4;-464,272;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SmoothstepOpNode;40;-160,640;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;27;640,1568;Inherit;False;Property;_VOTex_uv;VOTex_uv;11;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;-64,352;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;1;-1040,16;Inherit;True;Property;_MainTexture;MainTexture;3;0;Create;True;0;0;0;False;0;False;-1;None;6f8ab9b860f34974a934aa60df009c70;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;656,1440;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;28;832,1568;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;72;720,704;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;128,240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;47;112,560;Inherit;False;Standard;WorldNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;896,1440;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;29;864,1680;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;73;496,768;Inherit;False;Property;_Mask_udrl;Mask_udrl;16;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;74;976,752;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;75;992,992;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;12;-944,-448;Inherit;False;Property;_color2;color2;1;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,1;0.2358491,0.2358491,0.2358491,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;11;-944,-624;Inherit;False;Property;_color1;color1;0;1;[HDR];Create;True;0;0;0;False;0;False;2,2,2,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;2;-608,-64;Inherit;False;Property;_ColorIntensity;ColorIntensity;2;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;462.8087,459.1089;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;1072,1424;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;30;1040,1648;Inherit;False;Property;_VOTex_speed;VOTex_speed;12;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SmoothstepOpNode;76;1136,560;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;77;1136,704;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;78;1136,848;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;79;1136,992;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;9;-592,-480;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;84;-432,16;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-286,-12;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;48;656,240;Inherit;True;Property;_UseFresnel;UseFresnel;14;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;52;736,400;Inherit;True;Property;_Mask;Mask;15;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;22;1248,1504;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;1360,624;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;81;1392,928;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;32,0;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;1072,224;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;23;1456,1488;Inherit;True;Property;_VOTex;VOTex;10;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NormalVertexDataNode;45;1488,1712;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;82;1584,768;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;6;1648,-32;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;43;1712,1840;Inherit;False;Property;_VOintensity;VOintensity;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;1776,1568;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;1383.779,315.9991;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;8;1808,16;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;2000,1536;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;2160,16;Float;False;True;-1;2;ASEMaterialInspector;0;11;HunFX/SH_HunFX_common_back;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;False;True;2;5;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;True;True;1;False;;False;True;True;True;True;False;0;False;;False;False;False;False;False;False;False;False;False;True;2;False;;True;3;False;;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;62;0;59;2
WireConnection;62;1;61;0
WireConnection;63;0;61;0
WireConnection;63;1;59;2
WireConnection;60;1;63;0
WireConnection;60;0;62;0
WireConnection;37;0;36;1
WireConnection;37;1;36;2
WireConnection;64;0;60;0
WireConnection;64;1;35;0
WireConnection;34;0;64;0
WireConnection;34;1;37;0
WireConnection;38;0;36;3
WireConnection;38;1;36;4
WireConnection;33;0;34;0
WireConnection;33;1;38;0
WireConnection;69;0;65;1
WireConnection;69;1;68;0
WireConnection;31;0;33;0
WireConnection;31;2;39;0
WireConnection;70;0;69;0
WireConnection;70;1;71;0
WireConnection;18;0;17;1
WireConnection;18;1;17;2
WireConnection;32;1;31;0
WireConnection;66;1;70;0
WireConnection;85;0;13;0
WireConnection;85;1;60;0
WireConnection;20;0;85;0
WireConnection;20;1;18;0
WireConnection;19;0;17;3
WireConnection;19;1;17;4
WireConnection;67;0;32;2
WireConnection;67;1;66;0
WireConnection;21;0;20;0
WireConnection;21;1;19;0
WireConnection;50;0;67;0
WireConnection;50;1;14;1
WireConnection;15;0;21;0
WireConnection;15;2;16;0
WireConnection;40;0;50;0
WireConnection;40;1;41;1
WireConnection;40;2;41;2
WireConnection;42;0;4;4
WireConnection;42;1;40;0
WireConnection;1;1;15;0
WireConnection;28;0;27;1
WireConnection;28;1;27;2
WireConnection;7;0;1;4
WireConnection;7;1;42;0
WireConnection;25;0;26;0
WireConnection;25;1;28;0
WireConnection;29;0;27;3
WireConnection;29;1;27;4
WireConnection;74;0;72;2
WireConnection;75;0;72;1
WireConnection;49;0;7;0
WireConnection;49;1;47;0
WireConnection;24;0;25;0
WireConnection;24;1;29;0
WireConnection;76;0;72;2
WireConnection;76;2;73;2
WireConnection;77;0;74;0
WireConnection;77;2;73;1
WireConnection;78;0;72;1
WireConnection;78;2;73;4
WireConnection;79;0;75;0
WireConnection;79;2;73;3
WireConnection;9;0;12;0
WireConnection;9;1;11;0
WireConnection;9;2;1;1
WireConnection;84;0;2;0
WireConnection;84;1;14;3
WireConnection;3;0;9;0
WireConnection;3;1;84;0
WireConnection;48;1;7;0
WireConnection;48;0;49;0
WireConnection;22;0;24;0
WireConnection;22;2;30;0
WireConnection;80;0;76;0
WireConnection;80;1;77;0
WireConnection;81;0;78;0
WireConnection;81;1;79;0
WireConnection;5;0;3;0
WireConnection;5;1;4;0
WireConnection;53;0;48;0
WireConnection;53;1;52;1
WireConnection;23;1;22;0
WireConnection;82;0;80;0
WireConnection;82;1;81;0
WireConnection;6;0;5;0
WireConnection;46;0;23;1
WireConnection;46;1;45;0
WireConnection;83;0;53;0
WireConnection;83;1;82;0
WireConnection;8;0;6;0
WireConnection;8;1;6;1
WireConnection;8;2;6;2
WireConnection;8;3;83;0
WireConnection;44;0;46;0
WireConnection;44;1;43;0
WireConnection;0;0;8;0
WireConnection;0;1;44;0
ASEEND*/
//CHKSM=891B12E7045DA1302DA4AC12800D6BD10552D785