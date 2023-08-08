Shader "Toon_Skin"
{
	Properties
	{
		_BaseMap("Base Map", 2D) = "white" {}
		_DiffuseRamp("Ramp",2D) = "white"{}
		_TintLayer1("TintLayer1 Color",Color) = (0.5,0.5,0.5,1)
		_TintLayer1_Offset("TintLayer1 Offset",Range(-1,1)) = 0
		_TintLayer2("TintLayer2 Color",Color) = (0.5,0.5,0.5,0)
		_TintLayer2_Offset("TintLayer2 Offset",Range(-1,1)) = 0
		_TintLayer3("TintLayer3 Color",Color) = (0.5,0.5,0.5,0)
		_TintLayer3_Offset("TintLayer3 Offset",Range(-1,1)) = 0
		
		_SpecColor("Spec Color",Color) = (0.5,0.5,0.5,1)
		_SpecIntensity("Spec Intensity",float) = 1
		_SpecShininess("Spec Shininess",float) = 100

		_EnvMap("Env Map", Cube) = "white" {}
		_Roughness("Roughness",Range(0,1)) = 0
		_FresnelMin("Fresnel Min",Range(-1,2)) = 0.5
		_FresnelMax("Fresnel Max",Range(-1,2)) = 1
		_EnvIntensity("Env Intensity",float) = 0.5

		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline Width", float) = 1
	}
	SubShader
	{
		Pass
		{
			Tags { "LightMode" = "UniversalForward" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalDir : TEXCOORD1;
				float3 tangentDir : TEXCOORD2;
				float3 binormalDir : TEXCOORD3;
				float4 posWorld : TEXCOORD4;
				float4 vertexColor : TEXCOORD5;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normalDir = UnityObjectToWorldNormal(v.normal);
				o.tangentDir = normalize(mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0.0)).xyz);
				o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.vertexColor = v.color;
				o.uv = v.texcoord0;
				return o;
			}
			

			sampler2D _BaseMap;
			sampler2D _NormalMap;
			sampler2D _AOMap;

			sampler2D _DiffuseRamp;
			float4 _TintLayer1;
			float _TintLayer1_Offset;
			float4 _TintLayer2;
			float _TintLayer2_Offset;
			float4 _TintLayer3;
			float _TintLayer3_Offset;

			sampler2D _SpecMap;
			float4 _SpecColor;
			float _SpecIntensity;
			float _SpecShininess;

			samplerCUBE _EnvMap;
			float4 _EnvMap_HDR;
			float _Roughness;
			float _FresnelMin;
			float _FresnelMax;
			float _EnvIntensity;

			half4 frag (v2f i) : SV_Target
			{
				//向量
				half3 normalDir = normalize(i.normalDir);
				half3 tangentDir = normalize(i.tangentDir);
				half3 binormalDir = normalize(i.binormalDir);
				half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				half3 viewDir = normalize(_WorldSpaceCameraPos - i.posWorld.xyz);
				//贴图数据
				half3 base_color = tex2D(_BaseMap, i.uv).rgb;
				base_color = pow(base_color,2.2);
				//half ao = tex2D(_AOMap, i.uv).r;
				half ao = 1.0f;

				//漫反射
				half NdotL = dot(normalDir, lightDir);
				half half_lambert = (NdotL + 1.0) * 0.5;
				half diffuse_term = half_lambert * ao;

				half3 final_diffuse = half3(0.0, 0.0, 0.0);
				//第一层上色
				half2 uv_ramp1 = half2(diffuse_term + _TintLayer1_Offset, 0.5);
				half toon_diffuse1 = tex2D(_DiffuseRamp, uv_ramp1).r;
				half3 tint_color1 = lerp(half3(1, 1, 1), _TintLayer1.rgb, toon_diffuse1 * _TintLayer1.a * i.vertexColor.r);
				final_diffuse = base_color * tint_color1;
				//第二层上色
				half2 uv_ramp2 = half2(diffuse_term + _TintLayer2_Offset,1.0 - i.vertexColor.g);
				half toon_diffuse2 = tex2D(_DiffuseRamp, uv_ramp2).g;
				half3 tint_color2 = lerp(half3(1, 1, 1), _TintLayer2.rgb, toon_diffuse2 * _TintLayer2.a);
				final_diffuse = final_diffuse * tint_color2;
				//第三层上色
				half2 uv_ramp3 = half2(diffuse_term + _TintLayer3_Offset, 1.0 - i.vertexColor.b);
				half toon_diffuse3 = tex2D(_DiffuseRamp, uv_ramp3).b;
				half3 tint_color3 = lerp(half3(1, 1, 1), _TintLayer3.rgb, toon_diffuse3 * _TintLayer3.a);
				final_diffuse = final_diffuse * tint_color3;

				//高光反射
				half3 H = normalize(lightDir + viewDir);
				half NdotH = dot(normalDir, H);
				half spec_term = max(0.0001, pow(NdotH, _SpecShininess)) * ao;
				half3 final_spec = spec_term * _SpecColor * _SpecIntensity;
				
				//环境反射/边缘光
				half fresnel = 1.0 - dot(normalDir, viewDir);
				fresnel = smoothstep(_FresnelMin, _FresnelMax, fresnel);
				half3 reflectDir = reflect(-viewDir, normalDir);
				float roughness = lerp(0.0, 0.95, saturate(_Roughness));
				roughness = roughness * (1.7 - 0.7 * roughness);
				float mip_level = roughness * 6.0;
				half4 color_cubemap = texCUBElod(_EnvMap, float4(reflectDir, mip_level));
				half3 env_color = DecodeHDR(color_cubemap, _EnvMap_HDR);
				half3 final_env = env_color * fresnel * _EnvIntensity * toon_diffuse1;

				half3 final_color = final_diffuse + final_spec + final_env;

				return float4(final_color,1.0);
			}
			ENDCG
		}
		Pass
		{
			Cull Front
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _BaseMap;
			float _Outline;
			float _OutlineZbias;
			float4 _OutlineColor;

			struct appdata
			{
				float4 vertex: POSITION;
				float3 normal: NORMAL;
				float2 texcoord0: TEXCOORD0;
				float4 color: COLOR;
			};
			struct v2f
			{
				float4 pos: SV_POSITION;
				float2 uv: TEXCOORD0;
				float4 vertex_color : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				float3 normal_world = UnityObjectToWorldNormal(v.normal);
				float3 pos_view = UnityObjectToViewPos(v.vertex);
				float3 outline_dir = normalize(mul((float3x3)UNITY_MATRIX_V, normal_world));
				pos_view = pos_view + outline_dir * _Outline * 0.001 * v.color.a;
				o.pos = mul(UNITY_MATRIX_P, float4(pos_view,1.0));
				//o.pos = UnityObjectToClipPos(mdlpos);
				o.uv = v.texcoord0.xy;
				o.vertex_color = v.color;
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				float3 basecolor = tex2D(_BaseMap, i.uv.xy).xyz;
				half maxComponent = max(max(basecolor.r, basecolor.g), basecolor.b) - 0.004;
				half3 saturatedColor = step(maxComponent.rrr, basecolor) * basecolor;
				saturatedColor = lerp(basecolor.rgb, saturatedColor, 0.6);
				half3 outlineColor = 0.8 * saturatedColor * basecolor * _OutlineColor.xyz;
				return float4(outlineColor,1.0);
			}
			ENDCG
		}
		Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
		Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
		Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitDepthNormalsPass.hlsl"
            ENDHLSL
        }

	}
}
