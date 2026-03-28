using UnityEngine;

namespace Weather
{
    public enum PrecipitationType { None, Rain, Snow, Hail }

    [CreateAssetMenu(fileName = "WeatherProfile", menuName = "Weather/Weather Profile", order = 1)]
    public class WeatherProfile : ScriptableObject
    {
        [Header("Profile")]
        [Tooltip("Display name for this weather condition")]
        public string profileName = "Clear";

        // ─── CLOUD SETTINGS ──────────────────────────────────────────

        [Header("Cloud Settings")]
        [Range(0f, 1f)]
        [Tooltip("Minimum cloud coverage — a random value between Min and Max is chosen each transition for variety")]
        public float cloudCoverageMin = 0f;

        [Range(0f, 1f)]
        [Tooltip("Maximum cloud coverage — a random value between Min and Max is chosen each transition for variety")]
        public float cloudCoverageMax = 0.05f;

        [Tooltip("Multiplier on the material's base cloud density (1 = no change, 1.5 = 50% denser)")]
        public float cloudDensityMultiplier = 1f;

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

        [Tooltip("Multiplier on the material's base cloud scale (1 = no change, 1.4 = 40% larger clouds)")]
        public float cloudScaleMultiplier = 1f;

        [Tooltip("Multiplier on the material's base cloud speed (1 = no change, 2 = twice as fast)")]
        public float cloudSpeedMultiplier = 1f;

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
        [Tooltip("Intensity of the haze band at the horizon line (0 = off, 1 = full)")]
        public float horizonHazeStrength = 0.3f;

        [Tooltip("Base tint color of the horizon haze, blended with time-of-day sun color")]
        public Color horizonHazeColor = Color.white;

        [Tooltip("How high the haze extends upward into the sky (0.01 = very low band, 1.0 = full sky)")]
        public float horizonHazeHeight = 0.15f;

        [Tooltip("Sharpness of the haze fade edge (low value = soft gradient, high = hard band)")]
        public float horizonHazeFalloff = 3f;

        // ─── CLOUD LAYER 2 ───────────────────────────────────────────

        [Header("Cloud Layer 2")]
        [Range(0f, 1f)]
        [Tooltip("Coverage of the secondary high-altitude cloud layer (0 = off)")]
        public float cloudLayer2Coverage = 0f;

        [Tooltip("Scale of the secondary cloud layer — larger values produce thinner wispy clouds")]
        public float cloudLayer2Scale = 8f;

        [Tooltip("Scroll speed of the secondary cloud layer — upper layers can move at a different rate")]
        public float cloudLayer2Speed = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Opacity/visibility of the secondary cloud layer")]
        public float cloudLayer2Opacity = 0.3f;

        [Tooltip("Vertical height bias for the secondary cloud layer")]
        public float cloudLayer2Height = 0.1f;

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
            horizonHazeStrength = 0.2f; horizonHazeColor = Color.white; horizonHazeHeight = 0.15f; horizonHazeFalloff = 3f;
            cloudLayer2Coverage = 0.05f; cloudLayer2Scale = 8f; cloudLayer2Speed = 0.5f; cloudLayer2Opacity = 0.15f; cloudLayer2Height = 0.1f;
            volumeInfluence = 0.0f; // fully defer to DayNightVolumeController
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
            horizonHazeStrength = 0.25f; horizonHazeColor = Color.white; horizonHazeHeight = 0.15f; horizonHazeFalloff = 3f;
            cloudLayer2Coverage = 0.1f; cloudLayer2Scale = 8f; cloudLayer2Speed = 0.5f; cloudLayer2Opacity = 0.2f; cloudLayer2Height = 0.1f;
            volumeInfluence = 0.1f;
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
            horizonHazeStrength = 0.35f; horizonHazeColor = Color.white; horizonHazeHeight = 0.15f; horizonHazeFalloff = 3f;
            cloudLayer2Coverage = 0.2f; cloudLayer2Scale = 8f; cloudLayer2Speed = 0.5f; cloudLayer2Opacity = 0.3f; cloudLayer2Height = 0.1f;
            volumeInfluence = 0.2f;
        }

