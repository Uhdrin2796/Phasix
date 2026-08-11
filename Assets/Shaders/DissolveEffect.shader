Shader "Phasix/DissolveEffect"
{
    // Hand-coded URP shader implementing the standard Shader-Graph dissolve technique
    // (2026-08-11, user-directed — see DECISIONS.md -> [Combat]): a procedural noise field is
    // compared against a DissolveAmount threshold to clip pixels away, with a thin glowing band
    // just above the threshold ("about to dissolve") drawn in EdgeColor. Circular masking (via
    // UV distance-from-center) is baked in so a plain Quad primitive renders as a circle matching
    // the stage-creature's own on-screen shape, with no separate sprite/texture asset needed.
    //
    // Note on authorship: Unity MCP tooling can create raw shader files but cannot author an
    // actual Shader Graph (.shadergraph) node asset — those only exist through the Shader Graph
    // editor window. This is a hand-coded equivalent of that same technique (same math, same
    // visual result), not a node graph. See DissolveVfxBridge.cs for how this gets driven.
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Float) = 8
        _EdgeWidth("Edge Width", Range(0,0.5)) = 0.08
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                float4 _EdgeColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Cheap hash-based value noise — no texture asset dependency, matches Shader Graph's
            // "Simple Noise" node closely enough for this placeholder-tier effect.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Circular mask — clips the quad down to a circle matching the stage-creature's
                // own on-screen shape.
                float2 centered = IN.uv - 0.5;
                float distFromCenter = length(centered) * 2.0;
                clip(1.0 - distFromCenter);

                float noise = ValueNoise(IN.uv * _NoiseScale);

                // Core dissolve clip — pixels whose noise value is below DissolveAmount vanish.
                clip(noise - _DissolveAmount);

                // Thin glowing edge band just above the clip threshold.
                float edge = step(noise - _DissolveAmount, _EdgeWidth);
                float3 color = lerp(_BaseColor.rgb, _EdgeColor.rgb, edge);

                return float4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
