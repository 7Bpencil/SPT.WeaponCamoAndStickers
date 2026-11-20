// Copyright (c) 2026 7Bpencil
// SPDX-License-Identifier: Apache-2.0

Shader "WeaponCamoAndStickers/DecalMesh" {
    Properties {
        [MaterialEnum(Static, 0, Characters, 1, Hands, 2)] _StencilType ("_StencilType", Float) = 2
        [MaterialEnum(Keep, 0, DecrementWrap, 7)] _StencilPassOperation ("Stencil Pass Operation", Float) = 0
        _MainTex ("Diffuse", 2D) = "white" {}
        _MainTexUV ("Diffuse UV", Vector) = (0, 0, 1, 1)
        _MaskTex ("Mask", 2D) = "white" {}
        _MaskTexUV ("Mask UV", Vector) = (0, 0, 1, 1)
        _Color ("Main color", Color) = (1, 1, 1, 1)
        _Temperature ("_Temperature(min, max, factor)", Vector) = (0.1, 0.13, 0.33, 0)
        _MaxAngle ("Max angle", Range(-0.1, 1.1)) = -0.1
    }
    SubShader {
        Pass {
            Name ""
            Tags { "LIGHTMODE" = "DEFERRED" }
            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off
            Offset -1, -1

            // _StencilType:
            // 1: equipment (clothes/helmet/armor/rig/backpack)
            // 2: hands/weapon (but we patch hands to be 1)
            // 0: everything else

            Stencil {
                Ref [_StencilType]
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 decalPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            struct fout
            {
                float4 sv_target : SV_Target;
            };

            float4x4 _DecalFromObject;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.position = mul(unity_MatrixVP, float4(worldPos, 1));
                o.decalPos = mul(_DecalFromObject, float4(v.vertex.xyz, 1)).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            float4 _Color;
            float4 _MainTexUV;
            float4 _MaskTexUV;
            float4 _Temperature;
            float _MaxAngle;
            float _ThermalVisionOn;

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

                clip(0.5 - abs(i.decalPos));

                float3 axisY = normalize(UnityObjectToWorldNormal(_DecalFromObject[1].xyz));
                float3 normal = normalize(i.normalWS);
                float facing = dot(normal, axisY);
#if ERASE
                clip(facing - -0.1);

                float2 uvY = i.decalPos.xz;
                float2 maskUV = transformUV(uvY, _MaskTexRotation, _MaskTexUV.xy, _MaskTexUV.zw, _AspectRatio);
                float maskAlpha = tex2D(_MaskTex, maskUV).a;

                clip(maskAlpha - _MaxAngle);

                o.sv_target = 0;
#else
                clip(facing - _MaxAngle);

                float2 uvY = i.decalPos.xz;
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
