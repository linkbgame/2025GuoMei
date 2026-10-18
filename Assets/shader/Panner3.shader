Shader "FX/Panner3"
{
    Properties
    {
        [Enum(Addtive,1,Alpha,10)] _BlendMode("BlendMode", Float) = 10
        [Enum(Auto,0,Custom,1)]_AutoSwitch("AutoSwitch", Float) = 1
        _U("U", Float) = 0
        _V("V", Float) = 0
        [HDR]_MainColor("MainColor", Color) = (1,1,1,1)
        _MainTex("MainTex", 2D) = "white" {}
        _Mask("Mask", 2D) = "white" {}
    }
    
    SubShader
    {    
        Tags 
        { 
            "RenderType"="Transparent"
            "Queue"="Transparent" 
            "PreviewType"="Plane" 
        }
        Blend SrcAlpha [_BlendMode]
        Cull Off
        ColorMask RGBA
        ZWrite Off
        ZTest LEqual
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 texcoord1 : TEXCOORD0;
                float4 texcoord2 : TEXCOORD1;
                float4 color : COLOR;
                
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Mask;
            float4 _Mask_ST;
            half _U;
            half _V;
            half _AutoSwitch;
            half4 _MainColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord1.xy = v.texcoord.xy;
                o.texcoord2 = v.texcoord1;
                o.color = v.color;
                o.texcoord1.zw = 0;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                fixed4 finalColor;
                half2 uv0_MainTex = i.texcoord1.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                half2 uv0_Mask = i.texcoord1.xy * _Mask_ST.xy + _Mask_ST.zw;
                half2 HoweResult39 = (half2(( _Time.y * _U ) , ( _Time.y * _V )));
                half4 uv146 = i.texcoord2;
                uv146.xy = i.texcoord2.xy * float2( 1,1 ) + float2( 0,0 );
                half2 HoweResult54 = (half2(( uv0_MainTex.x + uv146.x ) , ( uv0_MainTex.y + uv146.y )));
                half2 lerpResult49 = lerp( ( uv0_MainTex + HoweResult39 ) , HoweResult54 , _AutoSwitch);
                half2 HoweResult57 = (half2(( uv0_Mask.x + uv146.z ) , ( uv0_Mask.y + uv146.w )));
                finalColor = ( tex2D( _MainTex, lerpResult49 ) * _MainColor * i.color * tex2D(_Mask, HoweResult57 ).a );
                return finalColor;
            }
            ENDCG
        }
    }
}