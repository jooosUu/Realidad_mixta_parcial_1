// Haz de luz falso (aditivo) que baja desde el techo sobre cada pieza.
Shader "MuseoAutista/Beam"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                // Más suave en los bordes del cono (según el ángulo de vista).
                float3 n = normalize(UnityObjectToWorldNormal(v.normal));
                float3 viewDir = normalize(WorldSpaceViewDir(v.vertex));
                float edge = saturate(abs(dot(n, viewDir)));
                o.color = v.color * _Color * edge * edge;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return fixed4(i.color.rgb * i.color.a, 1); }
            ENDCG
        }
    }
}
