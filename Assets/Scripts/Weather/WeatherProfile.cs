using UnityEngine;
using UnityEngine.Serialization;

namespace Weather
{
    public enum PrecipitationType { None, Rain, Snow, Hail }

    [CreateAssetMenu(fileName = "WeatherProfile", menuName = "Weather/Weather Profile", order = 1)]
    public class WeatherProfile : ScriptableObject
    {
        [Header("Profile")]
        [Tooltip("Display name for this weather condition")]
        public string profileName = "Clear";

        // ─── EDITOR AUTO-REFRESH ─────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>
        /// Called by Unity whenever any field on this ScriptableObject changes in the Inspector.
        /// If a WeatherManager is running and this is the active profile, immediately push the
        /// new values to the shader so changes are visible without re-triggering a transition.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (WeatherManager.Instance != null && WeatherManager.Instance.currentWeather == this)
                WeatherManager.Instance.RefreshCurrentWeather();
        }
#endif

        // ─── CLOUD SETTINGS ──────────────────────────────────────────

        [Header("Cloud Settings")]
        [Range(0f, 1f)]
        [Tooltip("Minimum cloud coverage — a random value between Min and Max is chosen each transition for variety")]
        public float cloudCoverageMin = 0f;

        [Range(0f, 1f)]
        [Tooltip("Maximum cloud coverage — a random value between Min and Max is chosen each transition for variety")]
        public float cloudCoverageMax = 0.05f;

        [FormerlySerializedAs("cloudDensity")]
        [Tooltip("Multiplier on the material's base cloud density (1 = no change, 1.5 = 50% denser)")]
        public float cloudDensityMultiplier = 1f;

        [FormerlySerializedAs("cloudSharpness")]
        [Tooltip("Multiplier on the material's base cloud sharpness (1 = no change, 2 = harder edges)")]
        public float cloudSharpnessMultiplier = 1f;

        [Tooltip("Brightness multiplier applied to cloud highlights")]
        public float cloudBrightness = 1f;

        [Range(0f, 1f)]
        [Tooltip("Darkening strength for cloud shadows and undersides")]
        public float cloudDarkness = 0.5f;

        [Tooltip("Base color tint applied on top of time-of-day cloud color")]
        public Color cloudColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        [Tooltip("Color used for the shadowed underside of clouds")]
        public Color cloudShadowColor = new Color(0.35f, 0.35f, 0.40f, 1f);

        [FormerlySerializedAs("cloudScale")]
        [Tooltip("Multiplier on the material's base cloud scale (1 = no change, 1.4 = 40% larger clouds)")]
        public float cloudScaleMultiplier = 1f;

        [FormerlySerializedAs("cloudSpeed")]
        [Tooltip("Multiplier on the material's base cloud speed (1 = no change, 2 = twice as fast)")]
        public float cloudSpeedMultiplier = 1f;

        [Range(0f, 0.5f)]
        [Tooltip("How gradually clouds fade at their edges (low = hard edges, high = soft/wispy edges)")]
        public float cloudEdgeSoftness = 0.18f;

        [Range(0f, 1f)]
        [Tooltip("How much turbulence and variation shape the clouds (0 = smooth blobs, 1 = complex billowy cumulus)")]
        public float cloudVariation = 0.5f;

        // ─── FOG SETTINGS ────────────────────────────────────────────

        [Header("Fog Settings")]
        [Tooltip("Multiplied against DayNightCycle's base fog density curve")]
        public float fogDensityMultiplier = 1f;

        [Tooltip("Color tint blended with or replacing the base fog color")]
        public Color fogColorTint = Color.white;

        [Tooltip("If enabled, fogColorTint completely replaces the time-of-day fog color")]
        public bool overrideFogColor = false;

        [Header("Advanced Fog")]
        [Tooltip("Override fog mode during this weather (Exponential, ExponentialSquared, Linear)")]
        public FogMode fogMode = FogMode.Exponential;

        [Tooltip("Whether to switch fog mode for this weather")]
        public bool overrideFogMode = false;

        // ─── LIGHT SETTINGS ──────────────────────────────────────────

