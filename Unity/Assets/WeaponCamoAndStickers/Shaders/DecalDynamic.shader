Shader "WeaponCamoAndStickers/DeferredDecal" {
    Properties {
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
            Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZClip False
            ZTest Always
            ZWrite Off

            // _StencilType:
            // 1: equimpent (clothes/helmet/armor/rig/backpack)
            // 2: hands/weapon (but we patch hands to be 1)
            // 0: everything else

            Stencil {
                Ref 2
                ReadMask 3
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 texcoord2 : TEXCOORD2;
                float3 texcoord3 : TEXCOORD3;
                float2 texcoord : TEXCOORD;
                float3 texcoord1 : TEXCOORD1;
                float3 texcoord6 : TEXCOORD6;
                float3 texcoord4 : TEXCOORD4;
            };

            struct fout
            {
                float4 sv_target : SV_Target;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 clipPos = mul(unity_MatrixVP, worldPos);

                o.position = clipPos;
                o.texcoord2.zw = clipPos.zw;

                clipPos.y *= _ProjectionParams.x;
                o.texcoord2.xy = (clipPos.xy + clipPos.ww) * 0.5;

                float3 viewPos = mul(unity_MatrixV, worldPos).xyz;
                o.texcoord3 = viewPos * float3(-1.0, -1.0, 1.0);
                o.texcoord = v.vertex.xz + 0.5;

                o.texcoord1 = unity_ObjectToWorld._m01_m11_m21;
                o.texcoord6 = unity_ObjectToWorld._m01_m11_m21;
                o.texcoord4 = unity_ObjectToWorld._m00_m10_m20;

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

            fout frag(v2f inp)
            {
                fout o;
                float4 tmp0;
                float4 tmp1;
                float4 tmp2;

                tmp1.xy = inp.texcoord2.xy / inp.texcoord2.ww;
                tmp2 = tex2D(_CameraDepthTexture, tmp1.xy);
                tmp0.xyz = (_ProjectionParams.z / inp.texcoord3.z) * inp.texcoord3.xyz;
                tmp0.xyz *= 1 / (_ZBufferParams.x * tmp2.x + _ZBufferParams.y);
                tmp0.xyz = mul(unity_CameraToWorld, float4(tmp0.xyz, 1));
                tmp0.xyz = mul(unity_WorldToObject, float4(tmp0.xyz, 1));

                if (any(abs(tmp0.xyz) > 0.5)) {
                    discard;
                }

                tmp1 = tex2D(_NormalsCopy, tmp1.xy);
                tmp1.xyz = tmp1.xyz * 2 - 1;
                tmp2.xyz = rsqrt(dot(inp.texcoord1.xyz, inp.texcoord1.xyz)) * inp.texcoord1.xyz;

                if (dot(tmp1.xyz, tmp2.xyz) < _MaxAngle) {
                    discard;
                }

                tmp0.xy = tmp0.xz + 0.5;

                float2 mainUV = (tmp0.xy + _MainTexUV.xy) * _MainTexUV.zw;
                float2 maskUV = (tmp0.xy + _MaskTexUV.xy) * _MaskTexUV.zw;

                tmp0 = tex2D(_MainTex, mainUV) * tex2D(_MaskTex, maskUV) * _Color;
                tmp1.x = _ThermalVisionOn > 0;
                tmp1.yzw = tmp0.xyz * _Temperature.zzz;
                tmp1.yzw = max(tmp1.yzw, _Temperature.xxx);
                tmp1.yzw = min(tmp1.yzw, _Temperature.yyy);
                tmp1.yzw = tmp1.yzw + _Temperature.www;
                o.sv_target.xyz = tmp1.xxx ? tmp1.yzw : tmp0.xyz;
                o.sv_target.w = tmp0.w;

                return o;
            }
            ENDCG

        }
    }
}
