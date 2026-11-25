#version 300 es

precision highp float;
precision highp int;
uniform 	float _Smoothness0;
uniform 	float _Smoothness1;
uniform 	float _Smoothness2;
uniform 	float _Smoothness3;
uniform mediump sampler2D _Control;
uniform mediump sampler2D _Splat0;
uniform mediump sampler2D _Splat1;
uniform mediump sampler2D _Splat2;
uniform mediump sampler2D _Splat3;
in highp vec2 vs_TEXCOORD0;
in highp vec2 vs_TEXCOORD1;
in highp vec2 vs_TEXCOORD2;
in highp vec2 vs_TEXCOORD3;
in highp vec2 vs_TEXCOORD4;
layout(location = 0) out highp vec4 SV_Target0;
vec4 u_xlat0;
vec4 u_xlat1;
vec4 u_xlat2;
void main()
{
    u_xlat0 = texture(_Splat0, vs_TEXCOORD1.xy);
    u_xlat0.w = u_xlat0.w * _Smoothness0;
    u_xlat1 = texture(_Splat1, vs_TEXCOORD2.xy);
    u_xlat1.w = u_xlat1.w * _Smoothness1;
    u_xlat2 = texture(_Control, vs_TEXCOORD0.xy);
    u_xlat1 = u_xlat1 * u_xlat2.yyyy;
    u_xlat0 = u_xlat0 * u_xlat2.xxxx + u_xlat1;
    u_xlat1 = texture(_Splat2, vs_TEXCOORD3.xy);
    u_xlat1.w = u_xlat1.w * _Smoothness2;
    u_xlat0 = u_xlat1 * u_xlat2.zzzz + u_xlat0;
    u_xlat1 = texture(_Splat3, vs_TEXCOORD4.xy);
    u_xlat1.w = u_xlat1.w * _Smoothness3;
    SV_Target0 = u_xlat1 * u_xlat2.wwww + u_xlat0;
    return;
}
