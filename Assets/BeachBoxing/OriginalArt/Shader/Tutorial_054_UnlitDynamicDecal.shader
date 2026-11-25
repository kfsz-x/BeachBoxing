Shader "Tutorial/054_UnlitDynamicDecal" {
	Properties {
		[HDR] _Color ("Tint", Vector) = (0,0,0,1)
		_MainTex ("Texture", 2D) = "white" {}
	}
	//DummyShaderTextExporter
	// Recovered from shipped Shader315 GLSL. The original projects a volume
	// onto opaque camera depth; its vertex program has no mesh UV input.
	SubShader{
		Tags { "Queue"="Transparent-400" "RenderType"="Transparent" "DisableBatching"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest LEqual
		Cull Back

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 screen : TEXCOORD0;
				float3 cameraRay : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = UnityObjectToClipPos(input.pos);
				output.screen = ComputeScreenPos(output.pos);
				output.cameraRay = mul(unity_ObjectToWorld, input.pos).xyz - _WorldSpaceCameraPos;
				return output;
			}

			sampler2D _MainTex;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float4 screen : TEXCOORD0;
				float3 cameraRay : TEXCOORD1;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				float3 ray = normalize(input.cameraRay);
				ray /= dot(ray, -UNITY_MATRIX_V[2].xyz);
				float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, input.screen.xy / input.screen.w);
				float3 world = _WorldSpaceCameraPos + ray * LinearEyeDepth(rawDepth);
				float3 local = mul(unity_WorldToObject, float4(world, 1)).xyz;
				clip(.5 - abs(local));
				return tex2D(_MainTex, local.xz + .5) * _Color;
			}

			ENDHLSL
		}
	}
}
