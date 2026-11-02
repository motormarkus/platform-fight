Shader "PlatformFight/WadeSprite"
{
    // Kahlaava hahmo: veden pinnan (_WaterY, maailman y) alapuolella hahmo on sinertävä ja läpikuultava, pinnalla vaalea aaltoileva viiva.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaterY ("Veden pinta (y)", Float) = -9999
        _WaterColor ("Veden väri", Color) = (0.12,0.5,0.58,1)
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
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float2 wp : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color, _WaterColor;
            float _WaterY;
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xy; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                float surf = _WaterY + sin(i.wp.x * 5.0 + _Time.y * 3.0) * 0.035 + sin(i.wp.x * 11.0 - _Time.y * 4.3) * 0.015;
                float under = step(i.wp.y, surf);
                float depth = saturate((surf - i.wp.y) / 1.2);
                float3 sub = lerp(c.rgb, _WaterColor.rgb * (0.55 + 0.25 * c.rgb), 0.38 + 0.25 * depth);
                c.rgb = lerp(c.rgb, sub, under);
                c.a *= lerp(1.0, 0.78, under);
                float ln = (1.0 - saturate(abs(i.wp.y - surf) / 0.05)) * (_WaterY > -9000 ? 1.0 : 0.0);
                c.rgb = lerp(c.rgb, float3(0.85, 0.97, 1.0), ln * 0.7 * step(0.05, c.a));
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
