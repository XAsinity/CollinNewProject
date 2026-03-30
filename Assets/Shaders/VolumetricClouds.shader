Shader "Custom/VolumetricClouds"
{
    Properties
    {
        [Header(Cloud Layer 1)]
        _EnableClouds ("Enable Clouds", Float) = 1
        _CloudScale ("Cloud Scale", Range(1, 20)) = 5.0
        _CloudSpeed ("Cloud Speed", Range(0, 2)) = 0.3
        _CloudDirection ("Cloud Wind Direction", Vector) = (1, 0, 0.5, 0)
        _CloudDensity ("Cloud Density", Range(0, 2)) = 1.0
        _CloudSharpness ("Cloud Sharpness", Range(0.1, 5.0)) = 1.5
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _CloudBrightness ("Cloud Brightness", Range(0, 2)) = 1.0
        _CloudDarkness ("Cloud Darkness (Shadow Intensity)", Range(0, 1)) = 0.5
        _CloudColor ("Cloud Color Tint", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.35, 0.35, 0.40, 1)
        _CloudEdgeSoftness ("Cloud Edge Softness", Range(0.0, 0.5)) = 0.35
        _CloudVariation ("Cloud Variation/Turbulence", Range(0, 1)) = 0.5
        _CloudDissolveOffset ("Cloud Dissolve Offset", Vector) = (0, 0, 0, 0)
        _CloudHeight ("Cloud Height Bias", Range(-0.5, 0.8)) = 0.2
        _CloudDayColor ("Cloud Day Color", Color) = (0.95, 0.95, 0.95, 1)
        _CloudNightColor ("Cloud Night Color", Color) = (0.05, 0.05, 0.1, 1)
        _CloudSunsetColor ("Cloud Sunset Color", Color) = (1.0, 0.5, 0.2, 1)
        _CloudAlpha ("Cloud Opacity", Range(0, 1)) = 0.8
        _CloudHorizonCoverage ("Cloud Horizon Coverage", Range(0, 1)) = 0.8
        _CloudZenithBlend ("Cloud Zenith Blend", Range(0, 1)) = 0.4
        _CloudShellRadius ("Cloud Shell Radius", Range(1000, 1000000)) = 25000.0
        _Cloud2ShellRadius ("Cloud2 Shell Radius", Range(1000, 1000000)) = 35000.0
        _CloudShellFlattening ("Cloud Shell Flattening", Range(0, 10)) = 0.0

        [Header(Cloud Layer 2)]
        _Cloud2Coverage ("Cloud2 Coverage", Range(0, 1)) = 0.0
        _Cloud2Scale ("Cloud2 Scale", Range(1, 20)) = 8.0
        _Cloud2Speed ("Cloud2 Speed", Range(0, 2)) = 0.15
        _Cloud2Density ("Cloud2 Density", Range(0, 2)) = 0.8
        _Cloud2Sharpness ("Cloud2 Sharpness", Range(0.1, 5.0)) = 2.0
        _Cloud2Brightness ("Cloud2 Brightness", Range(0, 2)) = 1.0
        _Cloud2Darkness ("Cloud2 Darkness", Range(0, 1)) = 0.3
        _Cloud2Color ("Cloud2 Color Tint", Color) = (0.96, 0.96, 0.98, 1)
        _Cloud2ShadowColor ("Cloud2 Shadow Color", Color) = (0.50, 0.52, 0.58, 1)
        _Cloud2Opacity ("Cloud2 Opacity", Range(0, 1)) = 0.3
        _CloudLayer2Height ("Cloud Layer 2 Height Bias", Range(-0.5, 0.8)) = 0.1

        [Header(Shared Time and Light)]
        _TimeOfDay ("Time Of Day", Range(0, 1)) = 0.5
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _MoonDirection ("Moon Direction", Vector) = (0, -1, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "VolumetricClouds"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ─── CAMERA MATRICES (set from C# render pass) ──────────────────
            float4x4 _InvProjectionMatrix;
            float4x4 _InvViewMatrix;

            // ─── CONSTANTS ───────────────────────────────────────────────────
            #define EPSILON 0.0001

            // ─── CLOUD LAYER 1 ───────────────────────────────────────────────
            float  _EnableClouds;
            float  _CloudScale;
            float  _CloudSpeed;
            float4 _CloudDirection;
            float  _CloudDensity;
            float  _CloudSharpness;
            float  _CloudCoverage;
            float  _CloudBrightness;
            float  _CloudDarkness;
            float4 _CloudColor;
            float4 _CloudShadowColor;
            float  _CloudEdgeSoftness;
            float  _CloudVariation;
            float4 _CloudDissolveOffset;
            float  _CloudHeight;
            float4 _CloudDayColor;
            float4 _CloudNightColor;
            float4 _CloudSunsetColor;
            float  _CloudAlpha;
            float  _CloudHorizonCoverage;
            float  _CloudZenithBlend;
            float  _CloudShellRadius;
            float  _CloudShellFlattening;

            // ─── CLOUD LAYER 2 ───────────────────────────────────────────────
            float  _Cloud2Coverage;
            float  _Cloud2Scale;
            float  _Cloud2Speed;
            float  _Cloud2Density;
            float  _Cloud2Sharpness;
            float  _Cloud2Brightness;
            float  _Cloud2Darkness;
            float4 _Cloud2Color;
            float4 _Cloud2ShadowColor;
            float  _Cloud2Opacity;
            float  _Cloud2ShellRadius;
            float  _CloudLayer2Height;

            // ─── SHARED ──────────────────────────────────────────────────────
            float  _TimeOfDay;
            float4 _SunDirection;
            float4 _MoonDirection;

            // ─── STRUCTS ─────────────────────────────────────────────────────

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 posCS  : SV_POSITION;
                float2 ndcPos : TEXCOORD0;
            };

            // ─── VERTEX ──────────────────────────────────────────────────────
            // Generates a full-screen triangle using SV_VertexID (no mesh needed).

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = float2((IN.vertexID << 1) & 2, IN.vertexID & 2);
                OUT.posCS  = float4(uv * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
                OUT.ndcPos = OUT.posCS.xy;
                return OUT;
            }

            // ─── HASH / NOISE ────────────────────────────────────────────────

            float HashSingle(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float Hash(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;
                return lerp(
                    lerp(
                        lerp(HashSingle(n +   0.0), HashSingle(n +   1.0), f.x),
                        lerp(HashSingle(n +  57.0), HashSingle(n +  58.0), f.x), f.y),
                    lerp(
                        lerp(HashSingle(n + 113.0), HashSingle(n + 114.0), f.x),
                        lerp(HashSingle(n + 170.0), HashSingle(n + 171.0), f.x), f.y),
                    f.z);
            }

            float FBM(float3 p)
            {
                float f = 0.0;
                f += 0.50000 * Noise3D(p); p *= 2.02;
                f += 0.25000 * Noise3D(p); p *= 2.13;
                f += 0.12500 * Noise3D(p); p *= 2.24;
                f += 0.06250 * Noise3D(p); p *= 2.35;
                f += 0.03125 * Noise3D(p);
                return f;
            }

            float FBMFine(float3 p)
            {
                float f = 0.0;
                f += 0.50000 * Noise3D(p); p *= 2.31;
                f += 0.25000 * Noise3D(p); p *= 2.47;
                f += 0.12500 * Noise3D(p); p *= 2.59;
                f += 0.06250 * Noise3D(p); p *= 2.68;
                f += 0.03125 * Noise3D(p); p *= 2.73;
                f += 0.01563 * Noise3D(p);
                return f;
            }

            // ─── HENYEY-GREENSTEIN PHASE FUNCTION ───────────────────────────
            // Models forward/backward scattering of light through the cloud.
            // g > 0 = forward scattering (silver lining on sun-facing edges)
            // g < 0 = backward scattering (glory / backlit glow)

            float HenyeyGreenstein(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }

            // ─── SAMPLE CLOUD DENSITY AT A SINGLE SHELL POINT ────────────────
            // Returns cloud density [0,1] at position 'shellPt' (already in noise-frequency
            // space), using the provided coverage/density/sharpness/edge controls.

            float SampleDensity(float3 shellPt, float coverage, float density,
                                 float sharpness, float edgeSoftness, float variation,
                                 float3 dissolveOff, float layerSeed)
            {
                float3 p = shellPt + dissolveOff
                         + float3(layerSeed * 3.7, layerSeed * 2.1, layerSeed * 1.3);

                // Base shape from three-octave FBM blend
                float baseShape = FBM(p)              * 0.40
                                + FBM(p * float3(1.0, 0.97, 1.0)) * 0.35
                                + FBM(p * float3(1.0, 0.94, 1.0)) * 0.25;

                // Mid-frequency detail and high-frequency wisps
                float wispW = lerp(0.05, 0.15, saturate(variation));
                float detail = FBMFine(p * 2.5 + float3(5.3, 1.7, 3.1));
                float wisps  = FBM(p * 5.0     + float3(17.5, 3.2, 11.1));
                float noiseVal = baseShape * (0.7 - wispW) + detail * 0.3 + wisps * wispW;

                // Directional coverage gradient: symmetric warp visible on both sides
                float2 windDir2D = normalize(_CloudDirection.xz + float2(EPSILON, EPSILON));
                float arrivalFactor = dot(normalize(shellPt.xz + float2(EPSILON, EPSILON)), windDir2D);
                float directionalBias = abs(arrivalFactor) * 0.08;
                float effectiveCoverage = saturate(coverage * (1.0 + directionalBias));

                // Preliminary mask for weighted core detail
                float prelimMask = saturate((noiseVal - (1.0 - effectiveCoverage)) * sharpness * density);
                float coreDetail = FBMFine(p * 2.5 + float3(2.1, 8.4, 4.7)) * 0.2;
                noiseVal += coreDetail * prelimMask;

                // Final coverage threshold
                float cloudMask = saturate((noiseVal - (1.0 - effectiveCoverage)) * sharpness * density);

                // Edge softness
                float edgeWidth = max(edgeSoftness * 2.0, 0.15);
                cloudMask = smoothstep(0.0, edgeWidth, cloudMask);

                return cloudMask;
            }

            // ─── VOLUMETRIC CLOUD LAYER ───────────────────────────────────────
            // Raymarches NUM_STEPS shells from shellOuter to shellInner, accumulating
            // density with Beer-Lambert transmittance. Returns (rgb, alpha).

            // NUM_STEPS_L1: 32 steps for the primary (lower-altitude) cloud layer.
            //               More steps give better depth and self-shadow quality.
            // NUM_STEPS_L2: 24 steps for the secondary (higher-altitude) layer.
            //               Higher-altitude cirrus-like clouds are typically thinner and
            //               need fewer steps, providing a small performance saving.
            #define NUM_STEPS_L1 32
            #define NUM_STEPS_L2 24

            float4 RenderLayer(
                float3 viewDir,
                float  shellRadius,
                float  cloudScale,
                float  cloudSpeed,
                float  coverage,
                float  density,
                float  sharpness,
                float  edgeSoftness,
                float  variation,
                float  layerOpacity,
                float  brightness,
                float  darkness,
                float4 cloudColor,
                float4 shadowColor,
                float3 timeColor,
                float  sunsetFactor,
                float4 sunsetCol,
                float3 dissolveOff,
                float  layerSeed,
                int    numSteps)
            {
                // Below-horizon guard
                if (viewDir.y < 0.005) return float4(0, 0, 0, 0);

                // Normalize so cloud visual size is constant regardless of shell radius
                float radiusNorm = 25000.0 / max(shellRadius, 1.0);
                float flattenFactor = 1.0 - _CloudShellFlattening * 0.08;

                // Wind offset (3D, same math as skybox shader)
                float3 windOffset = float3(_CloudDirection.x, 0.0, _CloudDirection.z)
                                  * cloudSpeed * _Time.y * cloudScale * 0.0003;

                // Shell slab: outer to inner (10% depth)
                float shellOuter = shellRadius;
                float shellInner = shellRadius * 0.90;

                // Zenith blend precomputed from view direction
                float zenithGate  = smoothstep(0.7, 0.95, viewDir.y);
                float rawBlend    = saturate(viewDir.y * _CloudZenithBlend * 2.0);
                float blendFactor = rawBlend * rawBlend * rawBlend
                                  * (rawBlend * (rawBlend * 6.0 - 15.0) + 10.0);
                blendFactor *= zenithGate;

                // HG phase function: sun/view angle
                float3 sunDir3  = normalize(_SunDirection.xyz);
                float cosTheta  = dot(viewDir, sunDir3);
                float phaseHG   = HenyeyGreenstein(cosTheta, 0.5);
                float phaseIsotropic = 1.0 / (4.0 * PI);
                // Blend: mostly forward-scattering, slight isotropic floor
                float phaseTerm = lerp(phaseIsotropic, phaseHG, 0.7) * 12.5;
                phaseTerm = saturate(phaseTerm);

                // Horizon fade
                float horizonScale = shellRadius / 25000.0;
                float fadeStart = min(0.005 * horizonScale, 0.12);
                float fadeEnd   = min(0.15  * horizonScale, 0.20);
                float horizonFade = smoothstep(fadeStart, fadeEnd, viewDir.y);

                float transmittance = 1.0;
                float3 accColor     = float3(0, 0, 0);

                for (int step = 0; step < numSteps; step++)
                {
                    float t = step / float(numSteps - 1);  // 0=outer, 1=inner

                    float radius = lerp(shellOuter, shellInner, t);

                    // Sphere-shell sample point
                    float3 spherePos = viewDir * radius;
                    spherePos.y *= flattenFactor;

                    // Flat-plane projection (eliminates zenith ring artifacts)
                    float tPlane = (shellRadius * flattenFactor) / max(viewDir.y, 0.3);
                    tPlane = min(tPlane, shellRadius * 3.0);
                    float3 flatPos = float3(viewDir.x * tPlane, 0.0, viewDir.z * tPlane)
                                   * lerp(1.0, radius / shellRadius, 0.5);

                    float3 basePos = lerp(spherePos, flatPos, blendFactor);
                    float3 noisePos = basePos * radiusNorm * cloudScale * 0.0003
                                    + windOffset + dissolveOff
                                    + float3(layerSeed * 3.7, layerSeed * 2.1, layerSeed * 1.3);

                    // Sample cloud density at this shell depth
                    float d = SampleDensity(noisePos, coverage, density,
                                            sharpness, edgeSoftness, variation,
                                            float3(0, 0, 0), 0.0);
                    d *= horizonFade;
                    if (d < 0.001) continue;

                    // Depth-based lighting: deeper into the cloud = darker (self-shadow)
                    // t=0 → top-illuminated, t=1 → shadowed base
                    float depthShadow = 1.0 - darkness * pow(t, 0.7);

                    // Silver-lining rim light on thin outer edges
                    float rimLight = 1.0 + 0.3 * pow(saturate(1.0 - d), 2.0);

                    // Base lit color from time-of-day + cloud color tint
                    float3 litColor = timeColor * cloudColor.rgb
                                    * brightness * rimLight * depthShadow;

                    // Phase function: forward scattering brightens sun-facing edges
                    litColor *= (0.4 + 0.6 * phaseTerm);

                    // Sunrise/sunset warm tint on bright top layers
                    litColor = lerp(litColor, litColor * sunsetCol.rgb * 1.3, sunsetFactor * (1.0 - t) * 0.6);

                    // Shadow-to-lit gradient (no hard floor — keeps wisps realistic)
                    float3 shadedColor = lerp(shadowColor.rgb, litColor, saturate(d * 1.2));

                    // Beer-Lambert step: absorption proportional to density
                    float sigma = d * density * 3.5 / float(numSteps);
                    float stepT = exp(-sigma);
                    float3 scatterContrib = (1.0 - stepT) * shadedColor;

                    accColor     += transmittance * scatterContrib;
                    transmittance *= stepT;

                    if (transmittance < 0.01) break;
                }

                float alpha = saturate((1.0 - transmittance) * layerOpacity);
                float3 finalColor = (alpha > 0.001)
                    ? (accColor / max(1.0 - transmittance, 0.001)) * layerOpacity
                    : float3(0, 0, 0);

                return float4(finalColor, alpha);
            }

            // ─── FRAGMENT ────────────────────────────────────────────────────

            float4 Frag(Varyings IN) : SV_Target
            {
                if (_EnableClouds < 0.5) return float4(0, 0, 0, 0);

                // ── Reconstruct world-space view direction from NDC ─────────
                float4 viewPos = mul(_InvProjectionMatrix, float4(IN.ndcPos, 1.0, 1.0));
                float3 viewDir = normalize(mul((float3x3)_InvViewMatrix, viewPos.xyz / viewPos.w));

                // Above-horizon guard (clouds never appear underground)
                if (viewDir.y < 0.005) return float4(0, 0, 0, 0);

                // ── Height mask (same logic as skybox) ──────────────────────
                float horizonPush = _CloudHorizonCoverage * max(saturate(_CloudCoverage * 2.0), 0.15);
                float heightMask  = smoothstep(-horizonPush, _CloudHeight + 0.3, viewDir.y);
                if (heightMask < 0.001) return float4(0, 0, 0, 0);

                // ── Time-of-day blend factors ────────────────────────────────
                float dayFactor = smoothstep(0.15, 0.35, _TimeOfDay)
                                * (1.0 - smoothstep(0.65, 0.85, _TimeOfDay));
                float sunriseFactor = smoothstep(0.15, 0.25, _TimeOfDay)
                                    * (1.0 - smoothstep(0.25, 0.40, _TimeOfDay));
                float sunsetFactor  = smoothstep(0.60, 0.75, _TimeOfDay)
                                    * (1.0 - smoothstep(0.75, 0.88, _TimeOfDay));
                float transitionFactor = saturate(sunriseFactor + sunsetFactor);

                // Layer 1 base time-of-day color
                float3 timeColor1 = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                timeColor1 = lerp(timeColor1, _CloudSunsetColor.rgb, transitionFactor * 0.8);

                // ── Layer 1 ─────────────────────────────────────────────────
                float4 layer1 = RenderLayer(
                    viewDir,
                    _CloudShellRadius,
                    _CloudScale,
                    _CloudSpeed,
                    _CloudCoverage,
                    _CloudDensity,
                    _CloudSharpness,
                    _CloudEdgeSoftness,
                    _CloudVariation,
                    _CloudAlpha * heightMask,
                    _CloudBrightness,
                    _CloudDarkness,
                    _CloudColor,
                    _CloudShadowColor,
                    timeColor1,
                    transitionFactor,
                    _CloudSunsetColor,
                    _CloudDissolveOffset.xyz,
                    0.0,       // layer seed
                    NUM_STEPS_L1);

                // ── Layer 2 ─────────────────────────────────────────────────
                float4 layer2 = float4(0, 0, 0, 0);
                if (_Cloud2Coverage > 0.001)
                {
                    float heightMask2 = smoothstep(
                        -horizonPush * 0.5, _CloudLayer2Height + 0.3, viewDir.y);

                    if (heightMask2 > 0.001)
                    {
                        // Same time-of-day color for Layer 2 (slightly cooler tint via _Cloud2Color)
                        float3 timeColor2 = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                        timeColor2 = lerp(timeColor2, _CloudSunsetColor.rgb, transitionFactor * 0.8);

                        // Layer 2 dissolve offset decays at half rate (higher altitude = slower transition)
                        float3 dissolveOff2 = _CloudDissolveOffset.xyz * 0.5;

                        layer2 = RenderLayer(
                            viewDir,
                            _Cloud2ShellRadius,
                            _Cloud2Scale,
                            _Cloud2Speed,
                            _Cloud2Coverage,
                            _Cloud2Density,
                            _Cloud2Sharpness,
                            _CloudEdgeSoftness * 1.5,
                            0.5,
                            _Cloud2Opacity * heightMask2,
                            _Cloud2Brightness,
                            _Cloud2Darkness,
                            _Cloud2Color,
                            _Cloud2ShadowColor,
                            timeColor2,
                            transitionFactor,
                            _CloudSunsetColor,
                            dissolveOff2,
                            1.0,       // different layer seed = different cloud pattern
                            NUM_STEPS_L2);
                    }
                }

                // ── Composite Layer 2 over Layer 1 (Porter-Duff "over") ─────
                float a1 = layer1.a;
                float a2 = layer2.a;

                if (a1 < 0.001 && a2 < 0.001) return float4(0, 0, 0, 0);

                float combinedAlpha = a1 + a2 * (1.0 - a1);
                float3 combinedColor = (a1 > 0.001 && a2 > 0.001)
                    ? (layer1.rgb * a1 * (1.0 - a2) + layer2.rgb * a2) / max(combinedAlpha, 0.001)
                    : (a2 > 0.001 ? layer2.rgb : layer1.rgb);

                return float4(combinedColor, saturate(combinedAlpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
