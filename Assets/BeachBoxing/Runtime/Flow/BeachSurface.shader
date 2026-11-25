Shader "BeachBoxing/BeachSurface"
{
    Properties
    {
        _MainTex ("Original sand texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            struct Output
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Output vert(appdata_base input)
            {
                Output output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                return output;
            }

            fixed4 frag(Output input) : SV_Target
            {
                return tex2D(_MainTex, input.uv) * _Color;
            }
            ENDCG
        }
    }
}
