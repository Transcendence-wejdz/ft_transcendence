// Enemy attack warning: crisp outline of the hit zone, a fill sweeping out to the edge as the hit approaches, and hazard stripes ahead of it.
Shader "Custom/AttackTelegraph"
{
    Properties
    {
        _Color("Color", Color) = (1, 0.25, 0.15, 1)
        _FlashColor("Flash Color", Color) = (1, 0.92, 0.8, 1)
        [HideInInspector] _Shape("Shape (mode, length, half angle or half width)", Vector) = (0, 1, 0.5, 0)
        [HideInInspector] _Progress("Progress", Range(0, 1)) = 0
        [HideInInspector] _Flash("Flash", Range(0, 1)) = 0
        [HideInInspector] _Alpha("Alpha", Range(0, 1)) = 1
        _Outline("Outline Width (world)", Float) = 0.05
        _FillAlpha("Fill Alpha", Range(0, 1)) = 0.12
        _ProgressAlpha("Progress Alpha", Range(0, 1)) = 0.35
        _StripeAlpha("Stripe Alpha", Range(0, 1)) = 0.1
        _StripeDensity("Stripe Density", Float) = 5
        _StripeSpeed("Stripe Speed", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 local : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FlashColor;
                float4 _Shape;
                half _Progress;
                half _Flash;
                half _Alpha;
                float _Outline;
                half _FillAlpha;
                half _ProgressAlpha;
                half _StripeAlpha;
                float _StripeDensity;
                float _StripeSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.local = input.positionOS.xy;
                return o;
            }

            // Local space has the attack pointing along +x from the origin; mode 0 = wedge, 1 = lane, 2 = full circle.
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.local;
                float mode = _Shape.x;
                float range = _Shape.y;
                float halfSize = _Shape.z;

                float forward;
                float edge;
                if (mode < 0.5)
                {
                    float r = length(p);
                    forward = r;
                    edge = min(range - r, (halfSize - abs(atan2(p.y, p.x))) * r);
                }
                else if (mode < 1.5)
                {
                    forward = p.x;
                    edge = min(min(range - p.x, halfSize - abs(p.y)), p.x);
                }
                else
                {
                    forward = length(p);
                    edge = range - forward;
                }

                float reached = _Progress * range;
                half alpha = _FillAlpha;
                if (forward <= reached)
                {
                    alpha = _ProgressAlpha;
                }
                else
                {
                    float stripe = frac((p.x + p.y) * _StripeDensity - _Time.y * _StripeSpeed);
                    alpha += stripe < 0.5 ? _StripeAlpha : 0.0;
                }
                if (abs(forward - reached) < _Outline && _Progress < 1.0)
                    alpha = max(alpha, 0.7);
                if (edge < _Outline)
                    alpha = 0.95;

                half3 rgb = lerp(_Color.rgb, _FlashColor.rgb, _Flash);
                alpha = lerp(alpha, max(alpha, 0.6), _Flash);
                return half4(rgb, alpha * _Alpha);
            }
            ENDHLSL
        }
    }
}
