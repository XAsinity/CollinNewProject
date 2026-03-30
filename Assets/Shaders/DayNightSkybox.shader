Shader "Custom/DayNightSkybox"
{
    Properties
    {
        [Header(HDR Panorama)]
        _MainTex ("HDR Panorama", 2D) = "black" {}
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0.0, 2.0)) = 0.5
        _Rotation ("Rotation (Degrees)", Range(0, 360)) = 0

        [Header(UV Correction)]
        _PoleClampMin ("Pole Clamp Min", Range(0.0, 0.15)) = 0.04
        _PoleClampMax ("Pole Clamp Max", Range(0.85, 1.0)) = 0.96

        [Header(Time Of Day)]
        _TimeOfDay ("Time Of Day", Range(0, 1)) = 0.5

        [Header(Daytime Atmosphere)]
        _DaySkyColorTop ("Day Sky Top Color", Color) = (0.15, 0.5, 0.95, 1)
        _DaySkyColorHorizon ("Day Sky Horizon Color", Color) = (0.6, 0.8, 1.0, 1)
        _DayAtmosphereStrength ("Day Atmosphere Strength", Range(0, 2)) = 1.0

        [Header(Sunset and Sunrise)]
        _SunsetColor ("Sunset Color", Color) = (1.0, 0.4, 0.1, 1)
        _SunriseColor ("Sunrise Color", Color) = (1.0, 0.6, 0.3, 1)
        _SunsetSpread ("Sunset Spread", Range(0.1, 1.0)) = 0.4
        _HorizonGlowStrength ("Horizon Glow Strength", Range(0, 2)) = 1.0

        [Header(Sun Disc)]
        _EnableSun ("Enable Sun Disc", Float) = 1
        _SunDirection ("Sun Direction", Vector) = (0, 1, 0, 0)
        _SunColor ("Sun Color", Color) = (1.0, 0.95, 0.8, 1)
        _SunSize ("Sun Disc Size", Range(0.001, 0.1)) = 0.03
        _SunGlowSize ("Sun Glow Size", Range(0.01, 0.5)) = 0.15
        _SunGlowStrength ("Sun Glow Strength", Range(0, 2)) = 0.5

        [Header(Moon Disc)]
        _EnableMoon ("Enable Moon Disc", Float) = 1
        _MoonDirection ("Moon Direction", Vector) = (0, -1, 0, 0)
        _MoonColor ("Moon Color", Color) = (0.8, 0.85, 0.95, 1)
        _MoonSize ("Moon Disc Size", Range(0.001, 0.08)) = 0.025
        _MoonGlowSize ("Moon Glow Size", Range(0.01, 0.3)) = 0.1
        _MoonGlowStrength ("Moon Glow Strength", Range(0, 1.5)) = 0.3

        [Header(Procedural Clouds)]
        _EnableClouds ("Enable Clouds", Float) = 1
        _CloudScale ("Cloud Scale", Range(1, 20)) = 5.0
        _CloudSpeed ("Cloud Speed", Range(0, 2)) = 0.3
        _CloudDirection ("Cloud Wind Direction", Vector) = (1, 0, 0.5, 0)
        _CloudDensity ("Cloud Density", Range(0, 2)) = 1.0
        _CloudSharpness ("Cloud Sharpness", Range(0.1, 5.0)) = 1.5
        _CloudHeight ("Cloud Height Bias", Range(-0.5, 0.8)) = 0.2
        _CloudDayColor ("Cloud Day Color", Color) = (0.95, 0.95, 0.95, 1)
        _CloudNightColor ("Cloud Night Color", Color) = (0.05, 0.05, 0.1, 1)
        _CloudSunsetColor ("Cloud Sunset Color", Color) = (1.0, 0.5, 0.2, 1)
        _CloudAlpha ("Cloud Opacity", Range(0, 1)) = 0.8
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.5
        _CloudBrightness ("Cloud Brightness", Range(0, 2)) = 1.0
        _CloudDarkness ("Cloud Darkness (Shadow Intensity)", Range(0, 1)) = 0.5
        _CloudColor ("Cloud Color Tint", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.35, 0.35, 0.40, 1)
        _CloudHorizonCoverage ("Cloud Horizon Coverage", Range(0, 1)) = 0.8
        _CloudEdgeSoftness ("Cloud Edge Softness", Range(0.0, 0.5)) = 0.35
        _CloudVariation ("Cloud Variation/Turbulence", Range(0, 1)) = 0.5

        [Header(Procedural Stars)]
        _EnableStars ("Enable Procedural Stars", Float) = 1
        _StarDensity ("Star Density", Range(0, 500)) = 200
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.2
        _StarSize ("Star Size", Range(0.0, 0.02)) = 0.005

        [Header(Star Diversity)]
        _EnableStarColors ("Enable Colored Stars", Float) = 1
        _RedDwarfChance ("Red Dwarf Frequency", Range(0, 0.3)) = 0.08
        _BlueSuperChance ("Blue Supergiant Frequency", Range(0, 0.3)) = 0.05
        _YellowStarChance ("Yellow Star Frequency", Range(0, 0.3)) = 0.1
        _OrangeGiantChance ("Orange Giant Frequency", Range(0, 0.3)) = 0.06
        _RedDwarfColor ("Red Dwarf Color", Color) = (1.0, 0.3, 0.15, 1)
        _BlueSupergiantColor ("Blue Supergiant Color", Color) = (0.4, 0.6, 1.0, 1)
        _YellowStarColor ("Yellow Star Color", Color) = (1.0, 0.95, 0.6, 1)
        _OrangeGiantColor ("Orange Giant Color", Color) = (1.0, 0.6, 0.2, 1)
        _ColoredStarBoost ("Colored Star Brightness Boost", Range(1, 4)) = 1.8

        [Header(Star Flicker)]
        _EnableFlicker ("Enable Star Flicker", Float) = 1
        _FlickerSpeed ("Flicker Speed", Range(0.1, 5.0)) = 1.5
        _FlickerIntensity ("Flicker Intensity", Range(0, 0.8)) = 0.3
        _DimStarChance ("Dim Flickering Star Frequency", Range(0, 0.5)) = 0.25
        _DimStarMin ("Dim Star Minimum Brightness", Range(0, 0.5)) = 0.05
        _DimStarMax ("Dim Star Maximum Brightness", Range(0.1, 1.0)) = 0.4

        [Header(Nebula)]
        _EnableNebula ("Enable Nebula", Float) = 1
        _NebulaColor1 ("Nebula Color 1", Color) = (0.15, 0.05, 0.3, 1)
        _NebulaColor2 ("Nebula Color 2", Color) = (0.05, 0.1, 0.4, 1)
        _NebulaScale ("Nebula Scale", Range(0.5, 8.0)) = 2.5
        _NebulaStrength ("Nebula Strength", Range(0, 1)) = 0.3
        _NebulaDensity ("Nebula Density", Range(0.1, 5.0)) = 1.5
        _NebulaOffset ("Nebula Position Offset", Vector) = (0, 0, 0, 0)

        [Header(Aurora)]
        _EnableAurora ("Enable Aurora", Float) = 1
        _AuroraColor1 ("Aurora Color 1", Color) = (0.1, 0.8, 0.6, 1)
        _AuroraColor2 ("Aurora Color 2", Color) = (0.3, 0.2, 0.9, 1)
        _AuroraBands ("Aurora Band Count", Range(1, 15)) = 5
        _AuroraStrength ("Aurora Strength", Range(0, 1)) = 0.25
        _AuroraHeight ("Aurora Height Position", Range(-1, 1)) = 0.3
        _AuroraSpread ("Aurora Spread", Range(0.05, 0.8)) = 0.25
        _AuroraSpeed ("Aurora Animation Speed", Range(0, 2)) = 0.3

        [Header(Galactic Dust)]
        _EnableDust ("Enable Galactic Dust", Float) = 1
        _DustColor ("Dust Color", Color) = (0.6, 0.35, 0.15, 1)
        _DustScale ("Dust Scale", Range(1, 30)) = 12
        _DustStrength ("Dust Strength", Range(0, 0.5)) = 0.1
        _DustThreshold ("Dust Threshold", Range(0.5, 0.95)) = 0.78
        _DustSpread ("Dust Band Width", Range(0.05, 1.0)) = 0.3
        _DustHeight ("Dust Band Height", Range(-1, 1)) = 0.0

        [Header(Atmosphere)]
        _VignetteColor ("Vignette Color", Color) = (0.02, 0.01, 0.05, 1)
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.15

        [Header(Horizon Haze)]
        _HorizonHazeStrength ("Horizon Haze Strength", Range(0, 1)) = 0.3
        _HorizonHazeHeight ("Horizon Haze Height", Range(0.01, 1.0)) = 0.15
        _HorizonHazeFalloff ("Horizon Haze Falloff", Range(0.5, 8.0)) = 3.0

        [Header(Cloud Layer 2)]
        _CloudLayer2Coverage ("Cloud Layer 2 Coverage", Range(0, 1)) = 0.0
        _CloudLayer2Scale ("Cloud Layer 2 Scale", Range(1, 20)) = 8.0
        _CloudLayer2Speed ("Cloud Layer 2 Speed", Range(0, 2)) = 0.5
        _CloudLayer2Opacity ("Cloud Layer 2 Opacity", Range(0, 1)) = 0.3
        _CloudLayer2Height ("Cloud Layer 2 Height Bias", Range(-0.5, 0.8)) = 0.1

        [Header(Cloud Layer 2 Weather Driven)]
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

        [Header(Storm Transition)]
        _CloudDissolveOffset ("Cloud Dissolve Offset", Vector) = (0, 0, 0, 0)

        [Header(Cloud Shell Altitude)]
        _CloudShellRadius ("Cloud Shell Radius", Range(1000, 1000000)) = 25000.0
        _Cloud2ShellRadius ("Cloud2 Shell Radius", Range(1000, 1000000)) = 35000.0
        _CloudShellFlattening ("Cloud Shell Flattening", Range(0, 1)) = 0.0
        _CloudZenithBlend ("Cloud Zenith Blend", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // ─── PROPERTIES ──────────────────────────────────────────

            sampler2D _MainTex;
            float4 _Tint;
            float _Exposure;
            float _Rotation;
            float _PoleClampMin;
            float _PoleClampMax;

            float _TimeOfDay;

            float4 _DaySkyColorTop;
            float4 _DaySkyColorHorizon;
            float _DayAtmosphereStrength;

            float4 _SunsetColor;
            float4 _SunriseColor;
            float _SunsetSpread;
            float _HorizonGlowStrength;

            float _EnableSun;
            float4 _SunDirection;
            float4 _SunColor;
            float _SunSize;
            float _SunGlowSize;
            float _SunGlowStrength;

            float _EnableMoon;
            float4 _MoonDirection;
            float4 _MoonColor;
            float _MoonSize;
            float _MoonGlowSize;
            float _MoonGlowStrength;

            float _EnableClouds;
            float _CloudScale;
            float _CloudSpeed;
            float4 _CloudDirection;
            float _CloudDensity;
            float _CloudSharpness;
            float _CloudHeight;
            float4 _CloudDayColor;
            float4 _CloudNightColor;
            float4 _CloudSunsetColor;
            float _CloudAlpha;
            float _CloudCoverage;
            float _CloudBrightness;
            float _CloudDarkness;
            float4 _CloudColor;
            float4 _CloudShadowColor;
            float _CloudHorizonCoverage;
            float _CloudEdgeSoftness;
            float _CloudVariation;

            float _EnableStars;
            float _StarDensity;
            float _StarBrightness;
            float _StarSize;

            float _EnableStarColors;
            float _RedDwarfChance;
            float _BlueSuperChance;
            float _YellowStarChance;
            float _OrangeGiantChance;
            float4 _RedDwarfColor;
            float4 _BlueSupergiantColor;
            float4 _YellowStarColor;
            float4 _OrangeGiantColor;
            float _ColoredStarBoost;

            float _EnableFlicker;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _DimStarChance;
            float _DimStarMin;
            float _DimStarMax;

            float _EnableNebula;
            float4 _NebulaColor1;
            float4 _NebulaColor2;
            float _NebulaScale;
            float _NebulaStrength;
            float _NebulaDensity;
            float4 _NebulaOffset;

            float _EnableAurora;
            float4 _AuroraColor1;
            float4 _AuroraColor2;
            float _AuroraBands;
            float _AuroraStrength;
            float _AuroraHeight;
            float _AuroraSpread;
            float _AuroraSpeed;

            float _EnableDust;
            float4 _DustColor;
            float _DustScale;
            float _DustStrength;
            float _DustThreshold;
            float _DustSpread;
            float _DustHeight;

            float4 _VignetteColor;
            float _VignetteStrength;

            float _HorizonHazeStrength;
            float _HorizonHazeHeight;
            float _HorizonHazeFalloff;

            float _CloudLayer2Coverage;
            float _CloudLayer2Scale;
            float _CloudLayer2Speed;
            float _CloudLayer2Opacity;
            float _CloudLayer2Height;

            float _Cloud2Coverage;
            float _Cloud2Scale;
            float _Cloud2Speed;
            float _Cloud2Density;
            float _Cloud2Sharpness;
            float _Cloud2Brightness;
            float _Cloud2Darkness;
            float4 _Cloud2Color;
            float4 _Cloud2ShadowColor;
            float _Cloud2Opacity;

            float4 _CloudDissolveOffset;

            float _CloudShellRadius;
            float _Cloud2ShellRadius;
            float _CloudShellFlattening;
            float _CloudZenithBlend;

            // ─── STRUCTS ─────────────────────────────────────────────

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldDir : TEXCOORD0;
            };

            // ─── VERTEX ──────────────────────────────────────────────

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldDir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            // ─── NOISE FUNCTIONS ─────────────────────────────────────

            float Hash(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float HashSingle(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float HashStar(float3 cell, float seed)
            {
                return frac(sin(dot(cell, float3(12.9898, 78.233, 45.164)) + seed) * 43758.5453);
            }

            float Noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n = p.x + p.y * 57.0 + 113.0 * p.z;

                return lerp(
                    lerp(
                        lerp(HashSingle(n + 0.0),   HashSingle(n + 1.0),   f.x),
                        lerp(HashSingle(n + 57.0),  HashSingle(n + 58.0),  f.x), f.y),
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

            // ─── 2D VALUE NOISE (used for cloud color variation) ─────────

            // Hash for 2D value noise
            float Hash2D(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Smooth value noise with quintic interpolation — Ken Perlin's smootherstep
            // polynomial (6t⁵ - 15t⁴ + 10t³) eliminates the grid artifacts that cubic
            // smoothstep (3t² - 2t³) produces at cell boundaries.
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Quintic: f³(f(6f - 15) + 10)
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
                float a = Hash2D(i);
                float b = Hash2D(i + float2(1, 0));
                float c = Hash2D(i + float2(0, 1));
                float d = Hash2D(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ─── 3D FBM CLOUD LAYER ──────────────────────────────────────
            //
            // Samples 3D noise on a sphere-shell blended with a flat-plane
            // projection.  The sphere-shell gives genuine 3D parallax and depth;
            // the flat-plane removes the concentric ring artifacts that appear near
            // the zenith.  _CloudZenithBlend controls how far up the sky the blend
            // kicks in (0 = pure sphere-shell everywhere, 1 = flat-plane bias).
            //
            // Directional coverage gradient: clouds grow in from the wind direction
            // rather than fizzling in as random pixels everywhere at once.
            float CalculateCloudLayer(float3 worldDir, float shellRadius, float cloudScale,
                                      float cloudSpeed, float4 cloudDir, float coverage,
                                      float density, float sharpness, float time,
                                      float3 dissolveOff, float edgeSoftness,
                                      float variation, float layerSeed, float zenithBlend)
            {
                float3 ndir = normalize(worldDir);

                // Below-horizon rays get no clouds
                if (ndir.y < 0.005) return 0.0;

                // Physics-based horizon scale — reference radius 25000 so that
                // cloud scale feels consistent across different shell radii.
                float horizonScale = shellRadius / 25000.0;

                // ── WIND OFFSET (3D) ────────────────────────────────────────
                float3 windOffset = float3(cloudDir.x, 0.0, cloudDir.z) * cloudSpeed * time;

                // ── SPHERE-SHELL POSITION ───────────────────────────────────
                float flattenFactor = 1.0 - _CloudShellFlattening * 0.8; // 0.8 max squash to avoid degeneracy
                float3 spherePos = ndir * shellRadius;
                spherePos.y *= flattenFactor;

                // ── FLAT-PLANE PROJECTION ───────────────────────────────────
                // Project view ray onto horizontal plane at height shellRadius.
                // This distributes noise evenly and eliminates zenith ring artifacts.
                float t = (shellRadius * flattenFactor) / max(ndir.y, 0.3);
                t = min(t, shellRadius * 3.0);  // never stretch beyond 3x shell radius
                float3 flatPos = float3(ndir.x * t, 0.0, ndir.z * t);

                // ── BLEND SPHERE AND FLAT-PLANE ─────────────────────────────
                // At low elevations (horizon) use mostly sphere-shell for depth.
                // At high elevations (near zenith) blend toward flat-plane to
                // eliminate ring artifacts that appear directly overhead.
                float zenithGate = smoothstep(0.7, 0.95, ndir.y);  // only blend near the pole
                float rawBlend = saturate(ndir.y * zenithBlend * 2.0);
                // Perlin smootherstep for tighter transition
                float blendFactor = rawBlend * rawBlend * rawBlend * (rawBlend * (rawBlend * 6.0 - 15.0) + 10.0);
                blendFactor *= zenithGate;  // restrict to near-pole region
                float3 basePos = lerp(spherePos, flatPos, blendFactor);

                // Normalize noise coordinates so cloud visual size stays constant
                // regardless of shell radius. Only the dome geometry changes, not the
                // apparent cloud scale.
                float radiusNorm = 25000.0 / max(shellRadius, 1.0);

                // Scale into noise-frequency space
                float3 samplePos = basePos * radiusNorm * cloudScale * 0.0003;

                // Apply wind and dissolve offsets.
                samplePos += windOffset * cloudScale * 0.0003;
                samplePos += dissolveOff;

                // Layer seed separation — ensures layer 2 samples a different region
                samplePos += float3(layerSeed * 3.7, layerSeed * 2.1, layerSeed * 1.3);

                // ── INNER SHELL PARALLAX (97% radius) ──────────────────────
                float3 innerSpherePos = ndir * (shellRadius * 0.97);
                innerSpherePos.y *= flattenFactor;
                float3 innerFlatPos = float3(ndir.x * (t * 0.97), 0.0, ndir.z * (t * 0.97));
                float3 innerBasePos = lerp(innerSpherePos, innerFlatPos, blendFactor);
                float3 sampleInner = innerBasePos * radiusNorm * cloudScale * 0.0003
                                   + windOffset * cloudScale * 0.0003
                                   + dissolveOff
                                   + float3(layerSeed * 3.7, layerSeed * 2.1, layerSeed * 1.3);

                // ── DEEP SHELL PARALLAX (94% radius) — third sample for more volumetric depth
                float3 deepSpherePos = ndir * (shellRadius * 0.94);
                deepSpherePos.y *= flattenFactor;
                float3 deepFlatPos = float3(ndir.x * (t * 0.94), 0.0, ndir.z * (t * 0.94));
                float3 deepBasePos = lerp(deepSpherePos, deepFlatPos, blendFactor);
                float3 sampleDeep = deepBasePos * radiusNorm * cloudScale * 0.0003
                                  + windOffset * cloudScale * 0.0003
                                  + dissolveOff
                                  + float3(layerSeed * 3.7, layerSeed * 2.1, layerSeed * 1.3);

                // ── BASE SHAPE — blend outer, middle, and deep shell for volumetric depth
                float baseShape = FBM(samplePos) * 0.35 + FBM(sampleInner) * 0.35 + FBM(sampleDeep) * 0.30;

                // ── MID-FREQUENCY DETAIL ────────────────────────────────────
                float3 detailPos = samplePos * 2.5 + float3(5.3, 1.7, 3.1);
                float detail = FBMFine(detailPos);

                // ── HIGH-FREQUENCY WISPS ────────────────────────────────────
                float wispW = lerp(0.05, 0.15, saturate(variation));
                float3 wispPos = samplePos * 5.0 + float3(17.5, 3.2, 11.1);
                float wisps = FBM(wispPos);

                // Combine octaves
                float noiseVal = baseShape * (0.7 - wispW) + detail * 0.3 + wisps * wispW;

                // ── DIRECTIONAL COVERAGE GRADIENT ──────────────────────────
                // Symmetric directional bias: both windward and leeward sides receive
                // equal influence so the warp is visible across the whole sky.
                float2 windDir2D = normalize(cloudDir.xz + float2(0.0001, 0.0001));
                float arrivalFactor = dot(ndir.xz, windDir2D); // -1 (leeward) to +1 (windward)
                // Scale down the directional bias so it's a gentle effect, not a hard gate
                float directionalBias = abs(arrivalFactor) * 0.08; // symmetric, reduced strength
                float effectiveCoverage = saturate(coverage * (1.0 + directionalBias));

                // ── PRELIMINARY CLOUD MASK for core detail weighting ────────
                float prelimMask = saturate((noiseVal - (1.0 - effectiveCoverage)) * sharpness * density);

                // ── CORE DETAIL (fine noise weighted by preliminary mask) ───
                float3 corePos = samplePos * 2.5 + float3(2.1, 8.4, 4.7);
                float coreDetail = FBMFine(corePos) * 0.2;
                noiseVal += coreDetail * prelimMask;

                // ── FINAL COVERAGE THRESHOLD ────────────────────────────────
                float cloudMask = saturate((noiseVal - (1.0 - effectiveCoverage)) * sharpness * density);

                // Edge softness
                float edgeWidth = max(edgeSoftness * 2.0, 0.15);
                cloudMask = smoothstep(0.0, edgeWidth, cloudMask);

                // Physics-based horizon fade — clouds thin out close to the horizon.
                // Clamp both values so the range stays valid regardless of shell radius.
                float fadeStart = min(0.005 * horizonScale, 0.12);
                float fadeEnd   = min(0.15  * horizonScale, 0.20);
                float horizonFade = smoothstep(fadeStart, fadeEnd, ndir.y);
                cloudMask *= horizonFade;

                return cloudMask;
            }

            // ─── ROTATION ────────────────────────────────────────────

            float3 RotateAroundY(float3 dir, float degrees)
            {
                float rad = degrees * 0.0174533;
                float s = sin(rad);
                float c = cos(rad);
                return float3(
                    dir.x * c - dir.z * s,
                    dir.y,
                    dir.x * s + dir.z * c
                );
            }

            // ─── EQUIRECTANGULAR UV ──────────────────────────────────

            float2 DirToEquirectUV(float3 dir)
            {
                dir = normalize(dir);
                float longitude = atan2(dir.z, dir.x);
                float latitude = asin(clamp(dir.y, -1.0, 1.0));

                float2 uv;
                uv.x = (longitude / (2.0 * UNITY_PI)) + 0.5;
                uv.y = (latitude / UNITY_PI) + 0.5;
                uv.y = saturate(lerp(_PoleClampMin, _PoleClampMax, uv.y));

                return uv;
            }

            // ─── PROCEDURAL STARS (with color + flicker) ─────────────

            void ProceduralStars(float3 dir, float density, float size,
                                 out float starIntensity, out float3 starColor)
            {
                starIntensity = 0.0;
                starColor = float3(1, 1, 1);

                float3 cell = floor(dir * density);
                float3 localPos = frac(dir * density) - 0.5;

                float3 starOffset = (float3(Hash(cell), Hash(cell + 1.0), Hash(cell + 2.0)) - 0.5) * 0.8;
                float dist = length(localPos - starOffset);

                float star = 1.0 - smoothstep(0.0, size * density, dist);
                if (star < 0.001) return;

                float baseBrightness = Hash(cell + 5.0);
                baseBrightness = pow(baseBrightness, 3.0);

                float typeRoll = Hash(cell + 10.0);
                float colorBoost = 1.0;

                if (_EnableStarColors > 0.5)
                {
                    float redMax    = _RedDwarfChance;
                    float blueMax   = redMax  + _BlueSuperChance;
                    float yellowMax = blueMax + _YellowStarChance;
                    float orangeMax = yellowMax + _OrangeGiantChance;

                    if (typeRoll < redMax)
                    {
                        starColor = _RedDwarfColor.rgb;
                        colorBoost = _ColoredStarBoost * 0.7;
                    }
                    else if (typeRoll < blueMax)
                    {
                        starColor = _BlueSupergiantColor.rgb;
                        colorBoost = _ColoredStarBoost * 1.5;
                        star = 1.0 - smoothstep(0.0, size * density * 1.4, dist);
                    }
                    else if (typeRoll < yellowMax)
                    {
                        starColor = _YellowStarColor.rgb;
                        colorBoost = _ColoredStarBoost;
                    }
                    else if (typeRoll < orangeMax)
                    {
                        starColor = _OrangeGiantColor.rgb;
                        colorBoost = _ColoredStarBoost * 0.9;
                        star = 1.0 - smoothstep(0.0, size * density * 1.2, dist);
                    }
                }

                float flickerMult = 1.0;

                if (_EnableFlicker > 0.5)
                {
                    float flickerPhase = HashStar(cell, 0.0) * 6.28318;
                    float flickerFreq = HashStar(cell, 3.0) * 2.0 + 0.5;

                    float flicker1 = sin(_Time.y * _FlickerSpeed * flickerFreq + flickerPhase);
                    float flicker2 = sin(_Time.y * _FlickerSpeed * flickerFreq * 1.7 + flickerPhase * 2.3) * 0.5;
                    float flicker3 = sin(_Time.y * _FlickerSpeed * flickerFreq * 0.3 + flickerPhase * 0.7) * 0.3;

                    float combinedFlicker = (flicker1 + flicker2 + flicker3) / 1.8;

                    float dimRoll = Hash(cell + 20.0);
                    if (dimRoll < _DimStarChance)
                    {
                        float dimCycle = sin(_Time.y * _FlickerSpeed * flickerFreq * 0.4 + flickerPhase);
                        float dimFactor = lerp(_DimStarMin, _DimStarMax, dimCycle * 0.5 + 0.5);
                        flickerMult = dimFactor;
                    }
                    else
                    {
                        flickerMult = 1.0 - combinedFlicker * _FlickerIntensity;
                        flickerMult = clamp(flickerMult, 0.3, 1.5);
                    }
                }

                starIntensity = star * baseBrightness * flickerMult * colorBoost;
            }

            // ─── NEBULA ──────────────────────────────────────────────

            float3 CalculateNebula(float3 dir)
            {
                float3 samplePos = dir * _NebulaScale + _NebulaOffset.xyz;

                float nebula1 = FBM(samplePos + 10.0);
                float nebula2 = FBM(samplePos * 1.5 + 30.0);
                float nebula3 = FBM(samplePos * 0.7 + 50.0);

                nebula1 = pow(saturate(nebula1), _NebulaDensity);
                nebula2 = pow(saturate(nebula2), _NebulaDensity * 1.3);
                nebula3 = pow(saturate(nebula3), _NebulaDensity * 0.8);

                float3 color1 = _NebulaColor1.rgb * nebula1;
                float3 color2 = _NebulaColor2.rgb * nebula2;
                float3 color3 = lerp(_NebulaColor1.rgb, _NebulaColor2.rgb, 0.5) * nebula3 * 0.5;

                return (color1 + color2 + color3) * _NebulaStrength;
            }

            // ─── AURORA ──────────────────────────────────────────────

            float3 CalculateAurora(float3 dir)
            {
                float heightDist = abs(dir.y - _AuroraHeight);
                float heightMask = 1.0 - smoothstep(0.0, _AuroraSpread, heightDist);

                if (heightMask < 0.001) return float3(0, 0, 0);

                float noiseWarp = FBM(dir * 3.0 + float3(0, _Time.y * _AuroraSpeed, 0)) * 3.0;
                float wave = sin(dir.x * _AuroraBands * 6.28318 + noiseWarp + _Time.y * _AuroraSpeed * 2.0);
                float wave2 = sin(dir.z * _AuroraBands * 4.5 + noiseWarp * 1.3 + _Time.y * _AuroraSpeed * 1.5);

                float aurora = abs(wave * 0.6 + wave2 * 0.4);
                aurora = pow(aurora, 8.0);

                float shimmer = FBM(dir * 8.0 + _Time.y * _AuroraSpeed * 0.5) * 0.5 + 0.5;

                float colorBlend = FBM(dir * 2.0 + 20.0);
                float3 auroraColor = lerp(_AuroraColor1.rgb, _AuroraColor2.rgb, colorBlend);

                return auroraColor * aurora * heightMask * shimmer * _AuroraStrength;
            }

            // ─── GALACTIC DUST ───────────────────────────────────────

            float3 CalculateDust(float3 dir)
            {
                float heightDist = abs(dir.y - _DustHeight);
                float bandMask = 1.0 - smoothstep(0.0, _DustSpread, heightDist);

                if (bandMask < 0.001) return float3(0, 0, 0);

                float dust = FBMFine(dir * _DustScale + 100.0);
                dust = smoothstep(_DustThreshold, 1.0, dust);

                float colorVar = Noise3D(dir * _DustScale * 0.5 + 200.0);
                float3 dustCol = lerp(_DustColor.rgb, _DustColor.rgb * 1.5, colorVar);

                return dustCol * dust * bandMask * _DustStrength;
            }

            // ─── DAYTIME ATMOSPHERE ──────────────────────────────────

            float3 CalculateDayAtmosphere(float3 dir)
            {
                // Gradient from horizon (y=0) to zenith (y=1)
                float horizonBlend = saturate(dir.y * 2.0);
                horizonBlend = pow(horizonBlend, 0.5);
                float3 skyColor = lerp(_DaySkyColorHorizon.rgb, _DaySkyColorTop.rgb, horizonBlend);
                return skyColor * _DayAtmosphereStrength;
            }

            // ─── PROCEDURAL CLOUDS ───────────────────────────────────

            float3 CalculateClouds(float3 dir, float sunsetFactor, float dayFactor, out float cloudAlpha)
            {
                cloudAlpha = 0.0;

                // Height mask — controls which sky directions show clouds.
                // A minimum floor of 0.15 on the coverage factor ensures that even at
                // low coverage values, horizonPush is large enough for clouds to render
                // without being altitude-restricted to invisibility.
                float horizonPush = _CloudHorizonCoverage * max(saturate(_CloudCoverage * 2.0), 0.15);
                float heightMask = smoothstep(-horizonPush, _CloudHeight + 0.3, dir.y);
                if (heightMask < 0.001) return float3(0, 0, 0);

                // ── Layer 1 — sphere-shell 3D FBM (primary cloud layer)
                // _CloudDissolveOffset.xyz is a 3D directional bias: when a storm clears,
                // clouds roll toward the horizon in that direction rather than fading in place.
                float3 dissolveOff1 = _CloudDissolveOffset.xyz;
                float density = CalculateCloudLayer(
                    dir, _CloudShellRadius, _CloudScale, _CloudSpeed,
                    _CloudDirection, _CloudCoverage, _CloudDensity,
                    _CloudSharpness, _Time.y, dissolveOff1,
                    _CloudEdgeSoftness, _CloudVariation, 0.0, _CloudZenithBlend);
                density *= heightMask;

                // ── Cloud Layer 2 — high-altitude weather-driven clouds (_Cloud2* properties)
                float density2 = 0.0;
                float3 cloudColor2 = float3(0, 0, 0);
                float alpha2 = 0.0;

                if (_Cloud2Coverage > 0.001)
                {
                    float heightMask2 = smoothstep(-horizonPush * 0.5, _CloudLayer2Height + 0.3, dir.y);
                    if (heightMask2 > 0.001)
                    {
                        // Higher shell radius = more distant appearance; dissolve bias halved
                        // so upper layer transitions more slowly than lower layer
                        float3 dissolveOff2 = _CloudDissolveOffset.xyz * 0.5;
                        density2 = CalculateCloudLayer(
                            dir, _Cloud2ShellRadius, _Cloud2Scale, _Cloud2Speed,
                            _CloudDirection, _Cloud2Coverage, _Cloud2Density,
                            _Cloud2Sharpness, _Time.y, dissolveOff2,
                            _CloudEdgeSoftness * 1.5, 0.5, 1.0, _CloudZenithBlend);
                        density2 *= heightMask2;

                        // Layer 2 color — distinct brightness/darkness conveys altitude and mass
                        float3 timeColor2 = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                        timeColor2 = lerp(timeColor2, _CloudSunsetColor.rgb, sunsetFactor * 0.8);
                        float3 tintedColor2 = timeColor2 * _Cloud2Color.rgb;
                        float3 litColor2 = tintedColor2 * _Cloud2Brightness;
                        float3 edgeBright2 = litColor2 * lerp(1.5, 1.0, density2);
                        float selfShadow2 = 1.0 - _Cloud2Darkness * density2 * 0.7;
                        float3 shadowBlend2 = lerp(_Cloud2ShadowColor.rgb, edgeBright2, saturate(density2 * 0.8 + 0.2));
                        cloudColor2 = shadowBlend2 * selfShadow2;
                        // Subtle low-frequency color variation using flat-plane projection
                        float t_color2 = _Cloud2ShellRadius / max(abs(normalize(dir).y), 0.02);
                        float3 colorPos2 = float3(normalize(dir).x * t_color2, 0.0, normalize(dir).z * t_color2)
                                         * _Cloud2Scale * 0.00005
                                         + float3(_CloudDirection.x, 0.0, _CloudDirection.z) * _Cloud2Speed * _Time.y;
                        float colorVar2 = Noise3D(colorPos2 * 0.4);
                        float colorVarWeight2 = lerp(0.03, 0.01, saturate(density2 - 0.5));
                        cloudColor2 *= lerp(1.0 - colorVarWeight2, 1.0 + colorVarWeight2, colorVar2);
                        // Power curve: edges wispy, cores opaque
                        alpha2 = pow(density2 * _Cloud2Opacity, 0.7);
                    }
                }

                // Early exit if neither layer is visible
                if (density < 0.001 && alpha2 < 0.001)
                    return float3(0, 0, 0);

                // ── Layer 1 color with volumetric self-shadowing
                float3 timeColor = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                timeColor = lerp(timeColor, _CloudSunsetColor.rgb, sunsetFactor * 0.8);
                float3 tintedColor = timeColor * _CloudColor.rgb;

                // Volumetric look: bright tops, dark bases
                // density close to 1 = deep inside cloud = darker (shadow)
                // density close to 0 = thin edge = brighter (silver lining)
                float3 litColor = tintedColor * _CloudBrightness;

                // Silver lining on edges (low density = bright rim light)
                float3 edgeBright = litColor * lerp(1.5, 1.0, density);

                // Self-shadow on dense cores (high density = darker base)
                float selfShadow = 1.0 - _CloudDarkness * density * 0.7;

                // Blend between shadow color (deep) and lit color (surface)
                float3 shadowBlend = lerp(_CloudShadowColor.rgb, edgeBright, saturate(density * 0.8 + 0.2));
                float3 cloudColorResult = shadowBlend * selfShadow;
                // Subtle low-frequency color variation — breaks uniform tint across large cloud formations
                float t_color1 = _CloudShellRadius / max(abs(normalize(dir).y), 0.02);
                float3 colorPos1 = float3(normalize(dir).x * t_color1, 0.0, normalize(dir).z * t_color1)
                                 * _CloudScale * 0.00005
                                 + float3(_CloudDirection.x, 0.0, _CloudDirection.z) * _CloudSpeed * _Time.y;
                float colorVar1 = Noise3D(colorPos1 * 0.4);
                float colorVarWeight1 = lerp(0.03, 0.01, saturate(density - 0.5));
                cloudColorResult *= lerp(1.0 - colorVarWeight1, 1.0 + colorVarWeight1, colorVar1);
                // Power curve: edges wispy, cores opaque
                float alpha1 = pow(density * heightMask * _CloudAlpha, 0.7);

                // ── Composite Layer 2 over Layer 1 using the standard "over" operator
                if (alpha1 > 0.001 && alpha2 > 0.001)
                {
                    float combinedAlpha = alpha1 + alpha2 * (1.0 - alpha1);
                    float3 combinedColor = (cloudColorResult * alpha1 * (1.0 - alpha2) + cloudColor2 * alpha2)
                                          / combinedAlpha;
                    cloudAlpha = combinedAlpha;
                    return combinedColor;
                }
                else if (alpha2 > 0.001)
                {
                    cloudAlpha = alpha2;
                    return cloudColor2;
                }
                else
                {
                    cloudAlpha = alpha1;
                    return cloudColorResult;
                }
            }

            // ─── SUN DISC ────────────────────────────────────────────

            float3 CalculateSunDisc(float3 dir)
            {
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = dot(dir, sunDir);

                // Sharp disc
                float disc = smoothstep(_SunSize + 0.001, _SunSize, 1.0 - sunDot);

                // Soft glow around the disc
                float glow = smoothstep(_SunGlowSize, 0.0, 1.0 - sunDot);
                glow = pow(glow, 2.0) * _SunGlowStrength;

                return _SunColor.rgb * (disc + glow);
            }

            // ─── MOON DISC ───────────────────────────────────────────

            float3 CalculateMoonDisc(float3 dir)
            {
                float3 moonDir = normalize(_MoonDirection.xyz);
                float moonDot = dot(dir, moonDir);

                float disc = smoothstep(_MoonSize + 0.001, _MoonSize, 1.0 - moonDot);

                float glow = smoothstep(_MoonGlowSize, 0.0, 1.0 - moonDot);
                glow = pow(glow, 2.0) * _MoonGlowStrength;

                return _MoonColor.rgb * (disc + glow);
            }

            // ─── FRAGMENT ────────────────────────────────────────────

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);
                dir = RotateAroundY(dir, _Rotation);

                // ─── TIME OF DAY BLEND FACTORS ────────────────────
                // dayFactor: peaks at noon (0.5), 0 at midnight
                float dayFactor = smoothstep(0.15, 0.35, _TimeOfDay)
                                * (1.0 - smoothstep(0.65, 0.85, _TimeOfDay));

                // nightFactor: 1 at midnight, 0 during daytime
                float nightFactor = 1.0 - dayFactor;

                // sunriseFactor: peaks around 0.25
                float sunriseFactor = smoothstep(0.15, 0.25, _TimeOfDay)
                                    * (1.0 - smoothstep(0.25, 0.40, _TimeOfDay));

                // sunsetFactor: peaks around 0.75
                float sunsetFactor = smoothstep(0.60, 0.75, _TimeOfDay)
                                   * (1.0 - smoothstep(0.75, 0.88, _TimeOfDay));

                // Combined transition glow (sunrise OR sunset)
                float transitionFactor = saturate(sunriseFactor + sunsetFactor);

                // ─── BASE SKY COLOR ───────────────────────────────

                // Night: HDR panorama as base
                float2 uv = DirToEquirectUV(dir);
                float3 nightBase = tex2D(_MainTex, uv).rgb * _Tint.rgb * _Exposure;

                // Day: procedural atmosphere
                float3 dayBase = CalculateDayAtmosphere(dir);

                // Blend between night and day
                float3 col = lerp(nightBase, dayBase, dayFactor);

                // ─── SUNSET / SUNRISE HORIZON GLOW ───────────────
                if (transitionFactor > 0.001)
                {
                    float3 glowColor = lerp(_SunriseColor.rgb, _SunsetColor.rgb,
                                            smoothstep(0.3, 0.7, _TimeOfDay));

                    // Glow is strongest near the horizon (dir.y close to 0)
                    float horizonMask = 1.0 - smoothstep(0.0, _SunsetSpread, abs(dir.y));
                    col += glowColor * horizonMask * transitionFactor * _HorizonGlowStrength;
                }

                // ─── PROCEDURAL STARS (night only) ───────────────
                if (_EnableStars > 0.5 && nightFactor > 0.001)
                {
                    float intensity;
                    float3 sColor;
                    ProceduralStars(dir, _StarDensity, _StarSize, intensity, sColor);
                    col += sColor * intensity * _StarBrightness * nightFactor;

                    float intensity2;
                    float3 sColor2;
                    ProceduralStars(dir, _StarDensity * 0.3, _StarSize * 2.5, intensity2, sColor2);
                    col += sColor2 * intensity2 * _StarBrightness * 0.4 * nightFactor;
                }

                // ─── NEBULA (night only) ──────────────────────────
                if (_EnableNebula > 0.5 && nightFactor > 0.001)
                {
                    col += CalculateNebula(dir) * nightFactor;
                }

                // ─── AURORA (night only) ──────────────────────────
                if (_EnableAurora > 0.5 && nightFactor > 0.001)
                {
                    col += CalculateAurora(dir) * nightFactor;
                }

                // ─── GALACTIC DUST (night only) ───────────────────
                if (_EnableDust > 0.5 && nightFactor > 0.001)
                {
                    col += CalculateDust(dir) * nightFactor;
                }

                // ─── PROCEDURAL CLOUDS — compute early to get cloudAlpha for occlusion ──
                float cloudAlpha = 0.0;
                float3 cloudColor = float3(0, 0, 0);
                if (_EnableClouds > 0.5)
                    cloudColor = CalculateClouds(dir, transitionFactor, dayFactor, cloudAlpha);
                float cloudOcclusion = 1.0 - cloudAlpha;
                if (_EnableClouds > 0.5)
                {
                    // Density-aware sun/moon occlusion:
                    // _CloudDensity and _CloudDarkness together define cloud optical thickness.
                    // Dense dark storm clouds block almost all light; thin fair-weather clouds
                    // let most light through the disc.
                    float opticalThickness = _CloudDensity * (0.3 + _CloudDarkness * 0.7);
                    float discOcclusion = saturate(cloudAlpha * opticalThickness);
                    cloudOcclusion = 1.0 - discOcclusion;

                    // Dim background sky (stars, nebula, aurora, dust) behind dense clouds
                    // so they don't bleed through at high cloud coverage.
                    // Applied here, BEFORE sun/moon discs are added, so their dedicated
                    // cloudOcclusion path is unaffected and they are not double-attenuated.
                    col *= lerp(1.0, 0.05, saturate(cloudAlpha * 1.5));
                }

                // ─── SUN DISC ─────────────────────────────────────
                if (_EnableSun > 0.5)
                {
                    // Sun visible during day and at sunrise/sunset transitions
                    float sunVisibility = saturate(dayFactor + transitionFactor * 1.5);
                    col += CalculateSunDisc(dir) * sunVisibility * cloudOcclusion;
                }

                // ─── MOON DISC (night only) ───────────────────────
                if (_EnableMoon > 0.5 && nightFactor > 0.001)
                {
                    col += CalculateMoonDisc(dir) * nightFactor * cloudOcclusion;
                }

                // ─── PROCEDURAL CLOUDS — composite ────────────────
                if (_EnableClouds > 0.5)
                    col = lerp(col, cloudColor, cloudAlpha);
                // ─── HORIZON HAZE ─────────────────────────────────────────
                // Haze color is derived automatically from sky + cloud colors, adapting
                // to both time of day and active weather — no separate color property needed.
                if (_HorizonHazeStrength > 0.001)
                {
                    // Sky horizon reference: blend between night panorama and day horizon
                    // so the haze color shifts from dark-blue at night → warm at sunset → blue-white at noon.
                    float3 skyHorizonColor = lerp(_DaySkyColorHorizon.rgb * _DayAtmosphereStrength,
                                                  nightBase, 1.0 - dayFactor);
                    // Add sunrise/sunset warmth at the horizon line
                    float3 transitionTint = lerp(_SunriseColor.rgb, _SunsetColor.rgb,
                                                 smoothstep(0.3, 0.7, _TimeOfDay));
                    skyHorizonColor = lerp(skyHorizonColor, transitionTint, transitionFactor * 0.5);

                    // Cloud color reference: time-of-day base tinted by the weather cloud color
                    float3 cloudLitColor = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                    cloudLitColor = lerp(cloudLitColor, _CloudSunsetColor.rgb, transitionFactor * 0.8);
                    cloudLitColor *= _CloudColor.rgb;

                    // 60% cloud tint, 40% sky — clouds dominate so horizon and distant
                    // clouds match when looking into the distance.
                    float3 hazeColor = lerp(skyHorizonColor, cloudLitColor, 0.6);

                    float hazeFactor = _HorizonHazeStrength
                        * pow(saturate(1.0 - abs(dir.y) / max(_HorizonHazeHeight, 0.01)),
                              _HorizonHazeFalloff);
                    col = lerp(col, hazeColor, saturate(hazeFactor));
                }

                // ─── VIGNETTE ─────────────────────────────────────
                float vignette = 1.0 - abs(dir.y);
                vignette = pow(vignette, 3.0);
                col = lerp(col, _VignetteColor.rgb, vignette * _VignetteStrength);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