        [Header("Light Settings")]
        [Tooltip("Multiplied against the sun light intensity curve (1 = normal, 0 = no sun)")]
        public float sunIntensityMultiplier = 1f;

        [Tooltip("Multiplied against the moon light intensity curve (1 = normal)")]
        public float moonIntensityMultiplier = 1f;

        [Tooltip("Multiplied against ambient light intensity (1 = normal, <1 = darker)")]
        public float ambientIntensityMultiplier = 1f;

        [Tooltip("Color tint multiplied with ambient light color")]
        public Color ambientColorTint = Color.white;

        // ─── URP VOLUME OVERRIDES ────────────────────────────────────

        [Header("URP Volume Overrides")]
        [Tooltip("Bloom post-process intensity for this weather condition")]
        public float bloomIntensity = 0.5f;

        [Tooltip("Luminance threshold above which bloom is applied")]
        public float bloomThreshold = 1f;

        [Range(0f, 1f)]
        [Tooltip("Vignette intensity (darkens screen edges)")]
        public float vignetteIntensity = 0.2f;

        [Tooltip("Post exposure adjustment in EV units (0 = no change)")]
        public float colorAdjustmentExposure = 0f;

        [Range(-100f, 100f)]
        [Tooltip("Color contrast adjustment (-100 to +100)")]
        public float colorAdjustmentContrast = 0f;

        [Range(-100f, 100f)]
        [Tooltip("Color saturation adjustment (-100 = grayscale, +100 = vivid)")]
        public float colorAdjustmentSaturation = 0f;

        // ─── WIND ────────────────────────────────────────────────────

        [Header("Wind")]
        [Tooltip("Normalized wind direction vector (used for cloud drift)")]
        public Vector3 windDirection = Vector3.right;

        [Tooltip("Wind speed multiplier applied to cloud movement")]
        public float windSpeed = 1f;

        [Tooltip("Extra speed added to cloud movement during this weather on top of the multiplier " +
                 "(0 = no boost, 0.3 = 30% faster). Useful for storm conditions to lean into faster winds.")]
        public float windSpeedBoost = 0f;

        // ─── STORM TRANSITION ────────────────────────────────────────

        [Header("Storm Transition")]
        [Range(0f, 2f)]
        [Tooltip("How fast the departing storm front rolls away during weather transitions. " +
                 "0 = clouds uniformly fade in place (gentle transitions like Clear/Slightly Cloudy). " +
                 "Higher values = more dramatic storm roll-off (e.g. 1.0 for Heavy Storm).")]
        public float stormRollSpeed = 0f;

        // ─── PRECIPITATION ───────────────────────────────────────────

        [Header("Precipitation")]
        [Tooltip("Type of precipitation (metadata — hooks into future particle systems)")]
        public PrecipitationType precipitationType = PrecipitationType.None;

        [Range(0f, 1f)]
        [Tooltip("Intensity of precipitation (0 = none, 1 = maximum)")]
        public float precipitationIntensity = 0f;

        // ─── SKYBOX ATMOSPHERE OVERRIDES ─────────────────────────────

        [Header("Skybox Atmosphere Overrides")]
        [Tooltip("Multiplier for the daytime atmosphere strength shader property")]
        public float dayAtmosphereMultiplier = 1f;

        [Tooltip("Multiplier for the horizon glow strength at sunrise/sunset")]
        public float horizonGlowMultiplier = 1f;

        [Range(0f, 1f)]
        [Tooltip("Multiplier for procedural star brightness (reduce when cloudy)")]
        public float starVisibilityMultiplier = 1f;

        // ─── HORIZON HAZE ────────────────────────────────────────────

        [Header("Horizon Haze")]
        [Tooltip("Absolute intensity of the haze band at the horizon line (0 = off, 1 = full). " +
                 "WeatherManager uses this value directly — no base multiplier applied.")]
        public float horizonHazeStrength = 0.3f;

        [Tooltip("How high the haze extends upward into the sky (0.01 = very low band, 1.0 = full sky)")]
        public float horizonHazeHeight = 0.15f;

