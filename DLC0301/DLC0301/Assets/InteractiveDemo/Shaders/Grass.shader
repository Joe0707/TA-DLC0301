// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Grass"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[Toggle(_DYNAMIC_ON_ON)] _DYNAMIC_ON("是否开启交互", Float) = 0
		_DynamicMotionIntensity("交互强度", Float) = 1
		_Albedo("Albedo", 2D) = "white" {}
		_AlphaCutoff("Alpha Cutoff", Range( 0 , 1)) = 0.35
		[Header(Color Settings)][Space(5)][KeywordEnum(UV,VertexColor,PositionOS)] _ColorGradientMode("垂直渐变模式", Float) = 0
		[Toggle]_UseTexColor("使用纹理颜色", Float) = 0
		_BottomColor("底部颜色", Color) = (0.4313726,1,0.007843138,0)
		_TipColor("顶部颜色", Color) = (0.4313726,1,0.007843138,0)
		_ColorGradient("渐变颜色控制", Float) = 1
		[Toggle(_GROUNDBLENDING_ON)] _GroundBlending("是否开启地形融合", Float) = 0
		_BlendGradient("地形融合控制", Float) = 1
		_IndirectLightIntensity("环境光强度", Range( 1 , 10)) = 1
		[Header(Wind Settings)][Space(5)][Toggle(_WIND_ON_ON)] _WIND_ON("是否开启风力", Float) = 0
		[KeywordEnum(UV,VertexColor,PositionOS)] _WindGradient("WindGradient", Float) = 0
		[Toggle(_PIVOTBAKE_ON)] _PIVOTBAKE("使用预烘焙轴点", Float) = 0
		_WindIntensity("WindIntensity", Float) = 0.2
		_WindSpeed("Wind Speed", Float) = 0.5
		_WindDirection("Wind Direction", Range( 0 , 360)) = 45
		_WindSizeBig("Wind Size Big", Float) = 10
		_WindSizeSmall("Wind Size Small", Float) = 4
		_WindLine("风线贴图", 2D) = "black" {}
		_WindLineColor("风线颜色", Color) = (0.07843138,0.2980392,0,0)
		_WindLineColorGradient("风线颜色范围", Float) = 1
		_WindLineScale("风线大小", Float) = 50
		_WindLineRotate("风线方向", Range( 0 , 2)) = 0.8
		_WindLineSpeed("风线速度", Vector) = (0,0.3,0,0)
		[Toggle]_FixedNormal("指定法线", Float) = 0
		_SpecifyNormal("指定法线方向", Vector) = (0,1,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest" "UniversalMaterialType"="Unlit" "NatureRendererInstancing"="True" }

		Cull Off
		AlphaToMask Off

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		// ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForwardOnly" }

			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma shader_feature _ _SAMPLE_GI
			#pragma multi_compile _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS SHADERPASS_UNLIT

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_FRAG_SHADOWCOORDS
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#pragma shader_feature_local _GROUNDBLENDING_ON
			#pragma shader_feature_local _COLORGRADIENTMODE_UV _COLORGRADIENTMODE_VERTEXCOLOR _COLORGRADIENTMODE_POSITIONOS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord : TEXCOORD1;
				#endif
				#ifdef ASE_FOG
					float fogFactor : TEXCOORD2;
				#endif
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 lightmapUVOrVertexSH : TEXCOORD5;
				float3 ase_normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);
			TEXTURE2D(_TerrainDiffuse);
			float4 _OrthographicCamPos;
			float _OrthographicCamSize;
			SAMPLER(sampler_TerrainDiffuse);
			TEXTURE2D(_WindLine);
			SAMPLER(sampler_WindLine);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			
			float3 ASEIndirectDiffuse( float2 uvStaticLightmap, float3 normalWS )
			{
			#ifdef LIGHTMAP_ON
				return SampleLightmap( uvStaticLightmap, normalWS );
			#else
				return SampleSH(normalWS);
			#endif
			}
			

			VertexOutput VertexFunction ( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				float3 ase_worldNormal = TransformObjectToWorldNormal(v.ase_normal);
				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( ase_worldNormal, o.lightmapUVOrVertexSH.xyz );
				
				o.ase_texcoord3.xy = v.ase_texcoord.xy;
				o.ase_color = v.ase_color;
				o.ase_texcoord4 = v.vertex;
				o.ase_normal = v.ase_normal;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord3.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = FinalVertexPosition485;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				#ifdef ASE_FOG
					o.fogFactor = ComputeFogFactor( positionCS.z );
				#endif

				o.clipPos = positionCS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 texcoord1 : TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				o.texcoord1 = v.texcoord1;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.worldPos;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float4 temp_cast_0 = (1.0).xxxx;
				float2 uv_Albedo = IN.ase_texcoord3.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float4 lerpResult321 = lerp( temp_cast_0 , tex2DNode205 , _UseTexColor);
				float4 TexColor319 = lerpResult321;
				float2 texCoord200 = IN.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_COLORGRADIENTMODE_UV)
				float staticSwitch202 = texCoord200.y;
				#elif defined(_COLORGRADIENTMODE_VERTEXCOLOR)
				float staticSwitch202 = IN.ase_color.g;
				#elif defined(_COLORGRADIENTMODE_POSITIONOS)
				float staticSwitch202 = IN.ase_texcoord4.xyz.y;
				#else
				float staticSwitch202 = texCoord200.y;
				#endif
				float VerticalFade201 = staticSwitch202;
				float clampResult211 = clamp( ( VerticalFade201 * _ColorGradient ) , 0.0 , 1.0 );
				float4 lerpResult206 = lerp( _BottomColor , _TipColor , clampResult211);
				float4 temp_output_305_0 = ( TexColor319 * lerpResult206 );
				float4 TerrainColor351 = SAMPLE_TEXTURE2D( _TerrainDiffuse, sampler_TerrainDiffuse, ( ( ( (WorldPosition).xz - (_OrthographicCamPos).xz ) / ( _OrthographicCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ) );
				float clampResult375 = clamp( ( VerticalFade201 * _BlendGradient ) , 0.0 , 1.0 );
				float4 lerpResult382 = lerp( TerrainColor351 , temp_output_305_0 , clampResult375);
				#ifdef _GROUNDBLENDING_ON
				float4 staticSwitch384 = lerpResult382;
				#else
				float4 staticSwitch384 = temp_output_305_0;
				#endif
				float cos278 = cos( ( _WindLineRotate * PI ) );
				float sin278 = sin( ( _WindLineRotate * PI ) );
				float2 rotator278 = mul( ( (WorldPosition).xz / _WindLineScale ) - float2( 0,0 ) , float2x2( cos278 , -sin278 , sin278 , cos278 )) + float2( 0,0 );
				float2 panner283 = ( 0.1 * _Time.y * _WindLineSpeed + rotator278);
				float clampResult399 = clamp( ( VerticalFade201 * _WindLineColorGradient ) , 0.0 , 1.0 );
				#ifdef _WIND_ON_ON
				float4 staticSwitch517 = ( SAMPLE_TEXTURE2D( _WindLine, sampler_WindLine, panner283 ).r * _WindLineColor * clampResult399 );
				#else
				float4 staticSwitch517 = float4( float3(0,0,0) , 0.0 );
				#endif
				float4 WindLine276 = staticSwitch517;
				float4 BaseColor121 = ( staticSwitch384 + WindLine276 );
				float ase_lightAtten = 0;
				Light ase_mainLight = GetMainLight( ShadowCoords );
				ase_lightAtten = ase_mainLight.distanceAttenuation * ase_mainLight.shadowAttenuation;
				float3 lerpResult331 = lerp( IN.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 objToWorldDir327 = normalize( mul( GetObjectToWorldMatrix(), float4( lerpResult331, 0 ) ).xyz );
				float3 WorldNormal333 = objToWorldDir327;
				float3 bakedGI177 = ASEIndirectDiffuse( IN.lightmapUVOrVertexSH.xy, WorldNormal333);
				MixRealtimeAndBakedGI(ase_mainLight, WorldNormal333, bakedGI177, half4(0,0,0,0));
				float4 CombineColor393 = ( ( BaseColor121 * ase_lightAtten ) + ( ( 1.0 - ase_lightAtten ) * float4( bakedGI177 , 0.0 ) * BaseColor121 * _IndirectLightIntensity ) );
				
				float OpacityMask96 = tex2DNode205.a;
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = CombineColor393.rgb;
				float Alpha = OpacityMask96;
				float AlphaClipThreshold = _AlphaCutoff;
				float AlphaClipThresholdShadow = 0.5;

				#ifdef _ALPHATEST_ON
					clip( Alpha - AlphaClipThreshold );
				#endif

				#if defined(_DBUFFER)
					ApplyDecalToBaseColor(IN.clipPos, Color);
				#endif

				#if defined(_ALPHAPREMULTIPLY_ON)
				Color *= Alpha;
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#ifdef ASE_FOG
					Color = MixFog( Color, IN.fogFactor );
				#endif

				return half4( Color, Alpha );
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0
			AlphaToMask Off

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = FinalVertexPosition485;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					o.worldPos = positionWS;
				#endif

				o.clipPos = TransformWorldToHClip( positionWS );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
					float3 WorldPosition = IN.worldPos;
				#endif

				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float2 uv_Albedo = IN.ase_texcoord2.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float OpacityMask96 = tex2DNode205.a;
				

				float Alpha = OpacityMask96;
				float AlphaClipThreshold = _AlphaCutoff;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }

			Cull Off

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			int _ObjectId;
			int _PassValue;

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = FinalVertexPosition485;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				o.clipPos = TransformWorldToHClip(positionWS);

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_Albedo = IN.ase_texcoord.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float OpacityMask96 = tex2DNode205.a;
				

				surfaceDescription.Alpha = OpacityMask96;
				surfaceDescription.AlphaClipThreshold = _AlphaCutoff;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}
			ENDHLSL
		}

		
		Pass
		{
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define SHADERPASS SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			float4 _SelectionID;


			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = FinalVertexPosition485;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				o.clipPos = TransformWorldToHClip(positionWS);
				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_Albedo = IN.ase_texcoord.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float OpacityMask96 = tex2DNode205.a;
				

				surfaceDescription.Alpha = OpacityMask96;
				surfaceDescription.AlphaClipThreshold = _AlphaCutoff;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = 0;
				outColor = _SelectionID;

				return outColor;
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On


			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define VARYINGS_NEED_NORMAL_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = FinalVertexPosition485;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);

				o.clipPos = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  normalWS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_Albedo = IN.ase_texcoord1.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float OpacityMask96 = tex2DNode205.a;
				

				surfaceDescription.Alpha = OpacityMask96;
				surfaceDescription.AlphaClipThreshold = _AlphaCutoff;

				#if _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				float3 normalWS = IN.normalWS;

				return half4(NormalizeNormalPerPixel(normalWS), 0.0);
			}

			ENDHLSL
		}

		
		Pass
		{
			
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }

			ZTest LEqual
			ZWrite On

			HLSLPROGRAM

			#pragma multi_compile_instancing
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 120107
			#define ASE_USING_SAMPLING_MACROS 1


			#pragma exclude_renderers glcore gles gles3 
			#pragma vertex vert
			#pragma fragment frag

			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD1
			#define VARYINGS_NEED_NORMAL_WS
			#define VARYINGS_NEED_TANGENT_WS

			#define SHADERPASS SHADERPASS_DEPTHNORMALSONLY

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_VERT_NORMAL
			#pragma shader_feature_local _DYNAMIC_ON_ON
			#pragma shader_feature_local _WIND_ON_ON
			#pragma shader_feature_local _PIVOTBAKE_ON
			#pragma shader_feature_local _WINDGRADIENT_UV _WINDGRADIENT_VERTEXCOLOR _WINDGRADIENT_POSITIONOS
			#include "Assets/Visual Design Cafe/Nature Renderer/Shader Includes/Nature Renderer.templatex"
			#pragma instancing_options procedural:SetupNatureRenderer


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _BottomColor;
			float4 _WindLineColor;
			float4 _Albedo_ST;
			float4 _TipColor;
			float3 _SpecifyNormal;
			float2 _WindLineSpeed;
			float _WindLineColorGradient;
			float _WindLineRotate;
			float _WindLineScale;
			float _BlendGradient;
			float _ColorGradient;
			float _WindSpeed;
			float _UseTexColor;
			float _FixedNormal;
			float _DynamicMotionIntensity;
			float _WindIntensity;
			float _WindSizeSmall;
			float _WindSizeBig;
			float _WindDirection;
			float _IndirectLightIntensity;
			float _AlphaCutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			TEXTURE2D(_GrassMotionRT);
			float4 _GrassFXCamPos;
			float _GrassFXCamSize;
			SAMPLER(sampler_GrassMotionRT);
			TEXTURE2D(_Albedo);
			SAMPLER(sampler_Albedo);


			float3 RotateXY165_g75974( float3 R, float degrees )
			{
				float3 reflUVW = R;
				half theta = degrees * PI / 180.0f;
				half costha = cos(theta);
				half sintha = sin(theta);
				reflUVW = half3(reflUVW.x * costha - reflUVW.z * sintha, reflUVW.y, reflUVW.x * sintha + reflUVW.z * costha);
				return reflUVW;
			}
			
			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			struct SurfaceDescription
			{
				float Alpha;
				float AlphaClipThreshold;
			};

			VertexOutput VertexFunction(VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float3 ase_worldPos = TransformObjectToWorld( (v.vertex).xyz );
				float3 _Vector0 = float3(0,0,1);
				float3 RotateAxis34_g75974 = cross( _Vector0 , float3(0,1,0) );
				float3 wind_direction31_g75974 = _Vector0;
				float3 wind_speed40_g75974 = ( ( _TimeParameters.x * _WindSpeed ) * float3(0.5,-0.5,-0.5) );
				float2 texCoord403 = v.ase_texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
				float2 texCoord406 = v.ase_texcoord3.xy * float2( 1,1 ) + float2( 0,0 );
				float3 appendResult405 = (float3(-texCoord403.x , texCoord406.x , -texCoord403.y));
				float3 MehsPivotOS473 = appendResult405;
				float3 objToWorld440 = mul( GetObjectToWorldMatrix(), float4( MehsPivotOS473, 1 ) ).xyz;
				float3 vertexToFrag511 = objToWorld440;
				#ifdef _PIVOTBAKE_ON
				float3 staticSwitch409 = vertexToFrag511;
				#else
				float3 staticSwitch409 = ase_worldPos;
				#endif
				float3 VitualPositionWS427 = staticSwitch409;
				float3 WorldPosition161_g75974 = VitualPositionWS427;
				float3 R165_g75974 = WorldPosition161_g75974;
				float degrees165_g75974 = _WindDirection;
				float3 localRotateXY165_g75974 = RotateXY165_g75974( R165_g75974 , degrees165_g75974 );
				float3 temp_cast_0 = (1.0).xxx;
				float3 temp_output_22_0_g75974 = abs( ( ( frac( ( ( ( wind_direction31_g75974 * wind_speed40_g75974 ) + ( localRotateXY165_g75974 / _WindSizeBig ) ) + 0.5 ) ) * 2.0 ) - temp_cast_0 ) );
				float3 temp_cast_1 = (3.0).xxx;
				float dotResult30_g75974 = dot( ( ( temp_output_22_0_g75974 * temp_output_22_0_g75974 ) * ( temp_cast_1 - ( temp_output_22_0_g75974 * 2.0 ) ) ) , wind_direction31_g75974 );
				float BigTriangleWave42_g75974 = dotResult30_g75974;
				float3 temp_cast_2 = (1.0).xxx;
				float3 temp_output_59_0_g75974 = abs( ( ( frac( ( ( wind_speed40_g75974 + ( localRotateXY165_g75974 / _WindSizeSmall ) ) + 0.5 ) ) * 2.0 ) - temp_cast_2 ) );
				float3 temp_cast_3 = (3.0).xxx;
				float SmallTriangleWave52_g75974 = distance( ( ( temp_output_59_0_g75974 * temp_output_59_0_g75974 ) * ( temp_cast_3 - ( temp_output_59_0_g75974 * 2.0 ) ) ) , float3(0,0,0) );
				float3 rotatedValue72_g75974 = RotateAroundAxis( ( float3( 0,0,0 ) - float3(0,0.1,0) ), WorldPosition161_g75974, normalize( RotateAxis34_g75974 ), ( ( BigTriangleWave42_g75974 + SmallTriangleWave52_g75974 ) * ( 2.0 * PI ) ) );
				float2 texCoord313 = v.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				#if defined(_WINDGRADIENT_UV)
				float staticSwitch315 = texCoord313.y;
				#elif defined(_WINDGRADIENT_VERTEXCOLOR)
				float staticSwitch315 = v.ase_color.g;
				#elif defined(_WINDGRADIENT_POSITIONOS)
				float staticSwitch315 = v.vertex.xyz.y;
				#else
				float staticSwitch315 = texCoord313.y;
				#endif
				float WindGradient316 = staticSwitch315;
				float3 worldToObj197 = mul( GetWorldToObjectMatrix(), float4( ( ase_worldPos + ( ( ( rotatedValue72_g75974 * float3(1,0,1) ) - WorldPosition161_g75974 ) * ( WindGradient316 * WindGradient316 ) * _WindIntensity * 0.1 ) ), 1 ) ).xyz;
				#ifdef _WIND_ON_ON
				float3 staticSwitch515 = worldToObj197;
				#else
				float3 staticSwitch515 = v.vertex.xyz;
				#endif
				float3 WindVertexPosition198 = staticSwitch515;
				half3 VertexPos40_g75972 = ( WindVertexPosition198 - MehsPivotOS473 );
				float3 appendResult74_g75972 = (float3(VertexPos40_g75972.x , 0.0 , 0.0));
				half3 VertexPosRotationAxis50_g75972 = appendResult74_g75972;
				float3 break84_g75972 = VertexPos40_g75972;
				float3 appendResult81_g75972 = (float3(0.0 , break84_g75972.y , break84_g75972.z));
				half3 VertexPosOtherAxis82_g75972 = appendResult81_g75972;
				float4 tex2DNode421 = SAMPLE_TEXTURE2D_LOD( _GrassMotionRT, sampler_GrassMotionRT, ( ( ( (VitualPositionWS427).xz - (_GrassFXCamPos).xz ) / ( _GrassFXCamSize * 2.0 ) ) + float2( 0.5,0.5 ) ), 0.0 );
				float2 break425 = ((tex2DNode421).rg*2.0 + -1.0);
				float3 appendResult424 = (float3(break425.x , 0.0 , break425.y));
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				float2 DirectionOS476 = (( mul( GetWorldToObjectMatrix(), float4( appendResult424 , 0.0 ) ).xyz * ase_objectScale )).xz;
				float MotionPower434 = tex2DNode421.b;
				float2 temp_output_478_0 = ( DirectionOS476 * MotionPower434 * _DynamicMotionIntensity * WindGradient316 );
				half Angle44_g75972 = (temp_output_478_0).y;
				half3 VertexPos40_g75973 = ( VertexPosRotationAxis50_g75972 + ( VertexPosOtherAxis82_g75972 * cos( Angle44_g75972 ) ) + ( cross( float3(1,0,0) , VertexPosOtherAxis82_g75972 ) * sin( Angle44_g75972 ) ) );
				float3 appendResult74_g75973 = (float3(0.0 , 0.0 , VertexPos40_g75973.z));
				half3 VertexPosRotationAxis50_g75973 = appendResult74_g75973;
				float3 break84_g75973 = VertexPos40_g75973;
				float3 appendResult81_g75973 = (float3(break84_g75973.x , break84_g75973.y , 0.0));
				half3 VertexPosOtherAxis82_g75973 = appendResult81_g75973;
				half Angle44_g75973 = -(temp_output_478_0).x;
				#ifdef _DYNAMIC_ON_ON
				float3 staticSwitch512 = ( ( VertexPosRotationAxis50_g75973 + ( VertexPosOtherAxis82_g75973 * cos( Angle44_g75973 ) ) + ( cross( float3(0,0,1) , VertexPosOtherAxis82_g75973 ) * sin( Angle44_g75973 ) ) ) + MehsPivotOS473 );
				#else
				float3 staticSwitch512 = WindVertexPosition198;
				#endif
				float3 FinalVertexPosition485 = staticSwitch512;
				
				float3 lerpResult331 = lerp( v.ase_normal , _SpecifyNormal , _FixedNormal);
				float3 VertexNormal387 = lerpResult331;
				
				o.ase_texcoord1.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord1.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = FinalVertexPosition485;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = VertexNormal387;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 normalWS = TransformObjectToWorldNormal(v.ase_normal);

				o.clipPos = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  normalWS;

				return o;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_texcoord2 = v.ase_texcoord2;
				o.ase_texcoord3 = v.ase_texcoord3;
				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.ase_color;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				o.ase_texcoord3 = patch[0].ase_texcoord3 * bary.x + patch[1].ase_texcoord3 * bary.y + patch[2].ase_texcoord3 * bary.z;
				o.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				o.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;

				float2 uv_Albedo = IN.ase_texcoord1.xy * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 tex2DNode205 = SAMPLE_TEXTURE2D( _Albedo, sampler_Albedo, uv_Albedo );
				float OpacityMask96 = tex2DNode205.a;
				

				surfaceDescription.Alpha = OpacityMask96;
				surfaceDescription.AlphaClipThreshold = _AlphaCutoff;

				#if _ALPHATEST_ON
					clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				float3 normalWS = IN.normalWS;

				return half4(NormalizeNormalPerPixel(normalWS), 0.0);
			}

			ENDHLSL
		}
		
	}
	
	CustomEditor "ASEMaterialInspector"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback "Hidden/InternalErrorShader"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.CommentaryNode;433;-4258.199,1605.891;Inherit;False;1780.648;427.7818;MeshPivot;11;440;427;409;338;406;405;403;473;509;510;511;MeshPivot;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;432;-4754.08,2196.913;Inherit;False;3148.505;629.8662;DynamicMotion;23;476;494;434;425;423;505;412;492;493;491;490;424;422;421;419;420;418;417;416;415;414;413;410;DynamicMotion;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;395;-1376.386,-1807.051;Inherit;False;1448.626;806.2704;Combine Color;11;393;118;175;389;392;390;157;293;173;391;177;Combine Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;42;-4267.434,439.5075;Inherit;False;2897.436;1064.95;;15;429;198;197;196;192;428;358;199;191;223;194;214;318;515;516;Wind;0.49,0.6290355,0.7,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;317;-3132.753,-2423.459;Inherit;False;816.999;564.3252;Wind Gradient;5;402;315;316;314;313;Wind Gradient;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;313;-3082.753,-2373.459;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;314;-3055.635,-2234.134;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;44;-4020.783,-1801.677;Inherit;False;2593.232;1193.395;BaseColor;24;373;374;375;372;324;209;207;208;121;305;325;145;380;211;206;319;96;321;322;205;377;382;384;385;BaseColor;0.5177868,0.7,0.49,1;0;0
