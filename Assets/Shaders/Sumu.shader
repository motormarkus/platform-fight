Shader "PlatformFight/Sumu"
{
    // Liikkuva sumu: kaksi kohinakerrosta eri nopeuksilla. Tila 0 = nouseva sumu (tiheä alhaalla, haalenee ylös), tila 1 = matala vaakasuuntainen sumuverho.
    // Reunat häivytetään, joten sumulla ei ole näkyvää rajaa. Käytetään MeshRenderer-nelikulmiolla (UV 0..1).
    Properties
    {
        _Color ("Sumun väri", Color) = (0.86,0.84,0.76,1)
        _Density ("Tiheys", Range(0,1)) = 0.4
        _Scroll ("Liike (x,y) / s", Vector) = (0.01,0.05,0,0)
        _Scale ("Kohinan koko (x,y)", Vector) = (3,4,0,0)
        _Mode ("Tila (0 nouseva, 1 verho)", Float) = 0
        _EdgeX ("Sivureunan häivytys", Float) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
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
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color; float _Density, _Mode, _EdgeX; float4 _Scroll, _Scale;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), f.x), lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x), f.y);
            }
            float fbm(float2 p) { return vnoise(p) * 0.55 + vnoise(p * 2.1 + 7.3) * 0.3 + vnoise(p * 4.3 + 3.1) * 0.15; }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv;
                float2 p1 = uv * _Scale.xy + _Scroll.xy * t;
                float2 p2 = uv * _Scale.xy * 1.7 + float2(5.2, 1.3) + float2(-_Scroll.x * 0.7, _Scroll.y * 1.4) * t;
                float n = fbm(p1) * 0.6 + fbm(p2) * 0.4;
                n = smoothstep(0.30, 0.78, n);
                float vert = _Mode < 0.5 ? pow(saturate(1.0 - uv.y), 1.4) * smoothstep(0.0, 0.04, uv.y) : pow(sin(saturate(uv.y) * 3.14159), 1.6);
                float hor = smoothstep(0.0, _EdgeX, uv.x) * smoothstep(1.0, 1.0 - _EdgeX, uv.x);
                float a = saturate(_Density * n * vert * hor * 1.6);
                return fixed4(_Color.rgb * a, a);
            }
            ENDCG
        }
    }
}