        [Tooltip("Sharpness of the haze fade edge (low value = soft gradient, high = hard band)")]
        public float horizonHazeFalloff = 3f;

        // ─── CLOUD LAYER 2 SETTINGS ──────────────────────────────────

        [Header("Cloud Layer 2 Settings")]
        [Range(0f, 1f)]
        [Tooltip("Minimum coverage for the high-altitude cloud layer — a random value between Min and Max is chosen each transition")]
        public float cloud2CoverageMin = 0f;

        [Range(0f, 1f)]
        [Tooltip("Maximum coverage for the high-altitude cloud layer — a random value between Min and Max is chosen each transition")]
        public float cloud2CoverageMax = 0.05f;

        [Tooltip("Multiplier on the material's base cloud2 density (1 = no change)")]
        public float cloud2DensityMultiplier = 1f;

        [Tooltip("Multiplier on the material's base cloud2 sharpness (1 = no change)")]
        public float cloud2SharpnessMultiplier = 1f;

        [Tooltip("Multiplier on the material's base cloud2 scale (1 = no change)")]
        public float cloud2ScaleMultiplier = 1f;

        [Tooltip("Multiplier on the material's base cloud2 speed (1 = no change)")]
        public float cloud2SpeedMultiplier = 1f;

        [Tooltip("Brightness multiplier for the high-altitude cloud layer")]
        public float cloud2Brightness = 1f;

        [Range(0f, 1f)]
        [Tooltip("Darkening strength for cloud2 shadows and undersides")]
        public float cloud2Darkness = 0.3f;

        [Tooltip("Base color tint for the high-altitude cloud layer")]
        public Color cloud2Color = new Color(0.96f, 0.96f, 0.98f, 1f);

        [Tooltip("Color for the shadowed underside of the high-altitude cloud layer")]
        public Color cloud2ShadowColor = new Color(0.50f, 0.52f, 0.58f, 1f);

        [Range(0f, 1f)]
        [Tooltip("Overall opacity of the high-altitude cloud layer (lower = more transparent, wispy)")]
        public float cloud2Opacity = 0.5f;

        // ─── CLOUD SHELL / ZENITH BLEND ─────────────────────────────

        [Header("Cloud Shell / Zenith Blend")]
        [Range(0f, 1f)]
        [Tooltip("Controls how far up the sky the flat-plane blend kicks in to eliminate zenith ring artifacts.\n" +
                 "0 = pure sphere-shell sampling everywhere (most 3D depth, rings possible near zenith).\n" +
                 "1 = strong flat-plane blend near zenith (no rings, slight flatness overhead).")]
        public float cloudZenithBlend = 0.4f;

        // ─── VOLUME INFLUENCE ────────────────────────────────────────

        [Header("Volume Influence")]
        [Range(0f, 1f)]
        [Tooltip("How strongly this weather overrides the time-of-day URP Volume.\n" +
                 "0 = clear sky lets the DayNightVolumeController fully control post-processing.\n" +
                 "1 = storm completely overrides with this profile's bloom/vignette/color values.")]
        public float volumeInfluence = 0f;

        // ─── CONTEXT MENU PRESETS ────────────────────────────────────

