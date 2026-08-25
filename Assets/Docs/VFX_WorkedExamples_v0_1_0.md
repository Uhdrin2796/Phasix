# Phasix — VFX Worked Examples: Text-Only vs. Hybrid
**Version:** 0.1.0
**Date:** August 2026
**Status:** Tutorial / worked examples — companion to `VFX_Pipeline_Directive_v0_1_0.md` Part 2.
**Errata (2026-08-24):** Testing steps in both examples updated to lead with video capture (full
cycle/sweep, saved and presented in-session) as the standard check, per `VFX_Pipeline_Directive`
Part 4 principle 7 — a single screenshot no longer treated as sufficient verification for
time-varying effects. Addition/refinement only — no version bump.
**Related:** VFX_Pipeline_Directive_v0_1_0.md (the reference doc these tutorials apply), Architecture_Directive_v0_1_0.md (Phase 3 — both examples assume the projectile/effect target is already a Scene GameObject, not the current `Painter2D` UI Toolkit content)

---

## Purpose

Two complete, contrasting examples, both real enough to build from:
- **Example A — Text-only (the realistic default for almost everything).** Fireball dissolve. No Unity Editor GUI session required at all, start to finish.
- **Example B — Hybrid, Custom Function Node (the deliberate exception).** A Corruption status visual. Included specifically because it's a case where the text-only default genuinely doesn't fit — a layered, composited look that needs eyes-on-it, per the decision heuristic in `VFX_Pipeline_Directive` Part 2.

Supersedes the earlier `ShaderGraph_WorkedExample_Fireball_v0_1_0.md`, which used the hybrid workflow for Fireball — reconsidered: Fireball is formula-verifiable and doesn't need a human GUI session at all, so it's the wrong effect to demonstrate the hybrid with. This doc corrects that pairing.

**Prerequisite for both:** `Architecture_Directive` Phase 3 must have landed — the effect target is a real `SpriteRenderer` GameObject, not `CombatProjectileVisual`'s current `Painter2D` drawing.

---

## Example A — Text-Only: Fireball Dissolve

### Step 1 — Claude Code writes the shader (plain `.shader`, no GUI, ever)

File: `Assets/Shaders/DissolveSprite.shader`

```hlsl
Shader "Phasix/URP2D/DissolveSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Dissolve Noise", 2D) = "white" {}
        _Threshold ("Dissolve Threshold", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0.001, 0.5)) = 0.05
        _EdgeColor ("Edge Color (HDR)", Color) = (2, 1, 0, 1)
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            float _Threshold, _EdgeWidth;
            float4 _EdgeColor, _Color;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv).r;

                // Discard pixels below the dissolve threshold
                clip(noise - _Threshold);

                // Glowing band just above the cutoff
                float edge = smoothstep(_Threshold, _Threshold + _EdgeWidth, noise) *
                             (1.0 - smoothstep(_Threshold + _EdgeWidth, _Threshold + _EdgeWidth * 2.0, noise));

                float4 col = tex * IN.color;
                col.rgb += _EdgeColor.rgb * edge;   // HDR push feeds Bloom
                return col;
            }
            ENDHLSL
        }
    }
}
```

Same math as `VFX_Pipeline_Directive`'s dissolve technique (noise vs. threshold via `clip`, glowing edge via `smoothstep`), same as `DissolveEffect.shader` already established — this is that formula, written as a real 2D-sprite-ready shader rather than pseudocode.

### Step 2 — Testing (no human required, no GUI session)

1. **Compile check** — save the file; a pink material means a syntax error, Console shows exactly where.
2. **Material Inspector preview** — create a Material from this shader (`manage_material`), sweep `_Threshold` in the Inspector; the preview swatch updates live, no Scene object needed.
3. **Video capture, the standard check (`VFX_Pipeline_Directive` Part 4, principle 7)** — assign the Material to a test `SpriteRenderer`, drive `_Threshold` through a full 0→1 sweep, and record it with an OS-level screen-recording tool targeting the Game view window. Save the clip and present it in-session — this is what actually confirms the dissolve *reads* correctly in motion, which neither the compile check nor a static preview swatch can tell you. A single screenshot remains a fine sanity check for "did this property change take effect," but isn't the verification step for "does this look right."

**Nothing in this loop required opening the Shader Graph window, because there is no graph — it's a shader like any other C# script in this project.**

### Step 3 — Material variants (Claude Code, repeatable, no further authoring)

| Material | `_NoiseTex` | `_EdgeColor` (HDR) |
|---|---|---|
| `Fire_Dissolve_Mat` | ember-pattern noise | bright orange/red |
| `Water_Dissolve_Mat` | ripple-pattern noise | bright cyan |
| `Lightning_Dissolve_Mat` | jagged/static noise | bright white-blue |

Same shader, different property values, all via `manage_material`. This is the realistic shape for nearly every skill in `VFX_Pipeline_Directive` Part 5's 9 families — dissolve, glow, noise-modulated color, crackle. No GUI session anywhere in this example, start to finish.

