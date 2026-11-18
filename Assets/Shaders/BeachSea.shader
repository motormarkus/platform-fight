Shader "PlatformFight/BeachSea"
{
    // Rannan meri (loppuranta): kuvan aallot ja vaahtopäät liikkuvat koodilla. Vaakasuora keinunta (voimistuu etualalle), pieni pystysuora heiluminen,
    // vaahdon (vaalea, vähän värillinen) kirkkauden aaltoilu ja kevyt kuun kimallus. Horisontin yläpuoli (saaret, taivas) pysyy paikallaan.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Horizon ("Horisontti (UV y, alhaalta)", Float) = 0.4825
        _Sway ("Vaakakeinunta (UV)", Float) = 0.0018
        _Bob ("Pystyheilunta (UV)", Float) = 0.0009
        _Foam ("Vaahdon kirkkauden vaihtelu", Float) = 0.35
        _Speed ("Nopeus", Float) = 1.0
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
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _Horizon, _Sway, _Bob, _Foam, _Speed;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                // veden syvyys: 0 horisontissa, 1 etualalla (liike kasvaa lähemmäs)
                float depth = saturate((_Horizon - i.uv.y) / _Horizon);
                depth = depth * depth * (3.0 - 2.0 * depth);
                float2 q = i.uv * float2(40.0, 70.0);
                float sx = sin(q.y * 1.0 + t * 1.15 + q.x * 0.15) + 0.5 * sin(q.y * 2.3 - t * 0.8 + q.x * 0.4);
                float sy = sin(q.x * 1.0 - t * 1.6 + q.y * 0.3);
                float2 d = float2(sx * _Sway, sy * _Bob) * depth;
                fixed4 c = tex2D(_MainTex, i.uv + d) * i.color;
                // vaahto: vaalea ja vähän värillinen -> kirkkaus aaltoilee (vaahtopäät "hengittävät") kulkien oikealta vasemmalle
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float mx = max(c.r, max(c.g, c.b)), mn = min(c.r, min(c.g, c.b));
                float sat = (mx - mn) / max(mx, 0.001);
                float foam = smoothstep(0.55, 0.85, lum) * (1.0 - smoothstep(0.1, 0.45, sat));
                float pulse = 0.5 + 0.5 * sin(i.uv.x * 90.0 + t * 2.1 + i.uv.y * 30.0);
                c.rgb *= 1.0 + foam * _Foam * (pulse - 0.35) * (0.4 + 0.6 * depth);
                // kuun kimallus: pienet vaaleat välähdykset aallonharjoilla (vain vedessä, vaalean alueen läheisyydessä)
                float spark = pow(0.5 + 0.5 * sin(i.uv.x * 260.0 + t * 3.0) * sin(i.uv.y * 190.0 - t * 2.3), 18.0) * smoothstep(0.25, 0.6, lum);
                c.rgb += spark * 0.25 * float3(0.8, 0.9, 1.0) * c.a;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