        [ContextMenu("Preset: Clear")]
        private void PresetClear()
        {
            profileName = "Clear";
            cloudCoverageMin = 0.0f; cloudCoverageMax = 0.05f;
            cloudDensityMultiplier = 1f; cloudSharpnessMultiplier = 1f; cloudBrightness = 1f; cloudDarkness = 0.3f;
            cloudColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            cloudShadowColor = new Color(0.35f, 0.35f, 0.40f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 1f;
            fogDensityMultiplier = 0.3f; fogColorTint = Color.white; overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 1.0f; moonIntensityMultiplier = 1.0f;
            ambientIntensityMultiplier = 1.0f; ambientColorTint = Color.white;
            bloomIntensity = 0.5f; bloomThreshold = 1f; vignetteIntensity = 0.1f;
            colorAdjustmentExposure = 0f; colorAdjustmentContrast = 0f; colorAdjustmentSaturation = 0f;
            windDirection = Vector3.right; windSpeed = 0.5f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 1f; horizonGlowMultiplier = 1f; starVisibilityMultiplier = 1f;
            horizonHazeStrength = 0.15f; horizonHazeHeight = 0.1f; horizonHazeFalloff = 4f;
            cloud2CoverageMin = 0.0f; cloud2CoverageMax = 0.05f;
            cloud2DensityMultiplier = 0.6f; cloud2SharpnessMultiplier = 0.7f; cloud2ScaleMultiplier = 1.2f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 1.0f; cloud2Darkness = 0.30f;
            cloud2Color = new Color(0.95f, 0.95f, 0.98f, 1f); cloud2ShadowColor = new Color(0.45f, 0.45f, 0.50f, 1f);
            cloud2Opacity = 0.30f;
            cloudEdgeSoftness = 0.25f; cloudVariation = 0.4f;
            volumeInfluence = 0.0f; // fully defer to DayNightVolumeController
            stormRollSpeed = 0f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Slightly Cloudy")]
        private void PresetSlightlyCloudy()
        {
            profileName = "Slightly Cloudy";
            cloudCoverageMin = 0.1f; cloudCoverageMax = 0.25f;
            cloudDensityMultiplier = 1f; cloudSharpnessMultiplier = 1f; cloudBrightness = 1f; cloudDarkness = 0.35f;
            cloudColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            cloudShadowColor = new Color(0.35f, 0.35f, 0.40f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 1f;
            fogDensityMultiplier = 0.5f; fogColorTint = Color.white; overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.95f; moonIntensityMultiplier = 1.0f;
            ambientIntensityMultiplier = 0.95f; ambientColorTint = Color.white;
            bloomIntensity = 0.4f; bloomThreshold = 1f; vignetteIntensity = 0.12f;
            colorAdjustmentExposure = 0f; colorAdjustmentContrast = 0f; colorAdjustmentSaturation = 0f;
            windDirection = Vector3.right; windSpeed = 0.8f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 1f; horizonGlowMultiplier = 0.9f; starVisibilityMultiplier = 0.9f;
            horizonHazeStrength = 0.2f; horizonHazeHeight = 0.11f; horizonHazeFalloff = 3.8f;
            cloud2CoverageMin = 0.05f; cloud2CoverageMax = 0.15f;
            cloud2DensityMultiplier = 0.7f; cloud2SharpnessMultiplier = 0.8f; cloud2ScaleMultiplier = 1.1f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 1.0f; cloud2Darkness = 0.35f;
            cloud2Color = new Color(0.90f, 0.90f, 0.93f, 1f); cloud2ShadowColor = new Color(0.40f, 0.40f, 0.45f, 1f);
            cloud2Opacity = 0.35f;
            cloudEdgeSoftness = 0.22f; cloudVariation = 0.5f;
            volumeInfluence = 0.1f;
            stormRollSpeed = 0f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Partly Cloudy")]
        private void PresetPartlyCloudy()
        {
            profileName = "Partly Cloudy";
            cloudCoverageMin = 0.3f; cloudCoverageMax = 0.5f;
            cloudDensityMultiplier = 1.1f; cloudSharpnessMultiplier = 1f; cloudBrightness = 1f; cloudDarkness = 0.4f;
            cloudColor = new Color(0.93f, 0.93f, 0.93f, 1f);
            cloudShadowColor = new Color(0.32f, 0.32f, 0.38f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 1f;
            fogDensityMultiplier = 0.7f; fogColorTint = Color.white; overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.85f; moonIntensityMultiplier = 0.9f;
            ambientIntensityMultiplier = 0.9f; ambientColorTint = Color.white;
            bloomIntensity = 0.35f; bloomThreshold = 1f; vignetteIntensity = 0.15f;
            colorAdjustmentExposure = 0f; colorAdjustmentContrast = 0f; colorAdjustmentSaturation = -5f;
            windDirection = Vector3.right; windSpeed = 1f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.9f; horizonGlowMultiplier = 0.8f; starVisibilityMultiplier = 0.6f;
            horizonHazeStrength = 0.25f; horizonHazeHeight = 0.12f; horizonHazeFalloff = 3.5f;
            cloud2CoverageMin = 0.1f; cloud2CoverageMax = 0.3f;
            cloud2DensityMultiplier = 0.8f; cloud2SharpnessMultiplier = 0.9f; cloud2ScaleMultiplier = 1.0f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.9f; cloud2Darkness = 0.40f;
            cloud2Color = new Color(0.82f, 0.82f, 0.86f, 1f); cloud2ShadowColor = new Color(0.32f, 0.32f, 0.38f, 1f);
            cloud2Opacity = 0.40f;
            cloudEdgeSoftness = 0.18f; cloudVariation = 0.65f;
            volumeInfluence = 0.2f;
            stormRollSpeed = 0.1f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Mostly Cloudy")]
        private void PresetMostlyCloudy()
        {
            profileName = "Mostly Cloudy";
            cloudCoverageMin = 0.55f; cloudCoverageMax = 0.75f;
            cloudDensityMultiplier = 1.2f; cloudSharpnessMultiplier = 1.1f; cloudBrightness = 0.9f; cloudDarkness = 0.5f;
            cloudColor = new Color(0.85f, 0.85f, 0.88f, 1f);
            cloudShadowColor = new Color(0.28f, 0.28f, 0.34f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 1.0f;
            fogDensityMultiplier = 1.0f; fogColorTint = Color.white; overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.6f; moonIntensityMultiplier = 0.7f;
            ambientIntensityMultiplier = 0.75f; ambientColorTint = new Color(0.9f, 0.9f, 0.95f, 1f);
            bloomIntensity = 0.25f; bloomThreshold = 1f; vignetteIntensity = 0.2f;
            colorAdjustmentExposure = -0.2f; colorAdjustmentContrast = 8f; colorAdjustmentSaturation = -15f;
            windDirection = Vector3.right; windSpeed = 1.2f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.7f; horizonGlowMultiplier = 0.5f; starVisibilityMultiplier = 0.2f;
            horizonHazeStrength = 0.35f; horizonHazeHeight = 0.16f; horizonHazeFalloff = 2.8f;
            cloud2CoverageMin = 0.25f; cloud2CoverageMax = 0.45f;
            cloud2DensityMultiplier = 0.9f; cloud2SharpnessMultiplier = 1.0f; cloud2ScaleMultiplier = 0.95f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.8f; cloud2Darkness = 0.55f;
            cloud2Color = new Color(0.70f, 0.70f, 0.76f, 1f); cloud2ShadowColor = new Color(0.24f, 0.24f, 0.30f, 1f);
            cloud2Opacity = 0.50f;
            cloudEdgeSoftness = 0.15f; cloudVariation = 0.6f;
            volumeInfluence = 0.5f;
            stormRollSpeed = 0.2f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Overcast")]
        private void PresetOvercast()
        {
            profileName = "Overcast";
            cloudCoverageMin = 0.85f; cloudCoverageMax = 0.95f;
            cloudDensityMultiplier = 1.4f; cloudSharpnessMultiplier = 1.3f; cloudBrightness = 0.75f; cloudDarkness = 0.6f;
            cloudColor = new Color(0.75f, 0.76f, 0.80f, 1f);
            cloudShadowColor = new Color(0.22f, 0.22f, 0.28f, 1f);
            cloudScaleMultiplier = 1.2f; cloudSpeedMultiplier = 1.05f;
            fogDensityMultiplier = 1.5f; fogColorTint = new Color(0.7f, 0.72f, 0.78f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.35f; moonIntensityMultiplier = 0.4f;
            ambientIntensityMultiplier = 0.55f; ambientColorTint = new Color(0.82f, 0.84f, 0.90f, 1f);
            bloomIntensity = 0.15f; bloomThreshold = 1.2f; vignetteIntensity = 0.30f;
            colorAdjustmentExposure = -0.3f; colorAdjustmentContrast = 12f; colorAdjustmentSaturation = -25f;
            windDirection = Vector3.right; windSpeed = 1.4f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.4f; horizonGlowMultiplier = 0.2f; starVisibilityMultiplier = 0.05f;
            horizonHazeStrength = 0.6f; horizonHazeHeight = 0.25f; horizonHazeFalloff = 1.8f;
            cloud2CoverageMin = 0.4f; cloud2CoverageMax = 0.7f;
            cloud2DensityMultiplier = 1.1f; cloud2SharpnessMultiplier = 1.0f; cloud2ScaleMultiplier = 0.9f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.65f; cloud2Darkness = 0.65f;
            cloud2Color = new Color(0.55f, 0.56f, 0.62f, 1f); cloud2ShadowColor = new Color(0.18f, 0.18f, 0.24f, 1f);
            cloud2Opacity = 0.60f;
            cloudEdgeSoftness = 0.12f; cloudVariation = 0.45f;
            volumeInfluence = 0.75f;
            stormRollSpeed = 0.3f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Super Cloudy")]
        private void PresetSuperCloudy()
        {
            profileName = "Super Cloudy";
            cloudCoverageMin = 0.93f; cloudCoverageMax = 0.99f;
            cloudDensityMultiplier = 1.6f; cloudSharpnessMultiplier = 1.5f; cloudBrightness = 0.65f; cloudDarkness = 0.7f;
            cloudColor = new Color(0.65f, 0.67f, 0.72f, 1f);
            cloudShadowColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            cloudScaleMultiplier = 1.2f; cloudSpeedMultiplier = 1.1f;
            fogDensityMultiplier = 2.0f; fogColorTint = new Color(0.62f, 0.64f, 0.70f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.2f; moonIntensityMultiplier = 0.25f;
            ambientIntensityMultiplier = 0.4f; ambientColorTint = new Color(0.72f, 0.74f, 0.82f, 1f);
            bloomIntensity = 0.1f; bloomThreshold = 1.3f; vignetteIntensity = 0.35f;
            colorAdjustmentExposure = -0.45f; colorAdjustmentContrast = 18f; colorAdjustmentSaturation = -35f;
            windDirection = Vector3.right; windSpeed = 1.6f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.25f; horizonGlowMultiplier = 0.1f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.5f; horizonHazeHeight = 0.22f; horizonHazeFalloff = 2.0f;
            cloud2CoverageMin = 0.5f; cloud2CoverageMax = 0.75f;
            cloud2DensityMultiplier = 1.2f; cloud2SharpnessMultiplier = 1.1f; cloud2ScaleMultiplier = 0.85f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.5f; cloud2Darkness = 0.75f;
            cloud2Color = new Color(0.45f, 0.47f, 0.54f, 1f); cloud2ShadowColor = new Color(0.14f, 0.14f, 0.18f, 1f);
            cloud2Opacity = 0.65f;
            cloudEdgeSoftness = 0.1f; cloudVariation = 0.35f;
            volumeInfluence = 0.85f;
            stormRollSpeed = 0.4f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Light Rain")]
        private void PresetLightRain()
        {
            profileName = "Light Rain";
            cloudCoverageMin = 0.7f; cloudCoverageMax = 0.85f;
            cloudDensityMultiplier = 1.3f; cloudSharpnessMultiplier = 1.2f; cloudBrightness = 0.7f; cloudDarkness = 0.6f;
            cloudColor = new Color(0.58f, 0.61f, 0.68f, 1f);
            cloudShadowColor = new Color(0.20f, 0.21f, 0.26f, 1f);
            cloudScaleMultiplier = 1.1f; cloudSpeedMultiplier = 1.1f;
            fogDensityMultiplier = 1.8f; fogColorTint = new Color(0.55f, 0.60f, 0.68f, 1f); overrideFogColor = true; overrideFogMode = false;
            sunIntensityMultiplier = 0.4f; moonIntensityMultiplier = 0.35f;
            ambientIntensityMultiplier = 0.5f; ambientColorTint = new Color(0.68f, 0.72f, 0.82f, 1f);
            bloomIntensity = 0.1f; bloomThreshold = 1.2f; vignetteIntensity = 0.35f;
            colorAdjustmentExposure = -0.4f; colorAdjustmentContrast = 15f; colorAdjustmentSaturation = -25f;
            windDirection = new Vector3(1f, 0f, 0.3f); windSpeed = 1.5f;
            precipitationType = PrecipitationType.Rain; precipitationIntensity = 0.4f;
            dayAtmosphereMultiplier = 0.5f; horizonGlowMultiplier = 0.15f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.55f; horizonHazeHeight = 0.2f; horizonHazeFalloff = 2.2f;
            cloud2CoverageMin = 0.3f; cloud2CoverageMax = 0.6f;
            cloud2DensityMultiplier = 1.1f; cloud2SharpnessMultiplier = 1.1f; cloud2ScaleMultiplier = 0.9f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.55f; cloud2Darkness = 0.70f;
            cloud2Color = new Color(0.42f, 0.44f, 0.52f, 1f); cloud2ShadowColor = new Color(0.15f, 0.15f, 0.20f, 1f);
            cloud2Opacity = 0.70f;
            cloudEdgeSoftness = 0.1f; cloudVariation = 0.4f;
            volumeInfluence = 0.85f;
            stormRollSpeed = 0.5f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Heavy Storm")]
        private void PresetHeavyStorm()
        {
            profileName = "Heavy Storm";
            cloudCoverageMin = 0.9f; cloudCoverageMax = 0.98f;
            cloudDensityMultiplier = 1.8f; cloudSharpnessMultiplier = 1.8f; cloudBrightness = 0.5f; cloudDarkness = 0.8f;
            cloudColor = new Color(0.40f, 0.42f, 0.48f, 1f);
            cloudShadowColor = new Color(0.12f, 0.12f, 0.16f, 1f);
            cloudScaleMultiplier = 1.4f; cloudSpeedMultiplier = 1.2f;
            fogDensityMultiplier = 2.5f; fogColorTint = new Color(0.35f, 0.38f, 0.45f, 1f); overrideFogColor = true; overrideFogMode = false;
            sunIntensityMultiplier = 0.15f; moonIntensityMultiplier = 0.1f;
            ambientIntensityMultiplier = 0.3f; ambientColorTint = new Color(0.55f, 0.58f, 0.68f, 1f);
            bloomIntensity = 0.03f; bloomThreshold = 1.5f; vignetteIntensity = 0.50f;
            colorAdjustmentExposure = -0.8f; colorAdjustmentContrast = 30f; colorAdjustmentSaturation = -50f;
            windDirection = new Vector3(1f, 0f, 0.5f); windSpeed = 2.5f;
            precipitationType = PrecipitationType.Rain; precipitationIntensity = 1.0f;
            dayAtmosphereMultiplier = 0.15f; horizonGlowMultiplier = 0.05f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.7f; horizonHazeHeight = 0.3f; horizonHazeFalloff = 1.5f;
            cloud2CoverageMin = 0.5f; cloud2CoverageMax = 0.85f;
            cloud2DensityMultiplier = 1.3f; cloud2SharpnessMultiplier = 1.2f; cloud2ScaleMultiplier = 0.85f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.4f; cloud2Darkness = 0.80f;
            cloud2Color = new Color(0.30f, 0.32f, 0.40f, 1f); cloud2ShadowColor = new Color(0.10f, 0.10f, 0.14f, 1f);
            cloud2Opacity = 0.80f;
            cloudEdgeSoftness = 0.08f; cloudVariation = 0.3f;
            volumeInfluence = 1.0f; // fully override TOD volume during a storm
            stormRollSpeed = 1.0f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Fog")]
        private void PresetFog()
        {
            profileName = "Fog";
            cloudCoverageMin = 0.3f; cloudCoverageMax = 0.5f;
            cloudDensityMultiplier = 0.8f; cloudSharpnessMultiplier = 0.7f; cloudBrightness = 0.9f; cloudDarkness = 0.3f;
            cloudColor = new Color(0.88f, 0.90f, 0.92f, 1f);
            cloudShadowColor = new Color(0.70f, 0.72f, 0.76f, 1f);
            cloudScaleMultiplier = 0.8f; cloudSpeedMultiplier = 0.5f;
            fogDensityMultiplier = 4.0f; fogColorTint = new Color(0.75f, 0.78f, 0.82f, 1f); overrideFogColor = true;
            fogMode = FogMode.ExponentialSquared; overrideFogMode = true;
            sunIntensityMultiplier = 0.5f; moonIntensityMultiplier = 0.5f;
            ambientIntensityMultiplier = 0.7f; ambientColorTint = new Color(0.85f, 0.88f, 0.92f, 1f);
            bloomIntensity = 0.35f; bloomThreshold = 0.65f; vignetteIntensity = 0.40f;
            colorAdjustmentExposure = -0.15f; colorAdjustmentContrast = -10f; colorAdjustmentSaturation = -30f;
            windDirection = Vector3.right; windSpeed = 0.2f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.6f; horizonGlowMultiplier = 0.3f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.9f; horizonHazeHeight = 0.35f; horizonHazeFalloff = 1f;
            cloud2CoverageMin = 0.1f; cloud2CoverageMax = 0.4f;
            cloud2DensityMultiplier = 0.7f; cloud2SharpnessMultiplier = 0.6f; cloud2ScaleMultiplier = 1.1f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.85f; cloud2Darkness = 0.35f;
            cloud2Color = new Color(0.65f, 0.68f, 0.72f, 1f); cloud2ShadowColor = new Color(0.35f, 0.35f, 0.40f, 1f);
            cloud2Opacity = 0.40f;
            cloudEdgeSoftness = 0.3f; cloudVariation = 0.2f;
            volumeInfluence = 0.90f;
            stormRollSpeed = 0.1f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }

        [ContextMenu("Preset: Snow")]
        private void PresetSnow()
        {
            profileName = "Snow";
            cloudCoverageMin = 0.6f; cloudCoverageMax = 0.8f;
            cloudDensityMultiplier = 1.2f; cloudSharpnessMultiplier = 1.1f; cloudBrightness = 1.1f; cloudDarkness = 0.3f;
            cloudColor = new Color(0.92f, 0.94f, 0.98f, 1f);
            cloudShadowColor = new Color(0.60f, 0.62f, 0.70f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 0.9f;
            fogDensityMultiplier = 1.5f; fogColorTint = new Color(0.85f, 0.88f, 0.95f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.5f; moonIntensityMultiplier = 0.6f;
            ambientIntensityMultiplier = 0.7f; ambientColorTint = new Color(0.90f, 0.92f, 0.98f, 1f);
            bloomIntensity = 0.6f; bloomThreshold = 0.9f; vignetteIntensity = 0.2f;
            colorAdjustmentExposure = 0.1f; colorAdjustmentContrast = -5f; colorAdjustmentSaturation = -20f;
            windDirection = new Vector3(0.8f, 0f, 0.2f); windSpeed = 0.8f;
            precipitationType = PrecipitationType.Snow; precipitationIntensity = 0.7f;
            dayAtmosphereMultiplier = 0.6f; horizonGlowMultiplier = 0.4f; starVisibilityMultiplier = 0.1f;
            horizonHazeStrength = 0.5f; horizonHazeHeight = 0.2f; horizonHazeFalloff = 2.5f;
            cloud2CoverageMin = 0.3f; cloud2CoverageMax = 0.65f;
            cloud2DensityMultiplier = 1.0f; cloud2SharpnessMultiplier = 1.0f; cloud2ScaleMultiplier = 1.0f; cloud2SpeedMultiplier = 1.0f;
            cloud2Brightness = 0.95f; cloud2Darkness = 0.25f;
            cloud2Color = new Color(0.72f, 0.74f, 0.78f, 1f); cloud2ShadowColor = new Color(0.30f, 0.30f, 0.35f, 1f);
            cloud2Opacity = 0.50f;
            cloudEdgeSoftness = 0.15f; cloudVariation = 0.5f;
            volumeInfluence = 0.6f;
            stormRollSpeed = 0.3f;
            windSpeedBoost = 0f;
            cloudZenithBlend = 0.4f;
        }
    }
}
