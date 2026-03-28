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

    private float _autoWeatherTimer = 0f;

    private Material _skyboxMaterial;
    private Volume _weatherVolume;
    private VolumeProfile _runtimeProfile;
    private Bloom _bloom;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

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

        SetupVolume();

        if (currentWeather != null)
        {
            _sourceWeather = currentWeather;
            _targetWeather = currentWeather;
            _fromCoverage = Random.Range(currentWeather.cloudCoverageMin, currentWeather.cloudCoverageMax);
            _toCoverage = _fromCoverage;
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

        _sourceWeather = currentWeather ?? profile;
        _targetWeather = profile;
        currentWeather = profile;

        // Pick a random target coverage within the new profile's diversity range
        _toCoverage = Random.Range(profile.cloudCoverageMin, profile.cloudCoverageMax);

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
        _weatherVolume.priority = 1f; // Higher priority than the default scene volume (priority 0)

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

        // ── Apply skybox material overrides
        if (_skyboxMaterial != null)
        {
            _skyboxMaterial.SetFloat("_CloudCoverage", cloudCoverage);
            _skyboxMaterial.SetFloat("_CloudDensity",   Mathf.Lerp(from.cloudDensity,   to.cloudDensity,   t));
            _skyboxMaterial.SetFloat("_CloudSharpness", Mathf.Lerp(from.cloudSharpness, to.cloudSharpness, t));
            _skyboxMaterial.SetFloat("_CloudBrightness", Mathf.Lerp(from.cloudBrightness, to.cloudBrightness, t));
            _skyboxMaterial.SetFloat("_CloudDarkness",  Mathf.Lerp(from.cloudDarkness,  to.cloudDarkness,  t));
            _skyboxMaterial.SetFloat("_CloudScale",     Mathf.Lerp(from.cloudScale,     to.cloudScale,     t));
            _skyboxMaterial.SetFloat("_CloudSpeed",     Mathf.Lerp(from.cloudSpeed,     to.cloudSpeed,     t));
            _skyboxMaterial.SetColor("_CloudColor",     Color.Lerp(from.cloudColor,     to.cloudColor,     t));
            _skyboxMaterial.SetColor("_CloudShadowColor", Color.Lerp(from.cloudShadowColor, to.cloudShadowColor, t));

            // Wind direction (normalised for cloud scrolling)
            Vector3 windDir = Vector3.Lerp(
                from.windDirection.normalized * from.windSpeed,
                to.windDirection.normalized * to.windSpeed,
                t);
            _skyboxMaterial.SetVector("_CloudDirection", new Vector4(windDir.x, windDir.y, windDir.z, 0f));

            // Atmosphere overrides
            _skyboxMaterial.SetFloat("_DayAtmosphereStrength",
                Mathf.Lerp(from.dayAtmosphereMultiplier, to.dayAtmosphereMultiplier, t));
            _skyboxMaterial.SetFloat("_HorizonGlowStrength",
                Mathf.Lerp(from.horizonGlowMultiplier, to.horizonGlowMultiplier, t));

            // Star visibility (base StarBrightness is 1.2 — multiply by weather factor)
            _skyboxMaterial.SetFloat("_StarBrightness",
                1.2f * Mathf.Lerp(from.starVisibilityMultiplier, to.starVisibilityMultiplier, t));
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

            // Fog colour override — use target state to decide whether to override
            bool overrideFog = (t >= 0.5f) ? to.overrideFogColor : from.overrideFogColor;
            Color fogTint = Color.Lerp(from.fogColorTint, to.fogColorTint, t);
            dayNightCycle.SetFogColorOverride(fogTint, overrideFog);
        }

        // ── Apply URP Volume overrides
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
