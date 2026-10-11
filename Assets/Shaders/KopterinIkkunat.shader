Shader "Beat em up/Kopterin ikkunat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Kuva", 2D) = "white" {}
        _Color ("Sävy", Color) = (1,1,1,1)
        _Continuation ("Jatkokuva", Float) = 0
        _ImageSize ("Kuvan koko", Vector) = (1672,941,0,0)
        _FloorRow ("Lattian takareuna", Float) = 590
        _SeamWallWidth ("Pilarin häivytys", Float) = 40
        _SeamStart ("Häivytyksen alku", Float) = 0
        _SeamWidth ("Sauman häivytys", Float) = 0
        _SeamTint ("Sauman sävy", Vector) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment WindowFrag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _Continuation, _FloorRow, _SeamStart, _SeamWidth, _SeamWallWidth;
            float4 _ImageSize, _SeamTint;

            // Lattialinjat kohdakkain, ikkunoista taivas ja vasen sauma häivyttäen.
            fixed4 WindowFrag(v2f IN) : SV_Target
            {
                const float floorY = 590.0 / 941.0;
                float row = 1.0 - IN.texcoord.y;
                float sourceRow = row < floorY ? row / floorY * _FloorRow
                    : _FloorRow + (row - floorY) / (1.0 - floorY) * (_ImageSize.y - _FloorRow);
                float2 uv = float2(IN.texcoord.x, 1.0 - sourceRow / _ImageSize.y);
                float2 p = float2(uv.x * _ImageSize.x, sourceRow);
                float d;
                if (_Continuation > 1.5)
                {
                    // Ohjaamokuvassa on aidot ikkuna-aukot: säilytetään oma alfa.
                    d = 2.0;
                }
                else if (_Continuation > 0.5)
                {
                    d = length((p - float2(158, 326)) / float2(66, 70));
                    d = min(d, length((p - float2(414, 326)) / float2(67, 71)));
                    d = min(d, length((p - float2(694, 326)) / float2(67, 71)));
                    d = min(d, length((p - float2(984, 326)) / float2(68, 71)));
                }
                else
                {
                    d = length((p - float2(539, 305)) / float2(60, 65));
                    d = min(d, length((p - float2(795, 305)) / float2(62, 65)));
                    d = min(d, length((p - float2(1053, 305)) / float2(62, 65)));
                    d = min(d, length((p - float2(1310, 305)) / float2(62, 65)));
                    d = min(d, length((p - float2(1541, 305)) / float2(61, 65)));
                }
                fixed4 source = SampleSpriteTexture(uv);
                fixed4 c = source * IN.color;
                c.a = IN.color.a * (_Continuation > 1.5 ? source.a : smoothstep(0.98, 1.0, d));
                // Päivämaiseman siniset ja vaaleat pikselit vaihtuvat kuunvalotaivaaseen.
                bool extensionSky = _Continuation > 0.5 && _Continuation < 1.5 && p.x > 1610 && p.y > 200 && p.y < 450;
                if (extensionSky)
                {
                    float blue = step(source.r + 0.015, source.b) * step(source.g, source.b) * step(0.2, source.b);
                    float cloud = step(0.65, min(source.r, min(source.g, source.b)));
                    c.a *= 1.0 - max(blue, cloud);
                }
                if (_SeamWidth > 0.0)
                {
                    float blend = smoothstep(_SeamStart, _SeamStart + _SeamWidth, p.x);
                    if (_Continuation > 1.5)
                    {
                        // Ovi kokonaisena ohjaamokuvasta, lattia leveämmin häivyttäen.
                        float wallBlend = smoothstep(0.0, _SeamWallWidth, p.x);
                        float floorBlend = smoothstep(_FloorRow - 20.0, _FloorRow + 40.0, p.y);
                        blend = lerp(wallBlend, blend, floorBlend);
                    }
                    c.a *= blend;
                    // Kuvien kirkkausero tasoittuu oven ja lattian yli.
                    c.rgb *= lerp(_SeamTint.rgb, float3(1,1,1), blend);
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
