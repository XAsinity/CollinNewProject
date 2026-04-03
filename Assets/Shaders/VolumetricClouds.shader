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

        [Header(Performance)]
        [Tooltip("Raymarching steps per cloud layer. Lower is faster, higher is better quality.")]
        _CloudStepCount ("Cloud Step Count", Range(8, 64)) = 32
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
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define CLOUD_EPSILON_SMALL 0.001
            #define CLOUD_EPSILON_W 0.0001
            #ifndef UNITY_RAW_FAR_CLIP_VALUE
                #if defined(UNITY_REVERSED_Z)
                    #define UNITY_RAW_FAR_CLIP_VALUE 0.0
                #else
                    #define UNITY_RAW_FAR_CLIP_VALUE 1.0
                #endif
            #endif

            float4x4 _CloudInvProjectionMatrix;
            float4x4 _CloudCameraInvView;

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

            float  _TimeOfDay;
            float4 _SunDirection;
            float4 _MoonDirection;
            int    _CloudStepCount;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 posCS  : SV_POSITION;
                float2 ndcPos : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                float2 uv = float2((IN.vertexID << 1) & 2, IN.vertexID & 2);
                OUT.posCS  = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                OUT.ndcPos = OUT.posCS.xy;
                return OUT;
            }

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash12(i + float2(0.0, 0.0));
                float b = Hash12(i + float2(1.0, 0.0));
                float c = Hash12(i + float2(0.0, 1.0));
                float d = Hash12(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FBM2D(float2 p)
            {
                float f = 0.0;
                f += 0.5000 * Noise2D(p); p *= 2.03;
                f += 0.2500 * Noise2D(p); p *= 2.11;
                f += 0.1250 * Noise2D(p); p *= 2.27;
                f += 0.0625 * Noise2D(p);
                return f;
            }

            float3 GetTimeColor(float timeOfDay)
            {
                float dayFactor = smoothstep(0.15, 0.35, timeOfDay)
                                * (1.0 - smoothstep(0.65, 0.85, timeOfDay));
                float sunriseFactor = smoothstep(0.15, 0.25, timeOfDay)
                                    * (1.0 - smoothstep(0.25, 0.40, timeOfDay));
                float sunsetFactor  = smoothstep(0.60, 0.75, timeOfDay)
                                    * (1.0 - smoothstep(0.75, 0.88, timeOfDay));
                float transition = saturate(sunriseFactor + sunsetFactor);

                float3 timeColor = lerp(_CloudNightColor.rgb, _CloudDayColor.rgb, dayFactor);
                return lerp(timeColor, _CloudSunsetColor.rgb, transition * 0.8);
            }

            float SampleLayerMask(
                float3 viewDir,
                float  cloudScale,
                float  cloudSpeed,
                float  coverage,
                float  density,
                float  sharpness,
                float  edgeSoftness,
                float  variation,
                float3 dissolveOffset,
                float  layerSeed)
            {
                float2 windDir = normalize(_CloudDirection.xz + float2(CLOUD_EPSILON_SMALL, CLOUD_EPSILON_SMALL));
                float2 windOffset = windDir * cloudSpeed * _Time.y * 0.03;
                float2 uv = viewDir.xz * max(cloudScale, 0.01) * 0.75
                          + windOffset
                          + dissolveOffset.xz
                          + float2(layerSeed * 17.3, layerSeed * 29.1);

                float baseNoise = FBM2D(uv);
                float detail    = FBM2D(uv * (2.0 + variation * 2.0) + 11.7);
                float n = lerp(baseNoise, detail, saturate(variation) * 0.35);

                float threshold = 1.0 - saturate(coverage);
                float shaped = saturate((n - threshold) * max(density, 0.0));
                shaped = pow(shaped, max(0.2, 1.1 / max(sharpness, 0.1)));

                float soft = max(edgeSoftness * 1.8, 0.08);
                return smoothstep(0.0, soft, shaped);
            }

            float4 RenderSimpleLayer(
                float3 viewDir,
                float  cloudScale,
                float  cloudSpeed,
                float  coverage,
                float  density,
                float  sharpness,
                float  edgeSoftness,
                float  variation,
                float  opacity,
                float  brightness,
                float  darkness,
                float4 cloudColor,
                float4 shadowColor,
                float  heightMask,
                float3 dissolveOffset,
                float  layerSeed)
            {
                float mask = SampleLayerMask(
                    viewDir, cloudScale, cloudSpeed, coverage, density,
                    sharpness, edgeSoftness, variation, dissolveOffset, layerSeed);

                float sunNdotV = saturate(dot(normalize(_SunDirection.xyz), viewDir) * 0.5 + 0.5);
                float horizonLight = smoothstep(0.0, 0.35, viewDir.y);
                float lightTerm = saturate(0.35 + sunNdotV * 0.65) * horizonLight;

                float3 timeColor = GetTimeColor(_TimeOfDay);
                float3 lit = cloudColor.rgb * timeColor * brightness;
                float3 shaded = lerp(shadowColor.rgb * timeColor, lit, lightTerm * (1.0 - darkness * 0.6));

                float alpha = saturate(mask * opacity * heightMask);
                return float4(shaded, alpha);
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                if (_EnableClouds < 0.5) return float4(0, 0, 0, 0);

                float4 viewPos = mul(_CloudInvProjectionMatrix, float4(IN.ndcPos, UNITY_RAW_FAR_CLIP_VALUE, 1.0));
                float3 viewDir = normalize(mul((float3x3)_CloudCameraInvView, viewPos.xyz / max(viewPos.w, CLOUD_EPSILON_W)));
                if (viewDir.y <= 0.001) return float4(0, 0, 0, 0);

                float zenithBlend = smoothstep(0.55, 0.95, viewDir.y) * saturate(_CloudZenithBlend);
                float horizonPush = _CloudHorizonCoverage * max(saturate(_CloudCoverage * 2.0), 0.15);
                float heightMask1 = smoothstep(-horizonPush, _CloudHeight + 0.3, viewDir.y);
                heightMask1 = lerp(heightMask1, 1.0, zenithBlend * 0.25);

                float heightMask2 = smoothstep(-horizonPush * 0.5, _CloudLayer2Height + 0.3, viewDir.y);
                heightMask2 = lerp(heightMask2, 1.0, zenithBlend * 0.15);

                float4 layer1 = RenderSimpleLayer(
                    viewDir,
                    _CloudScale,
                    _CloudSpeed,
                    _CloudCoverage,
                    _CloudDensity,
                    _CloudSharpness,
                    _CloudEdgeSoftness,
                    _CloudVariation,
                    _CloudAlpha,
                    _CloudBrightness,
                    _CloudDarkness,
                    _CloudColor,
                    _CloudShadowColor,
                    heightMask1,
                    _CloudDissolveOffset.xyz,
                    0.0);

                float4 layer2 = RenderSimpleLayer(
                    viewDir,
                    _Cloud2Scale,
                    _Cloud2Speed,
                    _Cloud2Coverage,
                    _Cloud2Density,
                    _Cloud2Sharpness,
                    _CloudEdgeSoftness * 1.5,
                    0.5,
                    _Cloud2Opacity,
                    _Cloud2Brightness,
                    _Cloud2Darkness,
                    _Cloud2Color,
                    _Cloud2ShadowColor,
                    heightMask2,
                    _CloudDissolveOffset.xyz * 0.5,
                    1.0);

                float a1 = layer1.a;
                float a2 = layer2.a;
                if (a1 < 0.001 && a2 < 0.001) return float4(0, 0, 0, 0);

                float alpha = a1 + a2 * (1.0 - a1);
                float3 color = (a1 > 0.001 && a2 > 0.001)
                    ? (layer1.rgb * a1 * (1.0 - a2) + layer2.rgb * a2) / max(alpha, 0.001)
                    : (a2 > 0.001 ? layer2.rgb : layer1.rgb);

                return float4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
