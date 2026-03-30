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

                // ─── SUN DISC ─────────────────────────────────────
                if (_EnableSun > 0.5)
                {
                    // Sun visible during day and at sunrise/sunset transitions
                    float sunVisibility = saturate(dayFactor + transitionFactor * 1.5);
                    col += CalculateSunDisc(dir) * sunVisibility;
                }

                // ─── MOON DISC (night only) ───────────────────────
                if (_EnableMoon > 0.5 && nightFactor > 0.001)
                {
                    col += CalculateMoonDisc(dir) * nightFactor;
                }

                // ─── HORIZON HAZE ─────────────────────────────────────────
                // Haze color is derived from sky colors only (clouds are now rendered
                // by the separate VolumetricClouds pass).
                if (_HorizonHazeStrength > 0.001)
                {
                    // Sky horizon reference: blend between night panorama and day horizon
                    // so the haze color shifts from dark-blue at night → warm at sunset → blue-white at noon.
                    float3 skyHorizonColor = lerp(_DaySkyColorHorizon.rgb * _DayAtmosphereStrength,
                                                  nightBase, 1.0 - dayFactor);
                    // Add sunrise/sunset warmth at the horizon line
                    float3 transitionTint = lerp(_SunriseColor.rgb, _SunsetColor.rgb,
                                                 smoothstep(0.3, 0.7, _TimeOfDay));
                    float3 hazeColor = lerp(skyHorizonColor, transitionTint, transitionFactor * 0.5);

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
