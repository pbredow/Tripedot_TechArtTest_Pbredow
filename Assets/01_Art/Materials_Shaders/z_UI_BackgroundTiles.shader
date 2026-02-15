Shader "Custom/UI/BackgroundTiles"
{
    Properties
    {
        _PatternTex ("Pattern Texture", 2D) = "white" {}
        _BackgroundColor ("Background Color", Color) = (0.12, 0.38, 0.76, 1.0)
        _GradientTopColor ("Gradient Top Color", Color) = (0.14, 0.42, 0.82, 1.0)
        _GradientBottomColor ("Gradient Bottom Color", Color) = (0.08, 0.28, 0.64, 1.0)
        _GradientFalloff ("Gradient Falloff", Range(0.2,4.0)) = 1.0
        _GradientBlend ("Gradient Blend", Range(0,1)) = 1.0
        _TileColor ("Tile Color", Color) = (0.08, 0.32, 0.66, 1.0)
        _TileOpacity ("Tile Opacity", Range(0,1)) = 0.28
        _TileCount ("Tile Count", Range(4,24)) = 11
        _TileSpacing ("Tile Spacing", Range(0,0.45)) = 0.08
        _PanDirection ("Pan Direction (XY)", Vector) = (1.0, 0.0, 0.0, 0.0)
        _PanSpeed ("Pan Speed", Range(-2.0,2.0)) = 0.06
        _AppearSpeedMin ("Pulse Speed Min", Range(0.02,1.0)) = 0.18
        _AppearSpeedMax ("Pulse Speed Max", Range(0.02,1.0)) = 0.42
        _OpacityJitter ("Opacity Jitter", Range(0,1)) = 0.12
        _MinTileVisibility ("Min Tile Visibility", Range(0,1)) = 0.6

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

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
            Name "UIBackgroundTiles"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _PatternTex;
            float4 _PatternTex_ST;
            half4 _Color;
            half4 _BackgroundColor;
            half4 _GradientTopColor;
            half4 _GradientBottomColor;
            half _GradientFalloff;
            half _GradientBlend;
            half4 _TileColor;
            half _TileOpacity;
            half _TileCount;
            half _TileSpacing;
            float4 _PanDirection;
            half _PanSpeed;
            half _AppearSpeedMin;
            half _AppearSpeedMax;
            half _OpacityJitter;
            half _MinTileVisibility;
            float4 _ClipRect;

            inline float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _PatternTex);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Rotate UV by 45 degrees around center, then tile.
                float2 centered = i.uv - 0.5;
                const float c = 0.70710678;
                float2 rotated = float2(
                    centered.x * c - centered.y * c,
                    centered.x * c + centered.y * c
                ) + 0.5;

                float2 panDir = _PanDirection.xy;
                float panLen = max(length(panDir), 1e-5);
                panDir /= panLen;
                float2 panned = rotated + panDir * (_Time.y * _PanSpeed);

                float2 tiled = panned * _TileCount;
                float2 tileId = floor(tiled);
                float2 tileUV = frac(tiled);

                // Increase spacing by shrinking usable UV area per tile.
                half spacing = saturate(_TileSpacing);
                half invScale = 1.0h / max(0.001h, (1.0h - spacing));
                float2 sampleUV = (tileUV - 0.5) * invScale + 0.5;
                half inside = step(0.0, sampleUV.x) * step(sampleUV.x, 1.0) * step(0.0, sampleUV.y) * step(sampleUV.y, 1.0);

                half4 tex = tex2D(_PatternTex, saturate(sampleUV));

                // For UI textures with soft transparent shadows, alpha-only masking is cleaner.
                half shapeMask = tex.a * inside;

                // Per-tile random speed and phase so tiles pulse independently.
                float rnd = Hash12(tileId);
                half speed = lerp(_AppearSpeedMin, _AppearSpeedMax, (half)rnd);
                // Pure sin pulse per tile (phase-randomized).
                half pulse = 0.5h + 0.5h * sin((_Time.y * speed) + rnd * 6.2831h);
                half drift = lerp(saturate(_MinTileVisibility), 1.0h, pulse);
                drift = lerp(1.0h, drift, saturate(_OpacityJitter * 1.25h));

                // Tiles are always present; only opacity drifts over time.
                half pattern = shapeMask * _TileOpacity * drift;
                
                // Simple vertical gradient: top (UV.y=1) to bottom (UV.y=0).
                half gradT = saturate(1.0h - i.uv.y);
                // No pow: cheap falloff curve by blending linear and smoothstep.
                half falloff01 = saturate((_GradientFalloff - 0.2h) / 3.8h);
                half smoothT = gradT * gradT * (3.0h - 2.0h * gradT);
                gradT = lerp(gradT, smoothT, falloff01);

                half3 gradientRgb = lerp(_GradientTopColor.rgb, _GradientBottomColor.rgb, gradT);
                half3 baseRgb = lerp(_BackgroundColor.rgb, gradientRgb, saturate(_GradientBlend));
                half3 rgb = lerp(baseRgb, _TileColor.rgb, pattern);
                half alpha = _BackgroundColor.a * i.color.a;

                half4 col = half4(rgb, alpha) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
