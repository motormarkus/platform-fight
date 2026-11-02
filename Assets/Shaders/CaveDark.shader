Shader "PlatformFight/CaveDark"
{
    // Luolan pimeys: koko ruudun tumma kerros, jossa on pehmeät valoaukot (_Lights: x, y, säde, voimakkuus maailmakoordinaateissa). Pimeys riippuu x:stä (_Zone: pimeä alkaa, täysi, valoisa alkaa, valoisa täysi).
    Properties
    {
        _DarkColor ("Pimeyden väri", Color) = (0.015,0.025,0.05,1)
        _Full ("Pimeys täysi", Float) = 0.9
        _Residual ("Pimeys valoisassa osassa", Float) = 0.2
        _Zone ("Vyöhykkeet (maailma-x)", Vector) = (0,0,0,0)
        _Warm ("Soihdun lämpö", Color) = (1,0.62,0.25,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 wp : TEXCOORD0; };
            fixed4 _DarkColor, _Warm;
            float _Full, _Residual;
            float4 _Zone;
            float4 _Lights[6];
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.wp = mul(unity_ObjectToWorld, v.vertex).xy; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float a = _Full * smoothstep(_Zone.x, _Zone.y, i.wp.x);
                a = lerp(a, _Residual, smoothstep(_Zone.z, _Zone.w, i.wp.x));
                float l = 0, warm = 0;
                for (int k = 0; k < 6; k++)
                {
                    float4 L = _Lights[k];
                    if (L.z <= 0) continue;
                    float2 d = (i.wp - L.xy) * float2(1.0, 1.2);
                    float v = (1.0 - smoothstep(L.z * 0.2, L.z, length(d))) * L.w;
                    l = max(l, v); warm = max(warm, v * v);
                }
                a *= (1.0 - l);
                return fixed4(_DarkColor.rgb * a + _Warm.rgb * warm * 0.10, a);
            }
            ENDCG
        }
    }
}
