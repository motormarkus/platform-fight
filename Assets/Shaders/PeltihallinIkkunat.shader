Shader "Beat em up/Peltihallin ikkunat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Kuva", 2D) = "white" {}
        _Color ("Sävy", Color) = (1,1,1,1)
        _ImageSize ("Kuvan koko", Vector) = (2172,724,0,0)
        _WindowA ("Ikkuna 1: vasen ylä oikea ala", Vector) = (1131,180,1344,251)
        _WindowB ("Ikkuna 2: vasen ylä oikea ala", Vector) = (1678,180,1864,250)
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
            float4 _ImageSize, _WindowA, _WindowB;
            float WindowHole(float2 p, float4 rect)
            {
                float edge = min(min(p.x - rect.x, rect.z - p.x), min(p.y - rect.y, rect.w - p.y));
                return smoothstep(0.0, 1.0, edge);
            }
            fixed4 WindowFrag(v2f IN) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(IN.texcoord);
                float2 p = float2(IN.texcoord.x, 1.0 - IN.texcoord.y) * _ImageSize.xy;
                float hole = max(WindowHole(p, _WindowA), WindowHole(p, _WindowB));
                fixed4 color = source * IN.color;
                // Ikkunat ovat aukkoja, muu sisätila peittää taustamaiseman täysin.
                color.a = IN.color.a * (1.0 - hole);
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
