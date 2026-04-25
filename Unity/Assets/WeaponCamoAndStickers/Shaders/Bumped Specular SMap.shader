Shader "WeaponCamoAndStickers/p0/Reflective/Bumped Specular SMap" {
	Properties {
		_StencilRef ("Stencil Ref", Float) = 2
		_Color ("Main Color", Color) = (1,1,1,1)
		_BaseTintColor ("Tint Color", Color) = (1,1,1,1)
		_SpecMap ("GlossMap", 2D) = "white" {}
		_SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Glossness ("Specularness", Range(0.01, 10)) = 1
		_Specularness ("Glossness", Range(0.01, 10)) = 0.078125
		_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
		_MainTex ("Base (RGB) Specular (A)", 2D) = "white" {}
		[Toggle(TINTMASK)] _HasTint ("Has tint", Float) = 0
		_TintMask ("Tint mask", 2D) = "black" {}
		_Cube ("Reflection Cubemap", Cube) = "" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
		_SpecVals ("Specular Vals", Vector) = (1.1,2,0,0)
		_DefVals ("Defuse Vals", Vector) = (0.5,0.7,0,0)
		_BumpTiling ("_BumpTiling", Float) = 1
		_NormalIntensity ("Normal intensity", Float) = 1
		_NormalUVMultiplier ("Normal UV tiling", Float) = 1
		_Factor ("Z Offset Angle", Float) = 0
		_Units ("Z Offset Forward", Float) = 0
		_DropsSpec ("Drops spec", Float) = 128
		_Temperature ("_Temperature", Vector) = (0.1,0.2,0.28,0)
		[Space(30)] [Header(Wetting)] _RippleTexScale ("_RippleTexScale", Float) = 4
		_RippleFakeLightIntensityOffset ("Ripple fake light offset", Float) = 0.7
		_NightRippleFakeLightOffset ("Night fake light offset", Float) = 0.2
		_NdotLOffset ("Normal dot light offset", Float) = 0.4
		[Toggle(USERAIN)] _USERAIN ("Is material affected by rain", Float) = 0
		[HideInInspector] _SkinnedMeshMaterial ("Skinned Mesh Material", Float) = 0
		[Toggle(USEHEAT)] USEHEAT ("Use metal heat glow", Float) = 0
		_HeatVisible ("_HeatVisible([0-1] for thermalVision only)", Float) = 1
		[HDR] _HeatColor1 ("_HeatColor1", Color) = (1,0,0,1)
		[HDR] _HeatColor2 ("_HeatColor2", Color) = (1,0.34,0,1)
		_HeatCenter ("_HeatCenter", Vector) = (0,0,0,1)
		_HeatSize ("_HeatSize", Vector) = (0.02,0.04,0.02,1)
		_HeatTemp ("_HeatTemp", Float) = 0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Pass {
			Name "DEFERRED"
			Tags { "LIGHTMODE" = "DEFERRED" "RenderType" = "Opaque" }
			Stencil {
				Ref [_StencilRef]
				WriteMask 63
				Comp Always
				Pass Replace
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			struct v2f
			{
				float4 position : SV_POSITION0;
				float2 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float3 viewDirWS : TEXCOORD2;
			};

			float4 _MainTex_ST;
			float4 _SpecColor;
			float4 _Color;
			float4 _ReflectColor;
			float _Specularness;
			float _Glossness;
			float _NormalIntensity;
			float _NormalUVMultiplier;
			float3 _SpecVals;
			float3 _DefVals;
			float _BumpTiling;
			float3 _Temperature;
			float _ThermalVisionOn;
			float _HeatThermalFactor;
			sampler2D _MainTex;
			sampler2D _SpecMap;
			sampler2D _BumpMap;
			samplerCUBE _Cube;

			v2f vert(appdata_full v)
			{
                v2f o;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);

                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = _WorldSpaceCameraPos - worldPos;

                return o;
 			}

			struct fout
			{
				float4 sv_target : SV_Target0;
				float4 sv_target1 : SV_Target1;
				float4 sv_target2 : SV_Target2;
				float4 sv_target3 : SV_Target3;
			};

			fout frag(v2f i)
			{
                fout o;

                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                float4 albedoTex = tex2D(_MainTex, i.uv);
                float3 albedo = albedoTex.rgb * _Color.rgb;

                float specMask = tex2D(_SpecMap, i.uv).r * _Specularness;
                float gloss = albedoTex.a * _Glossness;
                float fresnel = 1.0 - saturate(dot(N, V));
                fresnel = fresnel * fresnel * 0.5;
                float specFactor = (_SpecVals.y * fresnel + _SpecVals.x) * 0.5;
                float3 specular = _SpecColor.rgb * (specMask * gloss * specFactor);

                o.sv_target = float4(albedo, 1.0);
                o.sv_target1 = float4(specular, specMask);
                o.sv_target2 = float4(N * 0.5 + 0.5, 1.0);
                o.sv_target3 = float4(0, 0, 0, 1);

                return o;
			}
			ENDCG
		}
	}
}
