Shader "Total Fist/Poker Asset Model"
{
    Properties
    {
        _MainTex ("Purchased texture", 2D) = "white" {}
        _Color ("Source material colour", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Back ZWrite On
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a2v { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; };
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color;
            v2f vert(a2v v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.normal=UnityObjectToWorldNormal(v.normal); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv)*_Color; clip(c.a-.04);
                float light=.82+.22*saturate(dot(normalize(i.normal),normalize(float3(-.35,.5,-1))));
                // Ostetut tekstuurit jäivät harmaiksi: palautetaan väriä ja kontrastia.
                float luma=dot(c.rgb,float3(.299,.587,.114));
                float3 rgb=lerp(luma.xxx,c.rgb,1.15);
                // Pehmennä puhtaan valkoinen kermanväriseksi, jotta kortit istuvat maalattuun pöytäkuvaan.
                rgb*=lerp(float3(1,1,1),float3(.95,.92,.85),smoothstep(.7,1,luma));
                return fixed4(saturate(rgb*light),1);
            }
            ENDCG
        }
    }
}
