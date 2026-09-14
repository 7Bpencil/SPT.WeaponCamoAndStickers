Shader "WeaponCamoAndStickers/CombineTextures"
{
    Properties
    {
        _ColorTex ("Color", 2D) = "white" {}
        _ColorTexRotation ("Color Rotation", Vector) = (1, 0, 0, 0)
        _AlphaTex ("Alpha", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Off
            ZClip False
            ZTest Always
            ZWrite Off

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 colorUV : TEXCOORD0;
                float2 alphaUV : TEXCOORD1;
            };

	        sampler2D _ColorTex;
	        sampler2D _AlphaTex;
			float4 _ColorTex_ST;
			float2 _ColorTexRotation;

			float2 rotate(float2 vec, float2 rot)
			{
				return float2(
	                rot.x * vec.x - rot.y * vec.y,
	                rot.y * vec.x + rot.x * vec.y
				);
			}

            v2f vert (appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
				o.colorUV = rotate(TRANSFORM_TEX(v.uv, _ColorTex), _ColorTexRotation);
                o.alphaUV = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_ColorTex, i.colorUV);
                float4 alpha = tex2D(_AlphaTex, i.alphaUV);
                return float4(color.rgb, alpha.a);
			}

            ENDCG
        }
    }
}
