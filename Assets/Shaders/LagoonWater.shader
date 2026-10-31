Shader "PlatformFight/LagoonWater"
{
    // Laguunin vesi: kaksi hidasta aaltokuviota eri suuntiin, pieni elävä kirkkausvaihtelu ja kevyt heijastusten vääristymä.
    // Liike vaimenee lähes nollaan rannan (kuvan alfareunan) lähellä, jotta veden reuna pysyy paikallaan.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Distort ("Vääristymä (UV)", Float) = 0.006
        _Bright ("Kirkkausvaihtelu", Float) = 0.10
        _Speed ("Nopeus", Float) = 0.35
        _ShoreRadius ("Rannan vaimennusetäisyys (UV)", Float) = 0.045
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
            float _Distort, _Bright, _Speed, _ShoreRadius;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                // ranta: pienin alfa ympäristössä (8 näytettä); lähellä reunaa liike on lähes nolla
                float r = _ShoreRadius;
                float2 ra = float2(r * 0.6, r);
                float a = tex2D(_MainTex, i.uv).a;
                a = min(a, tex2D(_MainTex, i.uv + float2( ra.x, 0)).a); a = min(a, tex2D(_MainTex, i.uv + float2(-ra.x, 0)).a);
                a = min(a, tex2D(_MainTex, i.uv + float2(0,  ra.y)).a); a = min(a, tex2D(_MainTex, i.uv + float2(0, -ra.y)).a);
                a = min(a, tex2D(_MainTex, i.uv + ra * 0.7).a);           a = min(a, tex2D(_MainTex, i.uv - ra * 0.7).a);
                a = min(a, tex2D(_MainTex, i.uv + float2(ra.x, -ra.y) * 0.7).a); a = min(a, tex2D(_MainTex, i.uv + float2(-ra.x, ra.y) * 0.7).a);
                float calm = smoothstep(0.55, 0.98, a); calm *= calm;
                // kaksi aaltokuviota, jotka liikkuvat hitaasti eri suuntiin
                float2 p = i.uv * float2(22.0, 13.0);
                float w1 = sin(p.x * 1.0 + p.y * 0.6 + t * 1.3) * sin(p.y * 1.7 - p.x * 0.4 + t * 0.9);
                float w2 = sin(p.x * 0.7 - p.y * 0.9 - t * 1.1) * sin(p.y * 1.3 + p.x * 0.8 - t * 0.7);
                float2 d = float2(w1 - w2 * 0.5, w2 + w1 * 0.4) * _Distort * calm;
                fixed4 c = tex2D(_MainTex, i.uv + d) * i.color;
                float lum = 1.0 + (w1 + w2) * 0.5 * _Bright * calm;
                c.rgb *= lum;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
