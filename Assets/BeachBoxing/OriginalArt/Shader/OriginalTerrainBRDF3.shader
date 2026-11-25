Shader "BeachBoxing/Original Terrain BRDF3"
{
    Properties
    {
        [HideInInspector] _Control("Control", 2D) = "red" {}
        [HideInInspector] _Splat0("Layer 0", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2", 2D) = "white" {}
        [HideInInspector] _Splat3("Layer 3", 2D) = "white" {}
        [HideInInspector] _Normal0("Normal 0", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2", 2D) = "bump" {}
        [HideInInspector] _Normal3("Normal 3", 2D) = "bump" {}
        [HideInInspector] _Metallic0("Metallic 0", Float) = 0
        [HideInInspector] _Metallic1("Metallic 1", Float) = 0
        [HideInInspector] _Metallic2("Metallic 2", Float) = 0
        [HideInInspector] _Metallic3("Metallic 3", Float) = 0
        [HideInInspector] _Smoothness0("Smoothness 0", Float) = 0
        [HideInInspector] _Smoothness1("Smoothness 1", Float) = 0
        [HideInInspector] _Smoothness2("Smoothness 2", Float) = 0
        [HideInInspector] _Smoothness3("Smoothness 3", Float) = 0
        _SmoothnessScale("Smoothness Scale", Range(0, 1)) = 0
        [HideInInspector] _TerrainHolesTexture("Holes", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry-100" "RenderType"="Opaque" "TerrainCompatible"="True" }
        Pass
        {
        Tags { "LightMode"="ForwardBase" }
        CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_fwdbase
        #pragma multi_compile_local __ _ALPHATEST_ON
        // The shipped WebGL terrain program uses NHxRoughness / BRDF3.
        // Explicit selection keeps Editor D3D11 from substituting BRDF1.
        #define UNITY_BRDF_PBS BRDF3_Unity_PBS
        #define TERRAIN_STANDARD_SHADER
        #define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
        #include "UnityPBSLighting.cginc"
        #include "TerrainSplatmapCommon.cginc"
        half _Metallic0, _Metallic1, _Metallic2, _Metallic3;
        half _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;
        half _SmoothnessScale;
        struct VertexOutput
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 world : TEXCOORD1;
            float3 normal : TEXCOORD2;
        };
        VertexOutput vert(appdata_full vertex)
        {
            VertexOutput output;
            output.pos = UnityObjectToClipPos(vertex.vertex);
            output.uv = vertex.texcoord.xy;
            output.world = mul(unity_ObjectToWorld,vertex.vertex).xyz;
            output.normal = UnityObjectToWorldNormal(vertex.normal);
            return output;
        }
        float4 frag(VertexOutput input) : SV_Target
        {
            #ifdef _ALPHATEST_ON
                ClipHoles(input.uv);
            #endif
            float2 controlUv = (input.uv*(_Control_TexelSize.zw-1)+.5)*_Control_TexelSize.xy;
            float4 control = tex2D(_Control,controlUv);
            float weight = dot(control,float4(1,1,1,1));
            control /= weight+.001;
            float4 diffuse = tex2D(_Splat0,TRANSFORM_TEX(input.uv,_Splat0))*float4(1,1,1,_Smoothness0)*control.r;
            diffuse += tex2D(_Splat1,TRANSFORM_TEX(input.uv,_Splat1))*float4(1,1,1,_Smoothness1)*control.g;
            diffuse += tex2D(_Splat2,TRANSFORM_TEX(input.uv,_Splat2))*float4(1,1,1,_Smoothness2)*control.b;
            diffuse += tex2D(_Splat3,TRANSFORM_TEX(input.uv,_Splat3))*float4(1,1,1,_Smoothness3)*control.a;
            float metallic = dot(control,float4(_Metallic0,_Metallic1,_Metallic2,_Metallic3));
            // Guard against WebGL/Mobile ASTC formats that report an opaque alpha channel
            // and cause Unity Terrain to force _SmoothnessN=1 (turning matte sand into a mirror).
            diffuse.a *= _SmoothnessScale;
            float roughness = 1-diffuse.a;
            float mip = roughness*(1.7-.7*roughness)*6;
            float3 n = normalize(input.normal);
            float3 view = normalize(_WorldSpaceCameraPos-input.world);
            float3 specularColor = lerp(float3(.04,.04,.04),diffuse.rgb,metallic);
            float oneMinusReflectivity = .96*(1-metallic);
            float3 albedo = diffuse.rgb*oneMinusReflectivity;
            float reflectedLight = dot(reflect(view,n),_WorldSpaceLightPos0.xyz);
            float lookup = reflectedLight*reflectedLight;
            lookup *= lookup;
            float specular = tex2D(unity_NHxRoughness,float2(lookup,roughness)).r*16;
            float3 direct = (albedo+specularColor*specular)*_LightColor0.rgb*saturate(dot(n,_WorldSpaceLightPos0.xyz));
            float3 reflection = DecodeHDR(UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,reflect(-view,n),mip),unity_SpecCube0_HDR);
            float fresnel = 1-saturate(dot(n,view));
            fresnel *= fresnel; fresnel *= fresnel;
            float grazing = saturate(diffuse.a+1-oneMinusReflectivity);
            float3 indirect = max(ShadeSH9(float4(input.normal,1)),0)*albedo;
            indirect += reflection*lerp(specularColor,float3(grazing,grazing,grazing),fresnel);
            return float4((direct+indirect)*weight,1);
        }
        ENDCG
        }
    }
    Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Standard-AddPass"
    Dependency "BaseMapShader" = "Hidden/TerrainEngine/Splatmap/Standard-Base"
    Dependency "BaseMapGenShader" = "Hidden/TerrainEngine/Splatmap/Standard-BaseGen"
    Fallback "Nature/Terrain/Standard"
}
