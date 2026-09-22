// Sombreado cartoon: iluminación por bandas + luz de borde, estilo "parque de dinosaurios" caricaturesco.
#ifndef MUSEO_TOON_INCLUDED
#define MUSEO_TOON_INCLUDED

#include "UnityCG.cginc"
#include "Lighting.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
fixed4 _Color;
fixed4 _EmissionColor;
fixed4 _ShadowTint;
half _RimPower;
half _RimStrength;
half _Outline;
fixed4 _OutlineColor;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldNormal : TEXCOORD1;
    float3 worldPos : TEXCOORD2;
    half3 ambient : TEXCOORD3;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vertToon(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2f, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.ambient = ShadeSH9(half4(o.worldNormal, 1));
    return o;
}

fixed4 fragToon(v2f i) : SV_Target
{
    float3 n = normalize(i.worldNormal);
    float3 l = normalize(_WorldSpaceLightPos0.xyz);
    float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);
    float ndl = dot(n, l);
    // Dos bandas suaves (sombra / medio / luz)
    float band = smoothstep(-0.05, 0.05, ndl) * 0.6 + smoothstep(0.45, 0.55, ndl) * 0.4;
    fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
    half3 direct = _LightColor0.rgb * lerp(_ShadowTint.rgb, 1.0, band);
    half3 col = albedo.rgb * (direct * 0.75 + i.ambient * 0.55);
    float rim = pow(1.0 - saturate(dot(n, v)), _RimPower) * _RimStrength * saturate(ndl + 0.6);
    col += rim * _LightColor0.rgb * 0.5;
    col += _EmissionColor.rgb;
    return fixed4(col, 1);
}

struct v2fOutline
{
    float4 pos : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2fOutline vertOutline(appdata v)
{
    v2fOutline o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(v2fOutline, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 wn = UnityObjectToWorldNormal(v.normal);
    float dist = distance(wpos, _WorldSpaceCameraPos);
    wpos += wn * _Outline * clamp(dist * 0.25, 0.6, 2.5);
    o.pos = UnityWorldToClipPos(wpos);
    return o;
}

fixed4 fragOutline(v2fOutline i) : SV_Target
{
    return _OutlineColor;
}

#endif