---

## Example B — Hybrid, Custom Function Node: Corruption Status Visual

**Why this effect specifically needed the exception:** unlike Fireball, "does this Corruption visual look right" isn't a formula you can verify by reading code — it's a layered composite (writhing distortion + color-shift + outline glow) where the *balance* between the three is a taste call. This is exactly the "needs-eyes-on-it" case from the decision heuristic.

### Step 1 — Claude Code writes the one genuinely fiddly piece as a formula (`.hlsl`)

The distortion (a domain-warp — offsetting UVs by a scrolling noise pattern, layered) is math that's easier to write than to wire as native nodes. Everything else in this effect (hue rotation, Fresnel-based outline) has a direct built-in Shader Graph node and doesn't need this treatment.

File: `Assets/Shaders/Include/DomainWarp.hlsl`

```hlsl
#ifndef PHASIX_DOMAIN_WARP_INCLUDED
#define PHASIX_DOMAIN_WARP_INCLUDED

// Offsets UV by a scrolling noise sample, layered twice for a writhing look
// rather than a simple single-pass wobble.
void DomainWarp_float(float2 UV, UnityTexture2D NoiseTex, UnitySamplerState NoiseSampler,
    float Time, float Strength, out float2 WarpedUV)
{
    #ifdef SHADERGRAPH_PREVIEW
        WarpedUV = UV;
    #else
        float2 offset1 = SAMPLE_TEXTURE2D_LOD(NoiseTex.tex, NoiseSampler.samplerstate, UV + Time * 0.05, 0).rg - 0.5;
        float2 offset2 = SAMPLE_TEXTURE2D_LOD(NoiseTex.tex, NoiseSampler.samplerstate, UV * 1.7 - Time * 0.03, 0).rg - 0.5;
        WarpedUV = UV + (offset1 + offset2) * Strength;
    #endif
}

#endif
```

### Step 2 — Human builds the graph shell (the real, one-time GUI session)

1. `Create > Shader > 2D Renderer > Unlit Sprite Graph`, name `Corruption_Status_Graph`.
2. Add a Custom Function Node, File mode, Source = `DomainWarp.hlsl`, Name = `DomainWarp`. Define ports matching the signature.
3. Wire: UV node → `DomainWarp`'s `UV` input; a Blackboard `_WarpStrength` (Float) → `Strength`; Time node → `Time`. `WarpedUV` output feeds the main sprite texture's UV input instead of raw UV — this is the writhing look.
4. Add a built-in **Hue** node downstream of the sampled color, driven by a Blackboard `_HueShift` (Float) — no custom function needed, this is a native node.
5. Add a built-in **Fresnel Effect** node for the outline glow, feeding Emission alongside the hue-shifted base color.
6. Wire the three layers together into the Fragment output, save.

**This step is genuinely GUI-only and doesn't have a text-only equivalent that's worth pursuing** — composing three visual layers and judging the balance between them is exactly what this tool is for.

### Step 3 — Preview and iterate

Main Preview shows the composited result live during the GUI session. Iterating the distortion formula itself (Step 1's `.hlsl`) needs no re-wiring — same free-editing property as Example A's dissolve formula. Iterating the Hue/Fresnel balance, though, **does** mean going back into the graph — those are native nodes, not text, by design, because that's the part that's actually a taste call. Once a Material exists from this graph, the same video-capture verification from Example A applies — a live capture of the writhing/color-shift/glow over a full cycle, saved and presented in-session, is what actually confirms the composited balance reads correctly, not the Main Preview window alone (which shows the effect isolated, not against the actual creature sprite and battle background it'll render over).

### Step 4 — Everything downstream, still Claude Code

Once `Corruption_Status_Graph` exists and a Material is made from it, applying it to the affected creature's `SpriteRenderer`, driving `_WarpStrength`/`_HueShift` from the status system's intensity value, and pooling any one-shot burst VFX around it — all ordinary scriptable work, same as Example A's Step 3.

---

## The contrast, stated plainly

| | Example A (Fireball) | Example B (Corruption) |
|---|---|---|
| GUI session required? | No, ever | Yes, once |
| Why | Formula-verifiable | Needs-eyes-on-it, layered composite |
| What's text | The whole shader | Just the distortion formula |
| What's graph-native | Nothing | Hue shift, Fresnel outline — the taste-call parts |
| Testing loop | Compile check → Material Inspector → video capture (full sweep) | Same, plus live Main Preview during the GUI session, then video capture in actual context |

**The realistic default is Example A's shape.** Example B exists to show what it looks like when an effect genuinely earns the exception — not as an equally-weighted alternative.

---

## Open items

1. Not yet built — blocked on `Architecture_Directive` Phase 3, same as everything else in the VFX pipeline.
2. Water/Lightning's actual noise textures aren't sourced yet — placeholder noise validates the workflow.
3. Whether Corruption's `_WarpStrength`/`_HueShift` map to a status intensity curve or a flat per-stack value isn't decided — doesn't block validating this workflow, does block the real skill shipping.