Node;AmplifyShaderEditor.SamplerNode;205;-3937.189,-1675.272;Inherit;True;Property;_Albedo;Albedo;2;0;Create;True;0;0;0;False;0;False;-1;None;3ae725f497f157a47afd25a92165c2c1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;43;-4080.384,-2423.995;Inherit;False;912.9;564.4999;Color Gradient;5;202;201;204;200;379;Color Gradient;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;378;-5781.125,-1793.11;Inherit;False;1699.755;869.0917;Terrain  Color;13;351;370;368;369;360;366;367;365;363;364;359;361;362;Terrain  Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;217;-2211.203,-2271.141;Inherit;False;531;387;GPU Instance Indirect;1;27;GPU Instance Indirect;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;335;-5437.04,436.0918;Inherit;False;1134.637;806.608;WorldNormal;7;333;331;327;330;332;326;387;WorldNormal;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;286;-4013.078,-504.0045;Inherit;False;2175.28;847.7356;WindLine;19;398;274;386;396;399;397;268;279;285;267;276;269;283;270;291;271;278;292;517;WindLine;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;200;-4050.831,-2357.141;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;204;-4023.713,-2217.816;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PiNode;292;-3803.381,-73.59741;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;362;-5527.16,-1695.118;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode;361;-5724.583,-1743.11;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector4Node;359;-5731.125,-1544.597;Inherit;False;Global;_OrthographicCamPos;_OrthographicCamPos;31;0;Create;True;0;0;0;False;0;False;0,0,0,0;62.7,39,63,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;364;-5293.743,-1607.859;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;363;-5468.261,-1546.778;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;278;-3342.232,-423.519;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;271;-3781.079,-429.4637;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;291;-3549.381,-189.5974;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;270;-3963.078,-424.4637;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PannerNode;283;-2987.639,-420.6539;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;0.1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;269;-3527.077,-423.0635;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;34;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;SceneSelectionPass;0;6;SceneSelectionPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=SceneSelectionPass;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;32;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;30;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=ShadowCaster;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;28;-72,-34;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;0;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;31;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;True;False;False;False;False;0;False;;False;False;False;False;False;False;False;False;False;True;1;False;;False;False;True;1;LightMode=DepthOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;35;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;ScenePickingPass;0;7;ScenePickingPass;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Picking;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;37;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormalsOnly;0;9;DepthNormalsOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;True;9;d3d11;metal;vulkan;xboxone;xboxseries;playstation;ps4;ps5;switch;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;36;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;DepthNormals;0;8;DepthNormals;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;;True;3;False;;False;True;1;LightMode=DepthNormalsOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;33;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraphUnlitGUI;0;13;New Amplify Shader;2992e84f91cbeb14eab234972e07ea9d;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;0;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;4;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;UniversalMaterialType=Unlit;True;5;True;12;all;0;False;True;1;1;False;;0;False;;1;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=Universal2D;False;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;365;-5108.124,-1603.153;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;367;-5530.768,-1193.655;Inherit;False;Constant;_Float13;Float 13;31;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;366;-5282.081,-1263.462;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;360;-5541.676,-1326.725;Inherit;False;Global;_OrthographicCamSize;_OrthographicCamSize;31;0;Create;True;0;0;0;False;0;False;0;64.35;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;369;-5095.041,-1394.643;Inherit;False;Constant;_Vector6;Vector 6;31;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;368;-4878.433,-1600.723;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;370;-4701.012,-1627.902;Inherit;True;Global;_TerrainDiffuse;_TerrainDiffuse;28;0;Create;True;0;0;0;False;0;False;-1;None;;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;322;-3615.091,-1750.229;Inherit;False;Constant;_Float2;Float 2;25;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;321;-3386.091,-1694.229;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;96;-3411.939,-1548.177;Inherit;False;OpacityMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;207;-3881.959,-939.663;Inherit;False;201;VerticalFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;319;-3199.756,-1697.948;Inherit;False;TexColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;372;-2951.671,-1006.035;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;375;-2808.712,-1010.504;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;206;-3341.881,-1182.495;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;208;-3676.041,-934.8198;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;211;-3529.291,-936.3769;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;325;-3300.185,-1334.41;Inherit;False;319;TexColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;121;-1785.804,-1225.694;Inherit;False;BaseColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;351;-4365.686,-1626.184;Inherit;False;TerrainColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;374;-3196.479,-1028.986;Inherit;False;201;VerticalFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;382;-2597.164,-1044.289;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;377;-2810.226,-1113.487;Inherit;False;351;TerrainColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;305;-2954.683,-1248.663;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;373;-3186.587,-944.2634;Inherit;False;Property;_BlendGradient;地形融合控制;10;0;Create;False;0;0;0;False;0;False;1;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;209;-3872.068,-854.941;Inherit;False;Property;_ColorGradient;渐变颜色控制;8;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;380;-3767.418,-1356.331;Inherit;False;Property;_BottomColor;底部颜色;6;0;Create;False;0;0;0;False;0;False;0.4313726,1,0.007843138,0;0.4336353,0.6226414,0.04405403,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;145;-3767.917,-1169.681;Inherit;False;Property;_TipColor;顶部颜色;7;0;Create;False;0;0;0;False;0;False;0.4313726,1,0.007843138,0;0.6691964,0.8490566,0.1722131,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;324;-3599.091,-1609.229;Inherit;False;Property;_UseTexColor;使用纹理颜色;5;1;[Toggle];Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;201;-3458.832,-2208.141;Inherit;False;VerticalFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;267;-2747.786,-450.6007;Inherit;True;Property;_WindLine;风线贴图;20;0;Create;False;0;0;0;False;0;False;-1;None;2b9f81a91733f254ab562ce913cb295b;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;285;-3272.729,-258.3189;Inherit;False;Property;_WindLineSpeed;风线速度;25;0;Create;False;0;0;0;False;0;False;0,0.3;0,0.3;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;279;-3871.53,-193.019;Inherit;False;Property;_WindLineRotate;风线方向;24;0;Create;False;0;0;0;False;0;False;0.8;0.81;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;268;-3792.077,-287.0636;Inherit;False;Property;_WindLineScale;风线大小;23;0;Create;False;0;0;0;False;0;False;50;50;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;385;-1965.806,-1225.814;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-269.3699,-482.2037;Inherit;False;Property;_AlphaCutoff;Alpha Cutoff;3;0;Create;False;0;0;0;False;0;False;0.35;0.35;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;392;-1027.432,-1445.286;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;177;-1113.679,-1345.366;Inherit;False;World;1;0;FLOAT3;0,0,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;391;-1326.386,-1346.942;Inherit;False;333;WorldNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;173;-1051.679,-1247.365;Inherit;False;121;BaseColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LightAttenuation;390;-1281.979,-1561.331;Inherit;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;157;-1261.324,-1736.051;Inherit;False;121;BaseColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;389;-1007.98,-1697.331;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;293;-432.1838,-1689.607;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;175;-762.7804,-1446.905;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;393;-266.569,-1694.327;Inherit;False;CombineColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;118;-1148.48,-1159.565;Inherit;False;Property;_IndirectLightIntensity;环境光强度;11;0;Create;False;0;0;0;False;0;False;1;1;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;384;-2406.664,-1274.669;Inherit;False;Property;_GroundBlending;是否开启地形融合;9;0;Create;False;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;397;-2693.525,127.8821;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;399;-2555.525,129.8821;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;396;-2970.525,84.88208;Inherit;False;201;VerticalFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;386;-2401.625,-87.31744;Inherit;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;274;-2687.511,-219.299;Inherit;False;Property;_WindLineColor;风线颜色;21;0;Create;False;0;0;0;False;0;False;0.07843138,0.2980392,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;398;-3004.525,172.8821;Inherit;False;Property;_WindLineColorGradient;风线颜色范围;22;0;Create;False;0;0;0;False;0;False;1;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;401;-2199.323,-1104.464;Inherit;False;276;WindLine;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.PosVertexDataNode;379;-4019.141,-2031.814;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PosVertexDataNode;402;-3045.097,-2015.62;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;202;-3771.833,-2208.141;Inherit;False;Property;_ColorGradientMode;垂直渐变模式;4;0;Create;False;0;0;0;False;2;Header(Color Settings);Space(5);False;0;0;0;True;;KeywordEnum;3;UV;VertexColor;PositionOS;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;326;-5322.561,675.2295;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TransformDirectionNode;327;-4890.561,841.2294;Inherit;False;Object;World;True;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;331;-5089.561,846.2295;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;332;-5309.561,1078.228;Inherit;False;Property;_FixedNormal;指定法线;26;1;[Toggle];Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;330;-5308.561,887.2295;Inherit;False;Property;_SpecifyNormal;指定法线方向;27;0;Create;False;0;0;0;False;0;False;0,1,0;0,1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SwizzleNode;410;-4428.331,2495.804;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;413;-4133.792,2352.34;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;414;-3932.792,2364.34;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;415;-3930.792,2550.34;Inherit;False;Constant;_Vector10;Vector 10;59;0;Create;True;0;0;0;False;0;False;0.5,0.5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;416;-3693.29,2372.222;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;417;-4332.004,2725.779;Inherit;False;Constant;_Float10;Float 10;59;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;418;-4073.467,2585.314;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;420;-4685.33,2502.804;Inherit;False;Global;_GrassFXCamPos;_GrassFXCamPos;59;0;Create;True;0;0;0;False;0;False;0,0,0,0;62.4,25.6,62.8,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;419;-4384.004,2619.779;Inherit;False;Global;_GrassFXCamSize;_GrassFXCamSize;59;0;Create;True;0;0;0;False;0;False;0;36.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;421;-3504.691,2339.116;Inherit;True;Global;_GrassMotionRT;_GrassMotionRT;29;0;Create;True;0;0;0;False;0;False;-1;None;1b9e6ffea6dae004bae77ebb2d027c4d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SwizzleNode;422;-3157.364,2344.355;Inherit;False;FLOAT2;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;403;-4204.099,1741.847;Inherit;False;2;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;406;-4208.199,1870.946;Inherit;False;3;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;316;-2552.754,-2304.459;Inherit;False;WindGradient;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;29;170.1548,-567.5262;Float;False;True;-1;2;ASEMaterialInspector;0;13;Grass;2992e84f91cbeb14eab234972e07ea9d;True;Forward;0;1;Forward;8;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;True;2;False;;False;False;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;False;False;False;True;5;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=AlphaTest=Queue=0;UniversalMaterialType=Unlit;NatureRendererInstancing=True;True;5;True;12;all;0;False;True;1;1;False;;0;False;;1;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;False;255;False;;255;False;;255;False;;7;False;;1;False;;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=UniversalForwardOnly;False;False;0;Hidden/InternalErrorShader;0;0;Standard;23;Surface;0;0;  Blend;0;0;Two Sided;0;638043145277367111;Forward Only;0;0;Cast Shadows;0;638159204486182825;  Use Shadow Threshold;0;0;Receive Shadows;1;638159204286818129;GPU Instancing;1;0;LOD CrossFade;0;0;Built-in Fog;0;0;DOTS Instancing;0;0;Meta Pass;0;0;Extra Pre Pass;0;0;Tessellation;0;0;  Phong;0;0;  Strength;0.5,False,;0;  Type;0;0;  Tess;16,False,;0;  Min;10,False,;0;  Max;25,False,;0;  Edge Length;16,False,;0;  Max Displacement;25,False,;0;Vertex Position,InvertActionOnDeselection;0;638043171708848109;0;10;False;True;False;True;False;False;True;True;True;True;False;;True;0
Node;AmplifyShaderEditor.TransformPositionNode;440;-3373.838,1793.115;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;424;-2489.364,2330.355;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldToObjectMatrix;490;-2519.422,2242.523;Inherit;False;0;1;FLOAT4x4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;491;-2262.422,2237.523;Inherit;False;2;2;0;FLOAT4x4;0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ObjectScaleNode;493;-2330.422,2397.523;Inherit;False;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;492;-2109.422,2249.523;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;412;-4448.683,2347.777;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;27;-2161.203,-2221.141;Inherit;False;NatureRendererInstancing;-1;;75971;1eb8430ac00cf4b4bab22a09811e170c;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;409;-2960.886,1772.37;Inherit;False;Property;_PIVOTBAKE;使用预烘焙轴点;14;0;Create;False;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldPosInputsNode;338;-3179.192,1659.337;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NegateNode;509;-3987.06,1754.095;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;510;-3985.71,1836.848;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;405;-3823.1,1853.847;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;473;-3637.427,1792.162;Inherit;False;MehsPivotOS;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexToFragmentNode;511;-3167.305,1829.58;Inherit;False;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;60;-187.4033,-570.0414;Inherit;False;96;OpacityMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;394;-194.8819,-694.2847;Inherit;False;393;CombineColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;427;-2705.948,1769.079;Inherit;False;VitualPositionWS;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;505;-4694.642,2341.355;Inherit;False;427;VitualPositionWS;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;423;-2987.364,2344.355;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;2;False;2;FLOAT;-1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BreakToComponentsNode;425;-2787.364,2342.355;Inherit;True;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SwizzleNode;494;-1976.423,2244.523;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;476;-1816.763,2248.577;Inherit;False;DirectionOS;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;467;-814.3018,2319.614;Inherit;False;Compute Rotation X;-1;;75972;ed552fb58173f3f4d9b7e59fc11daba6;0;2;38;FLOAT3;0,0,0;False;43;FLOAT;0;False;1;FLOAT3;19
Node;AmplifyShaderEditor.GetLocalVarNode;487;-1384.703,2802.946;Inherit;False;316;WindGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;477;-1385.82,2473.64;Inherit;False;476;DirectionOS;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;480;-1409.82,2599.64;Inherit;False;434;MotionPower;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;478;-1123.82,2518.64;Inherit;False;4;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;469;-512.3018,2362.614;Inherit;False;Compute Rotation Z;-1;;75973;1170919757c82514daf4c94c781060d9;0;2;38;FLOAT3;0,0,0;False;43;FLOAT;0;False;1;FLOAT3;19
Node;AmplifyShaderEditor.SwizzleNode;481;-942.8203,2440.64;Inherit;False;FLOAT;1;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;482;-682.8203,2507.64;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;333;-4630.562,840.2294;Inherit;False;WorldNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;387;-4883.208,722.2115;Inherit;False;VertexNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;484;-466.8203,2524.64;Inherit;False;473;MehsPivotOS;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;483;-217.8203,2422.64;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;485;259.1797,2383.64;Inherit;False;FinalVertexPosition;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;513;-300.1796,2273.094;Inherit;False;198;WindVertexPosition;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;512;-25.17957,2390.094;Inherit;False;Property;_DYNAMIC_ON;是否开启交互;0;0;Create;False;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;486;-192.9855,-367.9806;Inherit;False;485;FinalVertexPosition;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;514;-165.4163,-223.415;Inherit;False;387;VertexNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;315;-2817.755,-2292.459;Inherit;False;Property;_WindGradient;WindGradient;13;0;Create;True;0;0;0;False;0;False;0;0;2;True;;KeywordEnum;3;UV;VertexColor;PositionOS;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;517;-2254.81,-227.6808;Inherit;False;Property;_Keyword0;Keyword 0;12;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;515;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.Vector3Node;518;-2383.81,-320.6808;Inherit;False;Constant;_Vector7;Vector 7;30;0;Create;True;0;0;0;False;0;False;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;276;-2101.805,-99.7228;Inherit;False;WindLine;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;437;-1353.241,2688.375;Inherit;False;Property;_DynamicMotionIntensity;交互强度;1;0;Create;False;0;0;0;False;0;False;1;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;474;-1336.82,2364.64;Inherit;False;473;MehsPivotOS;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;471;-1101.121,2302.822;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;470;-1341.669,2261.199;Inherit;False;198;WindVertexPosition;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;196;-2453.765,839.1451;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TransformPositionNode;197;-2281.053,832.5652;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.FunctionNode;358;-3183.321,834.6025;Inherit;False;GrassWind;-1;;75974;daa13501c3c3a6a4ba9f9777c1d8f165;0;7;158;FLOAT;1;False;160;FLOAT;1;False;1;FLOAT;1;False;167;FLOAT;0;False;156;FLOAT;10;False;157;FLOAT;2;False;147;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;194;-3594.32,954.1306;Inherit;False;Property;_WindSizeBig;Wind Size Big;18;0;Create;True;0;0;0;False;0;False;10;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;223;-3733.555,873.3425;Inherit;False;Property;_WindDirection;Wind Direction;17;0;Create;True;0;0;0;False;0;False;45;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;191;-3595.32,805.1324;Inherit;False;Property;_WindSpeed;Wind Speed;16;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;199;-3585.937,712.8342;Inherit;False;Property;_WindIntensity;WindIntensity;15;0;Create;True;0;0;0;False;0;False;0.2;0.01;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;428;-3578.765,1144.959;Inherit;False;427;VitualPositionWS;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;192;-3584.32,1036.133;Inherit;False;Property;_WindSizeSmall;Wind Size Small;19;0;Create;True;0;0;0;False;0;False;4;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;318;-4187.979,824.0375;Inherit;False;316;WindGradient;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;214;-3928.322,823.5665;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;429;-2812.759,635.3016;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PosVertexDataNode;516;-2236.878,659.4946;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;198;-1761.953,690.5532;Inherit;False;WindVertexPosition;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StaticSwitch;515;-1977.878,685.4946;Inherit;False;Property;_WIND_ON;是否开启风力;12;0;Create;False;0;0;0;False;2;Header(Wind Settings);Space(5);False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;434;-2950.2,2658.25;Inherit;False;MotionPower;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
WireConnection;362;0;361;0
WireConnection;364;0;362;0
WireConnection;364;1;363;0
WireConnection;363;0;359;0
WireConnection;278;0;269;0
WireConnection;278;2;291;0
WireConnection;271;0;270;0
WireConnection;291;0;279;0
WireConnection;291;1;292;0
WireConnection;283;0;278;0
WireConnection;283;2;285;0
WireConnection;269;0;271;0
WireConnection;269;1;268;0
WireConnection;365;0;364;0
WireConnection;365;1;366;0
WireConnection;366;0;360;0
WireConnection;366;1;367;0
WireConnection;368;0;365;0
WireConnection;368;1;369;0
WireConnection;370;1;368;0
WireConnection;321;0;322;0
WireConnection;321;1;205;0
WireConnection;321;2;324;0
WireConnection;96;0;205;4
WireConnection;319;0;321;0
WireConnection;372;0;374;0
WireConnection;372;1;373;0
WireConnection;375;0;372;0
WireConnection;206;0;380;0
WireConnection;206;1;145;0
WireConnection;206;2;211;0
WireConnection;208;0;207;0
WireConnection;208;1;209;0
WireConnection;211;0;208;0
WireConnection;121;0;385;0
WireConnection;351;0;370;0
WireConnection;382;0;377;0
WireConnection;382;1;305;0
WireConnection;382;2;375;0
WireConnection;305;0;325;0
WireConnection;305;1;206;0
WireConnection;201;0;202;0
WireConnection;267;1;283;0
WireConnection;385;0;384;0
WireConnection;385;1;401;0
WireConnection;392;0;390;0
WireConnection;177;0;391;0
WireConnection;389;0;157;0
WireConnection;389;1;390;0
WireConnection;293;0;389;0
WireConnection;293;1;175;0
WireConnection;175;0;392;0
WireConnection;175;1;177;0
WireConnection;175;2;173;0
WireConnection;175;3;118;0
WireConnection;393;0;293;0
WireConnection;384;1;305;0
WireConnection;384;0;382;0
WireConnection;397;0;396;0
WireConnection;397;1;398;0
WireConnection;399;0;397;0
WireConnection;386;0;267;1
WireConnection;386;1;274;0
WireConnection;386;2;399;0
WireConnection;202;1;200;2
WireConnection;202;0;204;2
WireConnection;202;2;379;2
WireConnection;327;0;331;0
WireConnection;331;0;326;0
WireConnection;331;1;330;0
WireConnection;331;2;332;0
WireConnection;410;0;420;0
WireConnection;413;0;412;0
WireConnection;413;1;410;0
WireConnection;414;0;413;0
WireConnection;414;1;418;0
WireConnection;416;0;414;0
WireConnection;416;1;415;0
WireConnection;418;0;419;0
WireConnection;418;1;417;0
WireConnection;421;1;416;0
WireConnection;422;0;421;0
WireConnection;316;0;315;0
WireConnection;29;2;394;0
WireConnection;29;3;60;0
WireConnection;29;4;56;0
WireConnection;29;5;486;0
WireConnection;29;6;514;0
WireConnection;440;0;473;0
WireConnection;424;0;425;0
WireConnection;424;2;425;1
WireConnection;491;0;490;0
WireConnection;491;1;424;0
WireConnection;492;0;491;0
WireConnection;492;1;493;0
WireConnection;412;0;505;0
WireConnection;409;1;338;0
WireConnection;409;0;511;0
WireConnection;509;0;403;1
WireConnection;510;0;403;2
WireConnection;405;0;509;0
WireConnection;405;1;406;1
WireConnection;405;2;510;0
WireConnection;473;0;405;0
WireConnection;511;0;440;0
WireConnection;427;0;409;0
WireConnection;423;0;422;0
WireConnection;425;0;423;0
WireConnection;494;0;492;0
WireConnection;476;0;494;0
WireConnection;467;38;471;0
WireConnection;467;43;481;0
WireConnection;478;0;477;0
WireConnection;478;1;480;0
WireConnection;478;2;437;0
WireConnection;478;3;487;0
WireConnection;469;38;467;19
WireConnection;469;43;482;0
WireConnection;481;0;478;0
WireConnection;482;0;478;0
WireConnection;333;0;327;0
WireConnection;387;0;331;0
WireConnection;483;0;469;19
WireConnection;483;1;484;0
WireConnection;485;0;512;0
WireConnection;512;1;513;0
WireConnection;512;0;483;0
WireConnection;315;1;313;2
WireConnection;315;0;314;2
WireConnection;315;2;402;2
WireConnection;517;1;518;0
WireConnection;517;0;386;0
WireConnection;276;0;517;0
WireConnection;471;0;470;0
WireConnection;471;1;474;0
WireConnection;196;0;429;0
WireConnection;196;1;358;0
WireConnection;197;0;196;0
WireConnection;358;158;214;0
WireConnection;358;160;199;0
WireConnection;358;1;191;0
WireConnection;358;167;223;0
WireConnection;358;156;194;0
WireConnection;358;157;192;0
WireConnection;358;147;428;0
WireConnection;214;0;318;0
WireConnection;214;1;318;0
WireConnection;198;0;515;0
WireConnection;515;1;516;0
WireConnection;515;0;197;0
WireConnection;434;0;421;3
ASEEND*/
//CHKSM=14989547419A5A62D61318AE1547FF043F817BCA