Shader "MuseoAutista/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Textura", 2D) = "white" {}
        _EmissionColor ("Emisión", Color) = (0,0,0,0)
        _ShadowTint ("Tono de sombra", Color) = (0.80,0.79,0.86,1)
        _RimPower ("Potencia borde", Range(1,8)) = 3
        _RimStrength ("Intensidad borde", Range(0,1)) = 0.25
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
    }
    Fallback "Diffuse"
}
