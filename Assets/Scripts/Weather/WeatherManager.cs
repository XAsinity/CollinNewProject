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
    public float minTimeBetweenChanges = 60f;

    [Tooltip("Maximum real-time seconds before auto-weather picks a new condition")]
    public float maxTimeBetweenChanges = 180f;

    [Tooltip("When the active weather profile's max cloud coverage exceeds this value, " +
             "auto-weather will only pick profiles with similarly high coverage so stormy " +
             "conditions are not abruptly replaced by clear skies. Set to 1 to disable biasing.")]
    [Range(0f, 1f)]
    public float autoWeatherCloudBiasThreshold = 0.6f;

    [Header("Debug")]
    [Tooltip("Press in Play mode to manually trigger a random weather change")]
    [SerializeField] private bool _debugForceRandomWeather = false;

    [Header("Auto Refresh")]
    [Tooltip("When enabled, the active weather profile's Inspector values are pushed to the shader " +
             "every frame while no transition is running. This means edits made to a WeatherProfile " +
             "asset in the Inspector are immediately visible in the scene without needing to " +
             "re-trigger a weather transition.\n\n" +
             "The per-frame cost is very small (a few dozen SetFloat/SetColor calls), but you can " +
             "disable this in shipped builds via code or toggle it off in the Inspector if every " +
             "CPU cycle matters.")]
    public bool autoRefreshProfile = true;

    // ─── PRIVATE STATE ───────────────────────────────────────────────

    private Weather.WeatherProfile _sourceWeather;
    private Weather.WeatherProfile _targetWeather;
    private float _transitionProgress = 1f;
    private float _fromCoverage = 0f;
    private float _toCoverage = 0f;
    private float _fromCoverage2 = 0f;
    private float _toCoverage2 = 0f;

    // Directional dissolve offset — accumulates during transitions so departing
    // clouds appear to roll away in the wind direction rather than fading in place.
    private Vector4 _dissolveOffset = Vector4.zero;

    private float _autoWeatherTimer = 0f;

    // Bug 3 fix: prevents the auto-weather timer from overriding a manually set weather.
    // Set to true when SetWeather() is called externally; cleared by UnlockWeather().
    private bool _weatherLocked = false;

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

    // ─── CLOUD SPEED SMOOTHING ────────────────────────────────────────
    // SmoothDamp prevents abrupt cloud speed changes during weather transitions.
    // Speed ramps naturally over cloudSpeedSmoothTime seconds instead of snapping.
    [SerializeField] private float cloudSpeedSmoothTime = 3f;
    private float _currentCloudSpeed;
    private float _cloudSpeedVelocity;
    private float _currentCloud2Speed;
    private float _cloud2SpeedVelocity;

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

        // Initialise the smoothed speed trackers to the starting profile's target speed
        // so there is no ramp-up on the first frame.
        if (currentWeather != null)
        {
            _currentCloudSpeed  = _baseCloudSpeed  * currentWeather.cloudSpeedMultiplier  + currentWeather.windSpeedBoost;
            _currentCloud2Speed = _baseCloud2Speed * currentWeather.cloud2SpeedMultiplier + currentWeather.windSpeedBoost;
        }
        else
        {
            _currentCloudSpeed  = _baseCloudSpeed;
            _currentCloud2Speed = _baseCloud2Speed;
        }

        SetupVolume();

        // Bug 2 fix: Always force a full clear-sky baseline on play start so the first
        // frame never inherits stale storm/fog shader values from a previous run.
        // If a starting weather profile is assigned, we then transition into it.
        if (_skyboxMaterial != null)
        {
            _skyboxMaterial.SetFloat("_CloudCoverage",     0f);
            _skyboxMaterial.SetFloat("_Cloud2Coverage",    0f);
            _skyboxMaterial.SetFloat("_CloudDensity",      _baseCloudDensity);
            _skyboxMaterial.SetFloat("_CloudSharpness",    _baseCloudSharpness);
            _skyboxMaterial.SetFloat("_CloudScale",        _baseCloudScale);
            _skyboxMaterial.SetFloat("_CloudSpeed",        _baseCloudSpeed);
            _skyboxMaterial.SetFloat("_CloudEdgeSoftness", 0.35f);
            _skyboxMaterial.SetFloat("_CloudVariation",    0.5f);
            _skyboxMaterial.SetVector("_CloudDirection",   new Vector4(1f, 0f, 0.5f, 0f));
            _skyboxMaterial.SetFloat("_CloudBrightness",   1f);
            _skyboxMaterial.SetFloat("_CloudDarkness",     0.3f);
            _skyboxMaterial.SetColor("_CloudColor",        new Color(0.95f, 0.95f, 0.95f, 1f));
            _skyboxMaterial.SetColor("_CloudShadowColor",  new Color(0.35f, 0.35f, 0.40f, 1f));
            _skyboxMaterial.SetFloat("_Cloud2Density",     _baseCloud2Density);
            _skyboxMaterial.SetFloat("_Cloud2Sharpness",   _baseCloud2Sharpness);
            _skyboxMaterial.SetFloat("_Cloud2Scale",       _baseCloud2Scale);
            _skyboxMaterial.SetFloat("_Cloud2Speed",       _baseCloud2Speed);
            _skyboxMaterial.SetFloat("_Cloud2Brightness",  1f);
            _skyboxMaterial.SetFloat("_Cloud2Darkness",    0.3f);
            _skyboxMaterial.SetColor("_Cloud2Color",       new Color(0.96f, 0.96f, 0.98f, 1f));
            _skyboxMaterial.SetColor("_Cloud2ShadowColor", new Color(0.50f, 0.52f, 0.58f, 1f));
            _skyboxMaterial.SetFloat("_Cloud2Opacity",     0.3f);
            _skyboxMaterial.SetFloat("_DayAtmosphereStrength", 1f);
            _skyboxMaterial.SetFloat("_HorizonGlowStrength",   1f);
            _skyboxMaterial.SetFloat("_HorizonHazeStrength",   0.15f);
            _skyboxMaterial.SetFloat("_HorizonHazeHeight",     0.1f);
            _skyboxMaterial.SetFloat("_HorizonHazeFalloff",    4f);
            _skyboxMaterial.SetFloat("_StarBrightness",         1.2f);
            _skyboxMaterial.SetVector("_CloudDissolveOffset", Vector4.zero);
            _skyboxMaterial.SetFloat("_CloudCurlStrength",    0.6f);
            _skyboxMaterial.SetFloat("_CloudCurlScale",       3.0f);
            _skyboxMaterial.SetFloat("_CloudElevationCompress", 1.2f);
        }
        if (dayNightCycle != null)
        {
            dayNightCycle.SetSunIntensityMultiplier(1f);
            dayNightCycle.SetMoonIntensityMultiplier(1f);
            dayNightCycle.SetAmbientMultiplier(1f);
            dayNightCycle.SetAmbientColorTint(Color.white);
            dayNightCycle.SetFogMultiplier(1f);
            dayNightCycle.SetFogColorOverride(Color.white, false);
        }
        _currentVolumeInfluence = 0f;
        if (_weatherVolume != null)
            _weatherVolume.weight = 0f;
        _fromCoverage  = 0f;
        _fromCoverage2 = 0f;

        if (currentWeather != null)
        {
            // Delay one frame so the clear sky renders first, then transition in.
            Weather.WeatherProfile startProfile = currentWeather;
            StartCoroutine(ApplyInitialWeatherDelayed(startProfile));
        }
        else
        {
            Debug.LogWarning("[WeatherManager] No starting weather profile assigned — applying clear-sky defaults.");
            // Apply a sensible clear-sky default so the first frame is not garbage.
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetFloat("_CloudDensity",     _baseCloudDensity);
                _skyboxMaterial.SetFloat("_CloudSharpness",   _baseCloudSharpness);
                _skyboxMaterial.SetFloat("_CloudScale",       _baseCloudScale);
                _skyboxMaterial.SetFloat("_CloudSpeed",       _baseCloudSpeed);
                _skyboxMaterial.SetFloat("_CloudEdgeSoftness", 0.35f);
                _skyboxMaterial.SetFloat("_CloudVariation",   0.5f);
                _skyboxMaterial.SetVector("_CloudDirection",  new Vector4(1f, 0f, 0.5f, 0f));
                _skyboxMaterial.SetFloat("_CloudBrightness",  1f);
                _skyboxMaterial.SetFloat("_CloudDarkness",    0.3f);
                _skyboxMaterial.SetColor("_CloudColor",       new Color(0.95f, 0.95f, 0.95f, 1f));
                _skyboxMaterial.SetColor("_CloudShadowColor", new Color(0.35f, 0.35f, 0.40f, 1f));
                _skyboxMaterial.SetFloat("_Cloud2Density",    _baseCloud2Density);
                _skyboxMaterial.SetFloat("_Cloud2Sharpness",  _baseCloud2Sharpness);
                _skyboxMaterial.SetFloat("_Cloud2Scale",      _baseCloud2Scale);
                _skyboxMaterial.SetFloat("_Cloud2Speed",      _baseCloud2Speed);
                _skyboxMaterial.SetFloat("_Cloud2Brightness", 1f);
                _skyboxMaterial.SetFloat("_Cloud2Darkness",   0.3f);
                _skyboxMaterial.SetColor("_Cloud2Color",       new Color(0.96f, 0.96f, 0.98f, 1f));
                _skyboxMaterial.SetColor("_Cloud2ShadowColor", new Color(0.50f, 0.52f, 0.58f, 1f));
                _skyboxMaterial.SetFloat("_Cloud2Opacity",    0.3f);
                _skyboxMaterial.SetFloat("_DayAtmosphereStrength", 1f);
                _skyboxMaterial.SetFloat("_HorizonGlowStrength",   1f);
                _skyboxMaterial.SetFloat("_HorizonHazeStrength",   0.15f);
                _skyboxMaterial.SetFloat("_HorizonHazeHeight",     0.1f);
                _skyboxMaterial.SetFloat("_HorizonHazeFalloff",    4f);
                _skyboxMaterial.SetFloat("_StarBrightness",        1.2f);
                _skyboxMaterial.SetFloat("_CloudCurlStrength",    0.6f);
                _skyboxMaterial.SetFloat("_CloudCurlScale",       3.0f);
                _skyboxMaterial.SetFloat("_CloudElevationCompress", 1.2f);
            }
        }

        _autoWeatherTimer = Random.Range(minTimeBetweenChanges, maxTimeBetweenChanges);
    }

    void Update()
    {
        // Smooth transition
        if (_transitionProgress < 1f)
        {
            // Accumulate directional dissolve offset from the departing weather's storm roll speed.
            // The offset shifts cloud noise UVs in the source profile's wind direction, making the
            // departing storm appear to roll away rather than uniformly fading.
            if (_sourceWeather != null && _sourceWeather.stormRollSpeed > 0f)
            {
                Vector3 windDir = _sourceWeather.windDirection.normalized;
                _dissolveOffset.x += windDir.x * _sourceWeather.stormRollSpeed * Time.deltaTime;
                _dissolveOffset.y += windDir.z * _sourceWeather.stormRollSpeed * Time.deltaTime;
            }

            _transitionProgress += Time.deltaTime / Mathf.Max(0.01f, transitionDuration);
            _transitionProgress = Mathf.Clamp01(_transitionProgress);
            ApplyWeatherLerp(_sourceWeather, _targetWeather, _transitionProgress);
        }
        else
        {
            // Auto-refresh: re-apply the active profile every frame so any Inspector edits
            // to the WeatherProfile asset are immediately visible without triggering a
            // new transition. Coverage values are kept stable (no random re-roll).
            if (autoRefreshProfile && currentWeather != null)
                ApplyWeatherLerp(currentWeather, currentWeather, 1f);

            // Gradually decay the dissolve offset back to zero after the transition completes
            // so the cloud pattern returns to its normal steady-state position.
            if (_dissolveOffset.sqrMagnitude > 0.0001f)
            {
                _dissolveOffset = Vector4.Lerp(_dissolveOffset, Vector4.zero, Time.deltaTime * 0.5f);
                if (_dissolveOffset.sqrMagnitude < 0.0001f)
                    _dissolveOffset = Vector4.zero;
                if (_skyboxMaterial != null)
                    _skyboxMaterial.SetVector("_CloudDissolveOffset", _dissolveOffset);
            }
        }

        // Keep the volume weight in sync every frame (volume influence can
        // change during transitions and the weight must reflect the current blend).
        if (_weatherVolume != null)
            _weatherVolume.weight = _currentVolumeInfluence;

        // Bug 3 fix: auto-weather cycling is skipped when weather is locked by an external
        // SetWeather() call, so manually chosen conditions are never overridden automatically.
        if (autoWeather && !_weatherLocked && weatherProfiles != null && weatherProfiles.Length > 1)
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

    /// <summary>Transitions to the given weather profile over transitionDuration seconds.
    /// Locks weather so the auto-cycling timer will not override this selection.
    /// Call UnlockWeather() to re-enable automatic cycling.</summary>
    public void SetWeather(Weather.WeatherProfile profile)
    {
        _weatherLocked = true;
        SetWeatherInternal(profile);
    }

    /// <summary>Prevents the auto-weather timer from overriding the current weather.
    /// Call this after SetWeather() if you want to pin the weather indefinitely and
    /// have not already done so via the public SetWeather() call.</summary>
    public void LockWeather() => _weatherLocked = true;

    /// <summary>Re-enables automatic weather cycling after a manual SetWeather() call.</summary>
    public void UnlockWeather() => _weatherLocked = false;

    // Internal transition that does NOT set the weather lock — used by auto-cycling
    // and the startup coroutine so those paths don't permanently block auto-weather.
    private void SetWeatherInternal(Weather.WeatherProfile profile)
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

        // Reset the dissolve offset at the start of each transition so each
        // storm departure begins from a neutral scroll position.
        _dissolveOffset = Vector4.zero;

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

    /// <summary>
    /// Immediately re-applies the active weather profile to the skybox material without
    /// starting a new transition. Useful when a WeatherProfile asset has been edited in
    /// the Inspector and you want the changes reflected instantly.
    /// Cloud coverage values are preserved — no random re-roll occurs.
    /// </summary>
    public void RefreshCurrentWeather()
    {
        if (currentWeather == null) return;
        // Lock in the current rendered coverage so there is no jump
        if (_skyboxMaterial != null)
        {
            _fromCoverage  = _skyboxMaterial.GetFloat("_CloudCoverage");
            _toCoverage    = _fromCoverage;
            if (_skyboxMaterial.HasProperty("_Cloud2Coverage"))
            {
                _fromCoverage2 = _skyboxMaterial.GetFloat("_Cloud2Coverage");
                _toCoverage2   = _fromCoverage2;
            }
        }
        ApplyWeatherLerp(currentWeather, currentWeather, 1f);
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────

    private void PickRandomWeather()
    {
        if (weatherProfiles == null || weatherProfiles.Length == 0) return;

        // Coverage bias: only enforce a cloudy->cloudy restriction when the current
        // weather is truly storm-like (active precipitation). This keeps transitions
        // natural during storms while still allowing all presets to cycle normally.
        float currentMaxCoverage = currentWeather != null ? currentWeather.cloudCoverageMax : 0f;
        bool requireCloudy = currentWeather != null &&
                             (currentWeather.precipitationIntensity > 0.05f ||
                              currentWeather.precipitationType != Weather.PrecipitationType.None) &&
                             currentMaxCoverage > autoWeatherCloudBiasThreshold;

        Weather.WeatherProfile next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
        int attempts = 0;
        while (attempts < 20)
        {
            bool differentFromCurrent = next != currentWeather || weatherProfiles.Length == 1;
            bool coverageOk = !requireCloudy || next.cloudCoverageMax > autoWeatherCloudBiasThreshold;
            if (differentFromCurrent && coverageOk) break;
            next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
            attempts++;
        }
        // Use internal overload so auto-cycling does not lock the weather lock flag.
        SetWeatherInternal(next);
    }

    /// <summary>
    /// Waits one frame (so the forced clear-sky state renders first) then kicks off
    /// the smooth transition from clear sky to the starting weather profile.
    /// Using the internal overload ensures the startup transition does not permanently
    /// lock the weather, allowing auto-cycling to take over once it completes.
    /// </summary>
    private System.Collections.IEnumerator ApplyInitialWeatherDelayed(Weather.WeatherProfile profile)
    {
        yield return null;
        // Use internal overload so the startup transition doesn't permanently lock weather.
        SetWeatherInternal(profile);
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

            // Cloud speed — use SmoothDamp so speed changes ramp naturally rather than
            // snapping on weather transitions (Issue 3 fix).
            float boost = Mathf.Lerp(from.windSpeedBoost, to.windSpeedBoost, t);
            float targetCloudSpeed = _baseCloudSpeed * Mathf.Lerp(from.cloudSpeedMultiplier, to.cloudSpeedMultiplier, t) + boost;
            _currentCloudSpeed = Mathf.SmoothDamp(_currentCloudSpeed, targetCloudSpeed, ref _cloudSpeedVelocity, cloudSpeedSmoothTime);
            _skyboxMaterial.SetFloat("_CloudSpeed", _currentCloudSpeed);

            // Atmosphere overrides
            _skyboxMaterial.SetFloat("_DayAtmosphereStrength",
                Mathf.Lerp(from.dayAtmosphereMultiplier, to.dayAtmosphereMultiplier, t));
            _skyboxMaterial.SetFloat("_HorizonGlowStrength",
                Mathf.Lerp(from.horizonGlowMultiplier, to.horizonGlowMultiplier, t));

            // Star visibility (base StarBrightness is 1.2 — multiply by weather factor)
            _skyboxMaterial.SetFloat("_StarBrightness",
                1.2f * Mathf.Lerp(from.starVisibilityMultiplier, to.starVisibilityMultiplier, t));

            // ─── HORIZON HAZE ──────────────────────────────────────────
            // WeatherManager is the SOLE controller of horizon haze.
            // DayNightCycle does NOT touch these properties.
            // Profile values are absolute (no base multiplier).
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

            // Cloud2 speed also uses SmoothDamp for smooth transitions (Issue 3 fix).
            float targetCloud2Speed = _baseCloud2Speed * Mathf.Lerp(from.cloud2SpeedMultiplier, to.cloud2SpeedMultiplier, t) + boost;
            _currentCloud2Speed = Mathf.SmoothDamp(_currentCloud2Speed, targetCloud2Speed, ref _cloud2SpeedVelocity, cloudSpeedSmoothTime);
            _skyboxMaterial.SetFloat("_Cloud2Speed", _currentCloud2Speed);
            _skyboxMaterial.SetFloat("_Cloud2Brightness", Mathf.Lerp(from.cloud2Brightness, to.cloud2Brightness, t));
            _skyboxMaterial.SetFloat("_Cloud2Darkness",   Mathf.Lerp(from.cloud2Darkness,   to.cloud2Darkness,   t));
            _skyboxMaterial.SetColor("_Cloud2Color",       Color.Lerp(from.cloud2Color,       to.cloud2Color,       t));
            _skyboxMaterial.SetColor("_Cloud2ShadowColor", Color.Lerp(from.cloud2ShadowColor, to.cloud2ShadowColor, t));
            _skyboxMaterial.SetFloat("_Cloud2Opacity",     Mathf.Lerp(from.cloud2Opacity,     to.cloud2Opacity,     t));

            // Directional dissolve offset — shifts cloud noise UVs so departing storms
            // appear to roll away in the wind direction rather than fading uniformly.
            _skyboxMaterial.SetVector("_CloudDissolveOffset", _dissolveOffset);

            // Cloud Curl Warp — lerp the three new curl noise properties
            _skyboxMaterial.SetFloat("_CloudCurlStrength",
                Mathf.Lerp(from.cloudCurlStrength, to.cloudCurlStrength, t));
            _skyboxMaterial.SetFloat("_CloudCurlScale",
                Mathf.Lerp(from.cloudCurlScale, to.cloudCurlScale, t));
            _skyboxMaterial.SetFloat("_CloudElevationCompress",
                Mathf.Lerp(from.cloudElevationCompress, to.cloudElevationCompress, t));
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
