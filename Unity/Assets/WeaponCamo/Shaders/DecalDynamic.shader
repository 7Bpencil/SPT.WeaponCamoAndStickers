Shader "WeaponCamo/Decal/Deferred DecalShader Diffuse+Normals Dynamic" {
    Properties {
        [MaterialEnum(Static, 0, Characters, 1, Hands, 2)] _StencilType ("Stencil type to draw decals", Float) = 0
        _MainTex ("Diffuse", 2D) = "white" {}
        _Color ("Main color", Color) = (1, 1, 1, 1)
        _Temperature ("_Temperature(min, max, factor)", Vector) = (0.1, 0.13, 0.33, 0)
        _MaxAngle ("Max angle", Range(0, 1)) = 0.8
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
                float3 texcoord5 : TEXCOORD5;
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
                o.texcoord5 = unity_ObjectToWorld._m02_m12_m22;

                return o;
            }

            float4 _Color;
            float4 _UvStartEnd;
            float4 _Temperature;
            float _MaxAngle;
            float _ThermalVisionOn;
            sampler2D _CameraDepthTexture;
            sampler2D _NormalsCopy;
            sampler2D _MainTex;

            fout frag(v2f inp)
            {
                fout o;
                float4 tmp0;
                float4 tmp1;
                float4 tmp2;
                tmp0.x = _ProjectionParams.z / inp.texcoord3.z;
                tmp0.xyz = tmp0.xxx * inp.texcoord3.xyz;
                tmp1.xy = inp.texcoord2.xy / inp.texcoord2.ww;
                tmp2 = tex2D(_CameraDepthTexture, tmp1.xy);
                tmp0.w = _ZBufferParams.x * tmp2.x + _ZBufferParams.y;
                tmp0.w = 1.0 / tmp0.w;
                tmp0.xyz = tmp0.xyz * tmp0.www;
                tmp2.xyz = tmp0.yyy * unity_CameraToWorld._m01_m11_m21;
                tmp0.xyw = unity_CameraToWorld._m00_m10_m20 * tmp0.xxx + tmp2.xyz;
                tmp0.xyz = unity_CameraToWorld._m02_m12_m22 * tmp0.zzz + tmp0.xyw;
                tmp0.xyz = tmp0.xyz + unity_CameraToWorld._m03_m13_m23;
                tmp2.xyz = tmp0.yyy * unity_WorldToObject._m01_m11_m21;
                tmp0.xyw = unity_WorldToObject._m00_m10_m20 * tmp0.xxx + tmp2.xyz;
                tmp0.xyz = unity_WorldToObject._m02_m12_m22 * tmp0.zzz + tmp0.xyw;
                tmp0.xyz = tmp0.xyz + unity_WorldToObject._m03_m13_m23;
                tmp2.xyz = float3(0.5, 0.5, 0.5) - abs(tmp0.xyz);
                tmp2.xyz = tmp2.xyz < float3(0.0, 0.0, 0.0);
                tmp0.y = uint1(tmp2.x) | uint1(tmp2.y);
                tmp0.y = uint1(tmp0.y) | uint1(tmp2.z);
                if (tmp0.y) {
                    discard;
                }
                tmp1 = tex2D(_NormalsCopy, tmp1.xy);
                tmp1.xyz = tmp1.xyz * float3(2.0, 2.0, 2.0) + float3(-1.0, -1.0, -1.0);
                tmp0.y = dot(inp.texcoord1.xyz, inp.texcoord1.xyz);
                tmp0.y = rsqrt(tmp0.y);
                tmp2.xyz = tmp0.yyy * inp.texcoord1.xyz;
                tmp0.y = dot(tmp1.xyz, tmp2.xyz);
                tmp0.y = tmp0.y < _MaxAngle;
                if (tmp0.y) {
                    discard;
                }
                tmp0.xy = tmp0.xz * float2(2.0, 2.0) + float2(1.0, 1.0);
                tmp0.xy = tmp0.xy * float2(0.5, 0.5);
                tmp0.zw = _UvStartEnd.zw - _UvStartEnd.xy;
                tmp0.xy = tmp0.xy * tmp0.zw + _UvStartEnd.xy;
                tmp0 = tex2D(_MainTex, tmp0.xy);
                tmp0 = tmp0 * _Color;
                tmp1.x = _ThermalVisionOn > 0.0;
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