        [ContextMenu("Preset: Mostly Cloudy")]
        private void PresetMostlyCloudy()
        {
            profileName = "Mostly Cloudy";
            cloudCoverageMin = 0.55f; cloudCoverageMax = 0.75f;
            cloudDensityMultiplier = 1.2f; cloudSharpnessMultiplier = 1.1f; cloudBrightness = 0.9f; cloudDarkness = 0.5f;
            cloudColor = new Color(0.85f, 0.85f, 0.88f, 1f);
            cloudShadowColor = new Color(0.28f, 0.28f, 0.34f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 1.1f;
            fogDensityMultiplier = 1.0f; fogColorTint = Color.white; overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.6f; moonIntensityMultiplier = 0.7f;
            ambientIntensityMultiplier = 0.75f; ambientColorTint = new Color(0.9f, 0.9f, 0.95f, 1f);
            bloomIntensity = 0.25f; bloomThreshold = 1f; vignetteIntensity = 0.2f;
            colorAdjustmentExposure = -0.2f; colorAdjustmentContrast = 8f; colorAdjustmentSaturation = -15f;
            windDirection = Vector3.right; windSpeed = 1.2f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.7f; horizonGlowMultiplier = 0.5f; starVisibilityMultiplier = 0.2f;
            horizonHazeStrength = 0.45f; horizonHazeColor = new Color(0.85f, 0.87f, 0.92f, 1f); horizonHazeHeight = 0.18f; horizonHazeFalloff = 2.5f;
            cloudLayer2Coverage = 0.35f; cloudLayer2Scale = 8f; cloudLayer2Speed = 0.6f; cloudLayer2Opacity = 0.4f; cloudLayer2Height = 0.15f;
            volumeInfluence = 0.5f;
        }

        [ContextMenu("Preset: Overcast")]
        private void PresetOvercast()
        {
            profileName = "Overcast";
            cloudCoverageMin = 0.85f; cloudCoverageMax = 0.95f;
            cloudDensityMultiplier = 1.4f; cloudSharpnessMultiplier = 1.3f; cloudBrightness = 0.75f; cloudDarkness = 0.6f;
            cloudColor = new Color(0.75f, 0.76f, 0.80f, 1f);
            cloudShadowColor = new Color(0.22f, 0.22f, 0.28f, 1f);
            cloudScaleMultiplier = 1.2f; cloudSpeedMultiplier = 1.3f;
            fogDensityMultiplier = 1.5f; fogColorTint = new Color(0.7f, 0.72f, 0.78f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.35f; moonIntensityMultiplier = 0.4f;
            ambientIntensityMultiplier = 0.55f; ambientColorTint = new Color(0.82f, 0.84f, 0.90f, 1f);
            bloomIntensity = 0.15f; bloomThreshold = 1.2f; vignetteIntensity = 0.30f;
            colorAdjustmentExposure = -0.3f; colorAdjustmentContrast = 12f; colorAdjustmentSaturation = -25f;
            windDirection = Vector3.right; windSpeed = 1.4f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.4f; horizonGlowMultiplier = 0.2f; starVisibilityMultiplier = 0.05f;
            horizonHazeStrength = 0.6f; horizonHazeColor = new Color(0.72f, 0.75f, 0.82f, 1f); horizonHazeHeight = 0.2f; horizonHazeFalloff = 2f;
            cloudLayer2Coverage = 0.5f; cloudLayer2Scale = 7f; cloudLayer2Speed = 0.7f; cloudLayer2Opacity = 0.5f; cloudLayer2Height = 0.15f;
            volumeInfluence = 0.75f;
        }

        [ContextMenu("Preset: Super Cloudy")]
        private void PresetSuperCloudy()
        {
            profileName = "Super Cloudy";
            cloudCoverageMin = 0.93f; cloudCoverageMax = 0.99f;
            cloudDensityMultiplier = 1.6f; cloudSharpnessMultiplier = 1.5f; cloudBrightness = 0.65f; cloudDarkness = 0.7f;
            cloudColor = new Color(0.65f, 0.67f, 0.72f, 1f);
            cloudShadowColor = new Color(0.18f, 0.18f, 0.22f, 1f);
            cloudScaleMultiplier = 1.2f; cloudSpeedMultiplier = 1.3f;
            fogDensityMultiplier = 2.0f; fogColorTint = new Color(0.62f, 0.64f, 0.70f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.2f; moonIntensityMultiplier = 0.25f;
            ambientIntensityMultiplier = 0.4f; ambientColorTint = new Color(0.72f, 0.74f, 0.82f, 1f);
            bloomIntensity = 0.1f; bloomThreshold = 1.3f; vignetteIntensity = 0.35f;
            colorAdjustmentExposure = -0.45f; colorAdjustmentContrast = 18f; colorAdjustmentSaturation = -35f;
            windDirection = Vector3.right; windSpeed = 1.6f;
            precipitationType = PrecipitationType.None; precipitationIntensity = 0f;
            dayAtmosphereMultiplier = 0.25f; horizonGlowMultiplier = 0.1f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.7f; horizonHazeColor = new Color(0.62f, 0.65f, 0.72f, 1f); horizonHazeHeight = 0.22f; horizonHazeFalloff = 2f;
            cloudLayer2Coverage = 0.6f; cloudLayer2Scale = 7f; cloudLayer2Speed = 0.8f; cloudLayer2Opacity = 0.55f; cloudLayer2Height = 0.15f;
            volumeInfluence = 0.85f;
        }

        [ContextMenu("Preset: Light Rain")]
        private void PresetLightRain()
        {
            profileName = "Light Rain";
            cloudCoverageMin = 0.7f; cloudCoverageMax = 0.85f;
            cloudDensityMultiplier = 1.3f; cloudSharpnessMultiplier = 1.2f; cloudBrightness = 0.7f; cloudDarkness = 0.6f;
            cloudColor = new Color(0.58f, 0.61f, 0.68f, 1f);
            cloudShadowColor = new Color(0.20f, 0.21f, 0.26f, 1f);
            cloudScaleMultiplier = 1.1f; cloudSpeedMultiplier = 1.5f;
            fogDensityMultiplier = 1.8f; fogColorTint = new Color(0.55f, 0.60f, 0.68f, 1f); overrideFogColor = true; overrideFogMode = false;
            sunIntensityMultiplier = 0.4f; moonIntensityMultiplier = 0.35f;
            ambientIntensityMultiplier = 0.5f; ambientColorTint = new Color(0.68f, 0.72f, 0.82f, 1f);
            bloomIntensity = 0.1f; bloomThreshold = 1.2f; vignetteIntensity = 0.35f;
            colorAdjustmentExposure = -0.4f; colorAdjustmentContrast = 15f; colorAdjustmentSaturation = -25f;
            windDirection = new Vector3(1f, 0f, 0.3f); windSpeed = 1.5f;
            precipitationType = PrecipitationType.Rain; precipitationIntensity = 0.4f;
            dayAtmosphereMultiplier = 0.5f; horizonGlowMultiplier = 0.15f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.55f; horizonHazeColor = new Color(0.55f, 0.60f, 0.68f, 1f); horizonHazeHeight = 0.2f; horizonHazeFalloff = 2.5f;
            cloudLayer2Coverage = 0.4f; cloudLayer2Scale = 7f; cloudLayer2Speed = 0.9f; cloudLayer2Opacity = 0.45f; cloudLayer2Height = 0.1f;
            volumeInfluence = 0.85f;
        }

        [ContextMenu("Preset: Heavy Storm")]
        private void PresetHeavyStorm()
        {
            profileName = "Heavy Storm";
            cloudCoverageMin = 0.9f; cloudCoverageMax = 0.98f;
            cloudDensityMultiplier = 1.8f; cloudSharpnessMultiplier = 1.8f; cloudBrightness = 0.5f; cloudDarkness = 0.8f;
            cloudColor = new Color(0.40f, 0.42f, 0.48f, 1f);
            cloudShadowColor = new Color(0.12f, 0.12f, 0.16f, 1f);
            cloudScaleMultiplier = 1.4f; cloudSpeedMultiplier = 2.5f;
            fogDensityMultiplier = 2.5f; fogColorTint = new Color(0.35f, 0.38f, 0.45f, 1f); overrideFogColor = true; overrideFogMode = false;
            sunIntensityMultiplier = 0.15f; moonIntensityMultiplier = 0.1f;
            ambientIntensityMultiplier = 0.3f; ambientColorTint = new Color(0.55f, 0.58f, 0.68f, 1f);
            bloomIntensity = 0.03f; bloomThreshold = 1.5f; vignetteIntensity = 0.50f;
            colorAdjustmentExposure = -0.8f; colorAdjustmentContrast = 30f; colorAdjustmentSaturation = -50f;
            windDirection = new Vector3(1f, 0f, 0.5f); windSpeed = 2.5f;
            precipitationType = PrecipitationType.Rain; precipitationIntensity = 1.0f;
            dayAtmosphereMultiplier = 0.15f; horizonGlowMultiplier = 0.05f; starVisibilityMultiplier = 0.0f;
            horizonHazeStrength = 0.8f; horizonHazeColor = new Color(0.35f, 0.38f, 0.45f, 1f); horizonHazeHeight = 0.25f; horizonHazeFalloff = 1.5f;
            cloudLayer2Coverage = 0.7f; cloudLayer2Scale = 6f; cloudLayer2Speed = 1.2f; cloudLayer2Opacity = 0.6f; cloudLayer2Height = 0.1f;
            volumeInfluence = 1.0f; // fully override TOD volume during a storm
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
            horizonHazeStrength = 0.9f; horizonHazeColor = new Color(0.75f, 0.78f, 0.82f, 1f); horizonHazeHeight = 0.3f; horizonHazeFalloff = 1f;
            cloudLayer2Coverage = 0.2f; cloudLayer2Scale = 9f; cloudLayer2Speed = 0.2f; cloudLayer2Opacity = 0.25f; cloudLayer2Height = 0.15f;
            volumeInfluence = 0.90f;
        }

        [ContextMenu("Preset: Snow")]
        private void PresetSnow()
        {
            profileName = "Snow";
            cloudCoverageMin = 0.6f; cloudCoverageMax = 0.8f;
            cloudDensityMultiplier = 1.2f; cloudSharpnessMultiplier = 1.1f; cloudBrightness = 1.1f; cloudDarkness = 0.3f;
            cloudColor = new Color(0.92f, 0.94f, 0.98f, 1f);
            cloudShadowColor = new Color(0.60f, 0.62f, 0.70f, 1f);
            cloudScaleMultiplier = 1f; cloudSpeedMultiplier = 0.8f;
            fogDensityMultiplier = 1.5f; fogColorTint = new Color(0.85f, 0.88f, 0.95f, 1f); overrideFogColor = false; overrideFogMode = false;
            sunIntensityMultiplier = 0.5f; moonIntensityMultiplier = 0.6f;
            ambientIntensityMultiplier = 0.7f; ambientColorTint = new Color(0.90f, 0.92f, 0.98f, 1f);
            bloomIntensity = 0.6f; bloomThreshold = 0.9f; vignetteIntensity = 0.2f;
            colorAdjustmentExposure = 0.1f; colorAdjustmentContrast = -5f; colorAdjustmentSaturation = -20f;
            windDirection = new Vector3(0.8f, 0f, 0.2f); windSpeed = 0.8f;
            precipitationType = PrecipitationType.Snow; precipitationIntensity = 0.7f;
            dayAtmosphereMultiplier = 0.6f; horizonGlowMultiplier = 0.4f; starVisibilityMultiplier = 0.1f;
            horizonHazeStrength = 0.5f; horizonHazeColor = new Color(0.88f, 0.90f, 0.95f, 1f); horizonHazeHeight = 0.18f; horizonHazeFalloff = 2.5f;
            cloudLayer2Coverage = 0.3f; cloudLayer2Scale = 8f; cloudLayer2Speed = 0.4f; cloudLayer2Opacity = 0.35f; cloudLayer2Height = 0.1f;
            volumeInfluence = 0.6f;
        }
    }
}
