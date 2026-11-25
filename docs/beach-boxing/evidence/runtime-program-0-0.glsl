#version 300 es

uniform 	vec4 hlslcc_mtx4x4unity_ObjectToWorld[4];
uniform 	vec4 hlslcc_mtx4x4unity_MatrixVP[4];
uniform 	vec4 _Control_ST;
uniform 	vec4 _Control_TexelSize;
uniform 	vec4 _Splat0_ST;
uniform 	vec4 _Splat1_ST;
uniform 	vec4 _Splat2_ST;
uniform 	vec4 _Splat3_ST;
in highp vec4 in_POSITION0;
in highp vec2 in_TEXCOORD0;
out highp vec2 vs_TEXCOORD0;
out highp vec2 vs_TEXCOORD1;
out highp vec2 vs_TEXCOORD2;
out highp vec2 vs_TEXCOORD3;
out highp vec2 vs_TEXCOORD4;
vec4 u_xlat0;
vec4 u_xlat1;
vec2 u_xlat4;
void main()
{
    u_xlat0 = in_POSITION0.yyyy * hlslcc_mtx4x4unity_ObjectToWorld[1];
    u_xlat0 = hlslcc_mtx4x4unity_ObjectToWorld[0] * in_POSITION0.xxxx + u_xlat0;
    u_xlat0 = hlslcc_mtx4x4unity_ObjectToWorld[2] * in_POSITION0.zzzz + u_xlat0;
    u_xlat0 = u_xlat0 + hlslcc_mtx4x4unity_ObjectToWorld[3];
    u_xlat1 = u_xlat0.yyyy * hlslcc_mtx4x4unity_MatrixVP[1];
    u_xlat1 = hlslcc_mtx4x4unity_MatrixVP[0] * u_xlat0.xxxx + u_xlat1;
    u_xlat1 = hlslcc_mtx4x4unity_MatrixVP[2] * u_xlat0.zzzz + u_xlat1;
    gl_Position = hlslcc_mtx4x4unity_MatrixVP[3] * u_xlat0.wwww + u_xlat1;
    u_xlat0.xy = _Control_TexelSize.zw + vec2(-1.0, -1.0);
    u_xlat4.xy = in_TEXCOORD0.xy * _Control_ST.xy + _Control_ST.zw;
    u_xlat0.xy = u_xlat4.xy * u_xlat0.xy + vec2(0.5, 0.5);
    vs_TEXCOORD0.xy = u_xlat0.xy * _Control_TexelSize.xy;
    vs_TEXCOORD1.xy = u_xlat4.xy * _Splat0_ST.xy + _Splat0_ST.zw;
    vs_TEXCOORD2.xy = u_xlat4.xy * _Splat1_ST.xy + _Splat1_ST.zw;
    vs_TEXCOORD3.xy = u_xlat4.xy * _Splat2_ST.xy + _Splat2_ST.zw;
    vs_TEXCOORD4.xy = u_xlat4.xy * _Splat3_ST.xy + _Splat3_ST.zw;
    return;
}
