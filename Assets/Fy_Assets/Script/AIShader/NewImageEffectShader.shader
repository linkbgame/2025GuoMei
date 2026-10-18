Shader "Custom/UI_InkSpread"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InkColor("Ink Color", Color) = (0,0,0,1)
        _Radius("Radius", Range(0,1)) = 0
        _Feather("Feather", Range(0.001,0.5)) = 0.08
        _Alpha("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _InkColor;
            float _Radius;
            float _Feather;
            float _Alpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, i.uv) * i.color;

                // 以 UV 中心为扩散中心（确保 Image pivot 在中心）
                float2 center = float2(0.5, 0.5);
                float2 d = i.uv - center;
                // 用对角线归一化（使 maxRadius 在 0..1 更直观）
                float dist = length(d) / 0.70710678;

                // 墨水遮罩：中心为1，边缘过渡到0
                float inkMask = saturate(1.0 - smoothstep(_Radius - _Feather, _Radius, dist));

                // 墨色叠加（整体 alpha 受 _Alpha 控制）
                fixed4 ink = _InkColor;
                float inkA = ink.a * _Alpha * inkMask;

                // 将墨色作为混合覆盖到 baseCol（更像墨水覆盖效果）
                fixed4 outCol = lerp(baseCol, ink, inkA);

                // 维持合成透明度
                outCol.a = baseCol.a + (1 - baseCol.a) * inkA;
                return outCol;
            }
            ENDCG
        }
    }
}
