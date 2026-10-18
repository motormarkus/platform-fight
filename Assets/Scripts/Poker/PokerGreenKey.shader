Shader "Total Fist/Poker Green Key World"
{
    Properties
    {
        [PerRendererData] _MainTex ("First frame", 2D) = "white" {}
        _VideoTex ("Video", 2D) = "black" {}
        _UseVideo ("Video ready", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            sampler2D _MainTex, _VideoTex;
            float _UseVideo; fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c = _UseVideo > .5 ? tex2D(_VideoTex,i.uv) : tex2D(_MainTex,i.uv);
                float strongest=max(c.r,c.b);
                c.a *= 1-smoothstep(.12,.40,c.g-strongest);
                c.g=min(c.g,strongest+.08); // Despill the keyed edge; teal table felt stays intact.
                return c*i.color;
            }
            ENDCG
        }
    }
}
