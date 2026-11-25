Shader "SyntyStudios/Prototype_Global" {
    Properties {
        _BaseColor ("BaseColor", Color) = (0.06228374,0.8320726,0.9411765,0)
        _Grid ("Grid", 2D) = "white" {}
        _GridScale ("GridScale", Float) = 5
        _Falloff ("Falloff", Float) = 50
        _OverlayAmount ("OverlayAmount", Range(0, 1)) = 1
        [HideInInspector] __dirty ("", Float) = 1
    }
    //DummyShaderTextExporter
    // Reconstructed from the shipped GLES3 LIGHTPROBE_SH forward program.
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Tags { "LightMode"="ForwardBase" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _Grid;
            float4 _BaseColor;
            float4 _OriginalWorldNormalization;
            float _GridScale, _Falloff, _OverlayAmount;
            struct Output {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                SHADOW_COORDS(2)
            };
            Output vert(appdata_base v) {
                Output o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);
                return o;
            }
            float4 frag(Output i) : SV_Target {
                float3 n = normalize(i.normal);
                float scale = _OriginalWorldNormalization.w > 0 ? _OriginalWorldNormalization.w : 1;
                float3 originalWorld = (i.world - _OriginalWorldNormalization.xyz) / scale;
                float3 weight = pow(abs(n), _Falloff);
                weight /= max(dot(weight, float3(1,1,1)), 1e-8);
                float3 orientation = sign(n);
                float3 grid = tex2D(_Grid, originalWorld.zy * float2(orientation.x,1) * _GridScale).rgb * weight.x;
                grid += tex2D(_Grid, originalWorld.xz * float2(orientation.y,1) * _GridScale).rgb * weight.y;
                grid += tex2D(_Grid, originalWorld.xy * float2(-orientation.z,1) * _GridScale).rgb * weight.z;
                float3 albedo = _BaseColor.rgb * lerp(float3(1,1,1), grid, _OverlayAmount);
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                float3 light = normalize(_WorldSpaceLightPos0.xyz);
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.world);
                float nl = saturate(dot(n, light));
                float reflectedLight = dot(reflect(view, n), light);
                float lookupX = reflectedLight * reflectedLight;
                lookupX *= lookupX;
                float specular = tex2D(unity_NHxRoughness, float2(lookupX,1)).r * 0.64;
                float3 diffuse = albedo * 0.96;
                half4 encodedReflection = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflect(-view,n), 6);
                float3 reflection = DecodeHDR(encodedReflection, unity_SpecCube0_HDR);
                float fresnel = 1 - saturate(dot(n,view));
                fresnel *= fresnel;
                fresnel *= fresnel;
                float3 indirect = max(ShadeSH9(float4(n,1)),0) * diffuse;
                indirect += reflection * (0.04 + fresnel * 2.23517418e-8);
                return float4((diffuse + specular) * _LightColor0.rgb * nl * attenuation + indirect, 1);
            }
            ENDHLSL
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback "Diffuse"
    //CustomEditor "ASEMaterialInspector"
}
