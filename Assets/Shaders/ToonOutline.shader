// Igual que Toon pero con contorno negro (casco invertido) para los modelos.
Shader "MuseoAutista/ToonOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Textura", 2D) = "white" {}
        _EmissionColor ("Emisión", Color) = (0,0,0,0)
        _ShadowTint ("Tono de sombra", Color) = (0.80,0.79,0.86,1)
        _RimPower ("Potencia borde", Range(1,8)) = 3
        _RimStrength ("Intensidad borde", Range(0,1)) = 0.25
        _Outline ("Grosor contorno", Range(0,0.05)) = 0.012
        _OutlineColor ("Color contorno", Color) = (0.12,0.10,0.14,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vertToon
            #pragma fragment fragToon
            #pragma multi_compile_instancing
            #include "Toon.cginc"
            ENDCG
        }
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_instancing
            #include "Toon.cginc"
            ENDCG
        }
    }
    Fallback "Diffuse"
}
