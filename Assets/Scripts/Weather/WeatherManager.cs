using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton MonoBehaviour that drives the weather system.
/// Assign weather profiles in the Inspector, then call SetWeather() or enable autoWeather.
/// The manager lerps all shader, fog, ambient, light, and URP Volume properties between profiles.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    // ─── INSPECTOR FIELDS ────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Reference to the scene DayNightCycle component")]
    public DayNightCycle dayNightCycle;

    [Header("Weather Profiles")]
    [Tooltip("All available weather profiles that can be assigned or randomly selected")]
    public Weather.WeatherProfile[] weatherProfiles;

    [Tooltip("The currently active weather profile (applied on Start if assigned)")]
    public Weather.WeatherProfile currentWeather;

    [Header("Transition")]
    [Tooltip("Duration in seconds to smoothly blend between weather states")]
    public float transitionDuration = 10f;

    [Header("Auto Weather")]
    [Tooltip("Automatically cycle through random weather conditions over time")]
    public bool autoWeather = false;

    [Tooltip("Minimum real-time seconds before auto-weather picks a new condition")]
    public float minTimeBetweenChanges = 30f;

    [Tooltip("Maximum real-time seconds before auto-weather picks a new condition")]
    public float maxTimeBetweenChanges = 120f;

    [Header("Debug")]
    [Tooltip("Press in Play mode to manually trigger a random weather change")]
    [SerializeField] private bool _debugForceRandomWeather = false;

    // ─── PRIVATE STATE ───────────────────────────────────────────────

    private Weather.WeatherProfile _sourceWeather;
    private Weather.WeatherProfile _targetWeather;
    private float _transitionProgress = 1f;
    private float _fromCoverage = 0f;
    private float _toCoverage = 0f;
    private float _fromCoverage2 = 0f;
    private float _toCoverage2 = 0f;

    private float _autoWeatherTimer = 0f;

    private Material _skyboxMaterial;
    private Volume _weatherVolume;
    private VolumeProfile _runtimeProfile;
    private Bloom _bloom;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    // Base cloud values cached from the material at Start so weather profiles
    // can multiply them rather than override them entirely.
    private float _baseCloudScale     = 5f;
    private float _baseCloudSpeed     = 0.3f;
    private float _baseCloudDensity   = 1f;
    private float _baseCloudSharpness = 1.5f;

    // Base Cloud Layer 2 values cached from the material at Start.
    private float _baseCloud2Scale     = 8f;
    private float _baseCloud2Speed     = 0.15f;
    private float _baseCloud2Density   = 0.8f;
    private float _baseCloud2Sharpness = 2f;

    // Current lerped volume influence (controls _weatherVolume.weight)
    private float _currentVolumeInfluence = 0f;

    // ─── LIFECYCLE ───────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Resolve skybox material from DayNightCycle, then from RenderSettings
        if (dayNightCycle != null)
            _skyboxMaterial = dayNightCycle.skyboxMaterial;
        if (_skyboxMaterial == null)
            _skyboxMaterial = RenderSettings.skybox;

        // Cache the material's authored base values so weather profiles can
        // multiply them rather than overriding them with their own absolute values.
        if (_skyboxMaterial != null)
        {
            _baseCloudScale     = _skyboxMaterial.GetFloat("_CloudScale");
            _baseCloudSpeed     = _skyboxMaterial.GetFloat("_CloudSpeed");
            _baseCloudDensity   = _skyboxMaterial.GetFloat("_CloudDensity");
            _baseCloudSharpness = _skyboxMaterial.GetFloat("_CloudSharpness");

            if (_skyboxMaterial.HasProperty("_Cloud2Scale"))
                _baseCloud2Scale = _skyboxMaterial.GetFloat("_Cloud2Scale");
            if (_skyboxMaterial.HasProperty("_Cloud2Speed"))
                _baseCloud2Speed = _skyboxMaterial.GetFloat("_Cloud2Speed");
            if (_skyboxMaterial.HasProperty("_Cloud2Density"))
                _baseCloud2Density = _skyboxMaterial.GetFloat("_Cloud2Density");
            if (_skyboxMaterial.HasProperty("_Cloud2Sharpness"))
                _baseCloud2Sharpness = _skyboxMaterial.GetFloat("_Cloud2Sharpness");
        }

        SetupVolume();

        if (currentWeather != null)
        {
            _sourceWeather = currentWeather;
            _targetWeather = currentWeather;
            _fromCoverage = Random.Range(currentWeather.cloudCoverageMin, currentWeather.cloudCoverageMax);
            _toCoverage = _fromCoverage;
            _fromCoverage2 = Random.Range(currentWeather.cloud2CoverageMin, currentWeather.cloud2CoverageMax);
            _toCoverage2 = _fromCoverage2;
            _transitionProgress = 1f;
            ApplyWeatherLerp(currentWeather, currentWeather, 1f);
        }
        else
        {
            Debug.LogWarning("[WeatherManager] No starting weather profile assigned — using material/scene defaults.");
        }

        _autoWeatherTimer = Random.Range(minTimeBetweenChanges, maxTimeBetweenChanges);
    }

    void Update()
    {
        // Smooth transition
        if (_transitionProgress < 1f)
        {
            _transitionProgress += Time.deltaTime / Mathf.Max(0.01f, transitionDuration);
            _transitionProgress = Mathf.Clamp01(_transitionProgress);
            ApplyWeatherLerp(_sourceWeather, _targetWeather, _transitionProgress);
        }

        // Keep the volume weight in sync every frame (volume influence can
        // change during transitions and the weight must reflect the current blend).
        if (_weatherVolume != null)
            _weatherVolume.weight = _currentVolumeInfluence;

        // Auto weather cycling
        if (autoWeather && weatherProfiles != null && weatherProfiles.Length > 1)
        {
            _autoWeatherTimer -= Time.deltaTime;
            if (_autoWeatherTimer <= 0f)
            {
                PickRandomWeather();
                _autoWeatherTimer = Random.Range(minTimeBetweenChanges, maxTimeBetweenChanges);
            }
        }

        // Debug button
        if (_debugForceRandomWeather)
        {
            _debugForceRandomWeather = false;
            PickRandomWeather();
        }
    }

    void OnDestroy()
    {
        if (_weatherVolume != null)
            Destroy(_weatherVolume.gameObject);
        if (_runtimeProfile != null)
            Destroy(_runtimeProfile);
    }

    // ─── PUBLIC API ──────────────────────────────────────────────────

    /// <summary>Transitions to the given weather profile over transitionDuration seconds.</summary>
    public void SetWeather(Weather.WeatherProfile profile)
    {
        if (profile == null) return;

        // Capture the current rendered cloud coverage as transition start
        _fromCoverage = (_skyboxMaterial != null)
            ? _skyboxMaterial.GetFloat("_CloudCoverage")
            : (_sourceWeather != null ? _fromCoverage : 0f);

        _fromCoverage2 = (_skyboxMaterial != null && _skyboxMaterial.HasProperty("_Cloud2Coverage"))
            ? _skyboxMaterial.GetFloat("_Cloud2Coverage")
            : (_sourceWeather != null ? _fromCoverage2 : 0f);

        _sourceWeather = currentWeather ?? profile;
        _targetWeather = profile;
        currentWeather = profile;

        // Pick a random target coverage within the new profile's diversity range
        _toCoverage = Random.Range(profile.cloudCoverageMin, profile.cloudCoverageMax);
        _toCoverage2 = Random.Range(profile.cloud2CoverageMin, profile.cloud2CoverageMax);

        _transitionProgress = 0f;
    }

    /// <summary>Transitions to a weather profile that matches the given name.</summary>
    public void SetWeatherByName(string name)
    {
        if (weatherProfiles == null) return;
        foreach (var p in weatherProfiles)
        {
            if (p != null && p.profileName == name)
            {
                SetWeather(p);
                return;
            }
        }
        Debug.LogWarning($"[WeatherManager] No weather profile named '{name}' found.");
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────

    private void PickRandomWeather()
    {
        if (weatherProfiles == null || weatherProfiles.Length == 0) return;

        Weather.WeatherProfile next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
        int attempts = 0;
        while (next == currentWeather && weatherProfiles.Length > 1 && attempts < 10)
        {
            next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
            attempts++;
        }
        SetWeather(next);
    }

    private void SetupVolume()
    {
        // Create a dedicated WeatherVolume child so it doesn't disturb existing scene volumes
        GameObject volumeGO = new GameObject("WeatherVolume");
        volumeGO.transform.SetParent(transform, false);

        _weatherVolume = volumeGO.AddComponent<Volume>();
        _weatherVolume.isGlobal = true;
        // Priority 1 — above DayNightVolumeController (priority 0).
        // Weight is driven by the active profile's volumeInfluence:
        //   0 = clear sky, DayNightVolumeController fully controls post-processing.
        //   1 = severe weather, this volume fully overrides the TOD effects.
        _weatherVolume.priority = 1f;
        _weatherVolume.weight   = 0f;

        _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        _weatherVolume.profile = _runtimeProfile;

        _bloom = _runtimeProfile.Add<Bloom>(true);
        _vignette = _runtimeProfile.Add<Vignette>(true);
        _colorAdjustments = _runtimeProfile.Add<ColorAdjustments>(true);
    }

    /// <summary>
    /// Lerps all weather properties between 'from' and 'to' at blend factor t (0=from, 1=to).
    /// </summary>
    private void ApplyWeatherLerp(Weather.WeatherProfile from, Weather.WeatherProfile to, float t)
    {
        if (to == null) return;
        if (from == null) from = to;

        // ── Cloud coverage (captured random values, no per-frame jitter)
        float cloudCoverage = Mathf.Lerp(_fromCoverage, _toCoverage, t);
        float cloud2Coverage = Mathf.Lerp(_fromCoverage2, _toCoverage2, t);

        // ── Apply skybox material overrides
        if (_skyboxMaterial != null)
        {
            _skyboxMaterial.SetFloat("_CloudCoverage", cloudCoverage);

            // Scale/Speed/Density/Sharpness use MULTIPLIERS on the material's base values
            // so the designer's global settings remain the source of truth.
            _skyboxMaterial.SetFloat("_CloudDensity",
                _baseCloudDensity   * Mathf.Lerp(from.cloudDensityMultiplier,   to.cloudDensityMultiplier,   t));
            _skyboxMaterial.SetFloat("_CloudSharpness",
                _baseCloudSharpness * Mathf.Lerp(from.cloudSharpnessMultiplier, to.cloudSharpnessMultiplier, t));
            _skyboxMaterial.SetFloat("_CloudScale",
                _baseCloudScale     * Mathf.Lerp(from.cloudScaleMultiplier,     to.cloudScaleMultiplier,     t));

            _skyboxMaterial.SetFloat("_CloudBrightness", Mathf.Lerp(from.cloudBrightness, to.cloudBrightness, t));
            _skyboxMaterial.SetFloat("_CloudDarkness",   Mathf.Lerp(from.cloudDarkness,   to.cloudDarkness,   t));
            _skyboxMaterial.SetColor("_CloudColor",      Color.Lerp(from.cloudColor,      to.cloudColor,      t));
            _skyboxMaterial.SetColor("_CloudShadowColor", Color.Lerp(from.cloudShadowColor, to.cloudShadowColor, t));

            _skyboxMaterial.SetFloat("_CloudEdgeSoftness",
                Mathf.Lerp(from.cloudEdgeSoftness, to.cloudEdgeSoftness, t));
            _skyboxMaterial.SetFloat("_CloudVariation",
                Mathf.Lerp(from.cloudVariation, to.cloudVariation, t));

            // Wind direction (magnitude = windSpeed for smooth interpolation;
            // the shader normalises the vector itself, so only direction matters at render time)
            Vector3 windDir = Vector3.Lerp(
                from.windDirection.normalized * from.windSpeed,
                to.windDirection.normalized   * to.windSpeed,
                t);
            _skyboxMaterial.SetVector("_CloudDirection", new Vector4(windDir.x, windDir.y, windDir.z, 0f));

            // Cloud speed scalar for the shader's time-based scroll
            _skyboxMaterial.SetFloat("_CloudSpeed",
                _baseCloudSpeed * Mathf.Lerp(from.cloudSpeedMultiplier, to.cloudSpeedMultiplier, t));

            // Atmosphere overrides
            _skyboxMaterial.SetFloat("_DayAtmosphereStrength",
                Mathf.Lerp(from.dayAtmosphereMultiplier, to.dayAtmosphereMultiplier, t));
            _skyboxMaterial.SetFloat("_HorizonGlowStrength",
                Mathf.Lerp(from.horizonGlowMultiplier, to.horizonGlowMultiplier, t));

            // Star visibility (base StarBrightness is 1.2 — multiply by weather factor)
            _skyboxMaterial.SetFloat("_StarBrightness",
                1.2f * Mathf.Lerp(from.starVisibilityMultiplier, to.starVisibilityMultiplier, t));

            // Horizon Haze (WeatherManager is the SOLE controller — DayNightCycle does NOT touch these)
            _skyboxMaterial.SetFloat("_HorizonHazeStrength",
                Mathf.Lerp(from.horizonHazeStrength, to.horizonHazeStrength, t));
            _skyboxMaterial.SetFloat("_HorizonHazeHeight",
                Mathf.Lerp(from.horizonHazeHeight, to.horizonHazeHeight, t));
            _skyboxMaterial.SetFloat("_HorizonHazeFalloff",
                Mathf.Lerp(from.horizonHazeFalloff, to.horizonHazeFalloff, t));

            // Cloud Layer 2 — higher altitude, uses _Cloud2* shader properties
            _skyboxMaterial.SetFloat("_Cloud2Coverage", cloud2Coverage);
            _skyboxMaterial.SetFloat("_Cloud2Density",   _baseCloud2Density   * Mathf.Lerp(from.cloud2DensityMultiplier,   to.cloud2DensityMultiplier,   t));
            _skyboxMaterial.SetFloat("_Cloud2Sharpness", _baseCloud2Sharpness * Mathf.Lerp(from.cloud2SharpnessMultiplier, to.cloud2SharpnessMultiplier, t));
            _skyboxMaterial.SetFloat("_Cloud2Scale",     _baseCloud2Scale     * Mathf.Lerp(from.cloud2ScaleMultiplier,     to.cloud2ScaleMultiplier,     t));
            _skyboxMaterial.SetFloat("_Cloud2Speed",     _baseCloud2Speed     * Mathf.Lerp(from.cloud2SpeedMultiplier,     to.cloud2SpeedMultiplier,     t));
            _skyboxMaterial.SetFloat("_Cloud2Brightness", Mathf.Lerp(from.cloud2Brightness, to.cloud2Brightness, t));
            _skyboxMaterial.SetFloat("_Cloud2Darkness",   Mathf.Lerp(from.cloud2Darkness,   to.cloud2Darkness,   t));
            _skyboxMaterial.SetColor("_Cloud2Color",       Color.Lerp(from.cloud2Color,       to.cloud2Color,       t));
            _skyboxMaterial.SetColor("_Cloud2ShadowColor", Color.Lerp(from.cloud2ShadowColor, to.cloud2ShadowColor, t));
        }

        // ── Apply DayNightCycle multipliers
        if (dayNightCycle != null)
        {
            dayNightCycle.SetSunIntensityMultiplier(
                Mathf.Lerp(from.sunIntensityMultiplier, to.sunIntensityMultiplier, t));
            dayNightCycle.SetMoonIntensityMultiplier(
                Mathf.Lerp(from.moonIntensityMultiplier, to.moonIntensityMultiplier, t));
            dayNightCycle.SetAmbientMultiplier(
                Mathf.Lerp(from.ambientIntensityMultiplier, to.ambientIntensityMultiplier, t));
            dayNightCycle.SetAmbientColorTint(
                Color.Lerp(from.ambientColorTint, to.ambientColorTint, t));
            dayNightCycle.SetFogMultiplier(
                Mathf.Lerp(from.fogDensityMultiplier, to.fogDensityMultiplier, t));

            // Fog colour override — smoothly cross-fade between the two fog colours.
            // If either profile wants to override the fog colour, enable the override for the
            // duration of the transition so there's no hard snap at the 50% mark.
            bool eitherOverrides = from.overrideFogColor || to.overrideFogColor;
            Color fogTint = Color.Lerp(from.fogColorTint, to.fogColorTint, t);
            dayNightCycle.SetFogColorOverride(fogTint, eitherOverrides);

            // Fog mode override — switch to target profile's fog mode at the midpoint
            bool useFogMode = (t >= 0.5f) ? to.overrideFogMode : from.overrideFogMode;
            if (useFogMode)
            {
                FogMode mode = (t >= 0.5f) ? to.fogMode : from.fogMode;
                RenderSettings.fogMode = mode;
            }
        }

        // ── Volume influence: smoothstep easing avoids a perceptible snap when going
        // from 0 influence (clear weather) to > 0 (stormy weather).
        _currentVolumeInfluence = Mathf.SmoothStep(from.volumeInfluence, to.volumeInfluence, t);

        // ── Apply URP Volume overrides (these take effect when weight > 0)
        if (_bloom != null)
        {
            _bloom.intensity.Override(Mathf.Lerp(from.bloomIntensity, to.bloomIntensity, t));
            _bloom.threshold.Override(Mathf.Lerp(from.bloomThreshold, to.bloomThreshold, t));
        }

        if (_vignette != null)
            _vignette.intensity.Override(Mathf.Lerp(from.vignetteIntensity, to.vignetteIntensity, t));

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.Override(
                Mathf.Lerp(from.colorAdjustmentExposure, to.colorAdjustmentExposure, t));
            _colorAdjustments.contrast.Override(
                Mathf.Lerp(from.colorAdjustmentContrast, to.colorAdjustmentContrast, t));
            _colorAdjustments.saturation.Override(
                Mathf.Lerp(from.colorAdjustmentSaturation, to.colorAdjustmentSaturation, t));
        }
    }
}
