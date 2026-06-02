Shader "WeaponCamoAndStickers/DeferredDecal" {
    Properties {
		_StencilPassOperation ("Stencil Pass Operation", Float) = 0 // this has to be float, integer doesn't work
        _MainTex ("Diffuse", 2D) = "white" {}
        _MainTexUV ("Diffuse UV", Vector) = (0, 0, 1, 1)
        _MaskTex ("Mask", 2D) = "white" {}
        _MaskTexUV ("Mask UV", Vector) = (0, 0, 1, 1)
        _Color ("Main color", Color) = (1, 1, 1, 1)
        _Temperature ("_Temperature(min, max, factor)", Vector) = (0.1, 0.13, 0.33, 0)
        _MaxAngle ("Max angle", Range(0, 1)) = 0.5
    }
    SubShader {
        Pass {
            Name ""
            Tags { "LIGHTMODE" = "DEFERRED" }
            Blend SrcAlpha OneMinusSrcAlpha

			// this combination prevents decal disappearing
			// when camera gets inside decal cube and/or
			// really close to weapon mesh and/or
			// is viewed under really acute grazing angles

            Cull Off
            ZClip False
            ZTest Always
            ZWrite Off

            // _StencilType:
            // 1: equipment (clothes/helmet/armor/rig/backpack)
            // 2: hands/weapon (but we patch hands to be 1)
            // 0: everything else

            Stencil {
                Ref 2
                ReadMask 3
                WriteMask 3
                Comp Equal
				Pass [_StencilPassOperation]
            }
            CGPROGRAM

			#include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ ERASE

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float3 worldPositionVS : TEXCOORD1;
                float3 objectAxisY : TEXCOORD2;
            };

            struct fout
            {
                float4 sv_target : SV_Target;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.position = mul(unity_MatrixVP, worldPosition);
                o.screenPosition = ComputeScreenPos(o.position);
                o.worldPositionVS = mul(unity_MatrixV, worldPosition).xyz * float3(-1, -1, 1);
                o.objectAxisY = unity_ObjectToWorld._m01_m11_m21;

                return o;
            }

            float4 _Color;
            float4 _MainTexUV;
            float4 _MaskTexUV;
            float4 _Temperature;
            float _MaxAngle;
            float _ThermalVisionOn;

            sampler2D _CameraDepthTexture;
            sampler2D _NormalsCopy;
            sampler2D _MainTex;
            sampler2D _MaskTex;

            float _AspectRatio;
            float2 _MainTexRotation;
            float2 _MaskTexRotation;

			float2 rotate(float2 vec, float2 rot)
			{
				return float2(
	                rot.x * vec.x - rot.y * vec.y,
	                rot.y * vec.x + rot.x * vec.y
				);
			}

			float2 transformUV(float2 uv, float2 rot, float2 offset, float2 scale, float aspect)
			{
				uv += offset;
				uv.x *= aspect;
				uv = rotate(uv, rot);
				uv.x /= aspect;
				uv *= scale;
				uv += 0.5;
			    return uv;
			}

            fout frag(v2f i)
            {
                fout o;

                float2 screenUV = i.screenPosition.xy / i.screenPosition.ww;
                float3 viewRay = i.worldPositionVS * (_ProjectionParams.z / i.worldPositionVS.z);
                float3 viewPosition = viewRay * Linear01Depth(tex2D(_CameraDepthTexture, screenUV));
				float3 worldPosition = mul(unity_CameraToWorld, float4(viewPosition, 1));
				float3 localPosition = mul(unity_WorldToObject, float4(worldPosition, 1));

				clip(0.5 - abs(localPosition));

                float3 normal = tex2D(_NormalsCopy, screenUV) * 2 - 1;
                float3 objectAxisY = normalize(i.objectAxisY);
#if ERASE
				clip(dot(normal, objectAxisY));

				float2 uvY = localPosition.xz;
                float2 maskUV = transformUV(uvY, _MaskTexRotation, _MaskTexUV.xy, _MaskTexUV.zw, _AspectRatio);
                float maskAlpha = tex2D(_MaskTex, maskUV).a;

				clip(maskAlpha - _MaxAngle);

				o.sv_target = 0;
#else
				clip(dot(normal, objectAxisY) - _MaxAngle);

				float2 uvY = localPosition.xz;
                float2 mainUV = transformUV(uvY, _MainTexRotation, _MainTexUV.xy, _MainTexUV.zw, _AspectRatio);
                float2 maskUV = transformUV(uvY, _MaskTexRotation, _MaskTexUV.xy, _MaskTexUV.zw, _AspectRatio);

                float4 visibleColor = tex2D(_MainTex, mainUV) * tex2D(_MaskTex, maskUV) * _Color;

				if (_ThermalVisionOn > 0)
				{
	                o.sv_target.xyz = clamp(visibleColor.xyz * _Temperature.z, _Temperature.x, _Temperature.y) + _Temperature.w;
				}
				else
				{
	                o.sv_target.xyz = visibleColor.xyz;
				}

                o.sv_target.w = visibleColor.w;
#endif
                return o;
            }
            ENDCG
        }
    }
}
