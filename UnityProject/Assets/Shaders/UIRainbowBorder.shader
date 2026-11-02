// UI card border: a rainbow flowing around the edge, timed by _RainbowTime since time is frozen.
Shader "Custom/UIRainbowBorder"
{
    Properties
    {
        [PerRendererData] _MainTex("Unused", 2D) = "white" {}
        _CardSize("Card Size", Vector) = (150, 130, 0, 0)
        _BorderWidth("Border Width", Float) = 4
        _GlowWidth("Inner Glow Width", Float) = 10
        _GlowStrength("Inner Glow Strength", Range(0, 1)) = 0.35
        _Speed("Flow Speed", Float) = 0.35
        _Bands("Rainbow Bands", Float) = 2
        _Opacity("Opacity", Range(0, 1)) = 0.75

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 mask : TEXCOORD1;
            };

            float4 _CardSize;
            float _BorderWidth;
            float _GlowWidth;
            float _GlowStrength;
            float _Speed;
            float _Bands;
            float _Opacity;
            float _RainbowTime;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            // Same soft-clip data as Default UI, so the reels' RectMask2D fade still applies.
            v2f vert(appdata v)
            {
                v2f o;
                float2 pixelSize;
                float4 clampedRect;

                o.pos = UnityObjectToClipPos(v.vertex);
                pixelSize = o.pos.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                clampedRect = clamp(_ClipRect, -2e10, 2e10);
                o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize)));
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // Fully saturated hue, 0 to 1 looping.
            float3 HueToRgb(float hue)
            {
                float3 rgb = abs(frac(hue + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1;
                return saturate(rgb);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 fromEdge = min(i.uv, 1 - i.uv) * _CardSize.xy;
                float edge = min(fromEdge.x, fromEdge.y);
                float2 fromCenter = i.uv - 0.5;
                float angle = atan2(fromCenter.y, fromCenter.x) / UNITY_TWO_PI;
                float border = 1 - step(_BorderWidth, edge);
                float glow = _GlowStrength * (1 - saturate((edge - _BorderWidth) / max(_GlowWidth, 0.001)));
                fixed4 col = fixed4(HueToRgb(angle * _Bands - _RainbowTime * _Speed), max(border, glow) * _Opacity);

                col *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                float2 clipped = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
                col.a *= clipped.x * clipped.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
