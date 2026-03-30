using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton MonoBehaviour that drives the weather system.
/// Assign weather profiles in the Inspector, then call SetWeather() or enable autoWeather.
/// The manager lerps all shader, fog, ambient, light, and URP Volume properties between profiles.
/// Optionally assign a <see cref="Weather.WeatherPresetBundle"/> to use rule-based weather
/// sequences instead of pure random selection.
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

    [Header("Preset Bundle")]
    [Tooltip("Assign a WeatherPresetBundle to use rule-based weather transitions. " +
             "When assigned, this overrides the random auto-weather system with " +
             "realistic sequenced transitions. Leave empty to auto-generate a " +
             "default bundle at runtime.")]
    public Weather.WeatherPresetBundle presetBundle;

    [Header("Transition")]
    [Tooltip("Duration in seconds to smoothly blend between weather states (used when no bundle rule overrides it)")]
    public float transitionDuration = 120f;

    [Header("Auto Weather")]
    [Tooltip("Automatically cycle through random weather conditions over time")]
    public bool autoWeather = false;

    [Tooltip("Fallback minimum seconds before auto-weather picks a new condition. " +
             "Note: Per-profile duration overrides these global values when set on the WeatherProfile asset.")]
    public float minTimeBetweenChanges = 180f;

    [Tooltip("Fallback maximum seconds before auto-weather picks a new condition. " +
             "Note: Per-profile duration overrides these global values when set on the WeatherProfile asset.")]
    public float maxTimeBetweenChanges = 1500f;

    [Header("Debug")]
    [Tooltip("Press in Play mode to manually trigger a random weather change")]
    [SerializeField] private bool _debugForceRandomWeather = false;

    [Tooltip("When enabled, logs detailed cloud property values to the Console each frame during transitions " +
             "and once when a transition completes. Useful for diagnosing cloud speed, coverage, or visual issues.")]
    [SerializeField] private bool debugLogClouds = false;

    [Tooltip("How often to log cloud debug info during transitions (in seconds). Default: 2.0")]
    [SerializeField] private float debugLogInterval = 2.0f;

    [Header("Debug Time")]
    [Tooltip("Multiplies Time.deltaTime for weather transitions and auto-weather timer only. Does NOT affect Time.timeScale (physics, animations are unaffected).")]
    [Range(1f, 50f)]
    public float debugTimeScale = 1f;

    [Tooltip("When enabled, logs a message to the Console whenever debugTimeScale changes value.")]
    public bool debugLogTimeScale = false;

    [Header("Auto Refresh")]
    [Tooltip("When enabled, the active weather profile's Inspector values are pushed to the shader " +
             "every frame while no transition is running. This means edits made to a WeatherProfile " +
             "asset in the Inspector are immediately visible in the scene without needing to " +
             "re-trigger a weather transition.\n\n" +
             "The per-frame cost is very small (a few dozen SetFloat/SetColor calls), but you can " +
             "disable this in shipped builds via code or toggle it off in the Inspector if every " +
             "CPU cycle matters.")]
    public bool autoRefreshProfile = false;

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

    // Tracks the last profile reference seen so changes made in the Inspector during
    // Play mode are detected and trigger a proper transition via SetWeatherInternal().
    [System.NonSerialized]
    private Weather.WeatherProfile _lastKnownWeather;

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

    // Minimum cloud speed floor — prevents SmoothDamp from driving clouds to a
    // near-zero speed during profile cross-fades, which would make clouds appear frozen.
    private const float MIN_CLOUD_SPEED = 0.01f;

    // ─── ACTIVE TRANSITION DURATION ──────────────────────────────────
    // Stores the duration in use for the current transition. Bundle rules can override
    // this per-transition without touching the Inspector-serialised transitionDuration field.
    private float _activeTransitionDuration;

    // ─── AUTO-WEATHER BIAS COUNTER (REMOVED) ────────────────────────
    // The old cloud-bias system has been replaced by the WeatherPresetBundle.
    // When a bundle is assigned, PickBundleWeather() handles realistic sequencing.
    // When no bundle is assigned, PickRandomWeather() simply picks any different profile.

    // Current lerped volume influence (controls _weatherVolume.weight)
    private float _currentVolumeInfluence = 0f;

    // ─── DEBUG LOGGING STATE ─────────────────────────────────────────
    private bool  _debugLoggedCompletion = false;
    private float _debugLogNextTime      = 0f;

    // ─── INCOMING CLOUD DISSOLVE ─────────────────────────────────────
    // Set at transition start; decays to zero by transition end to simulate
    // incoming clouds rolling in from the horizon.
    private Vector4 _incomingDissolveOffset  = Vector4.zero;
    private Vector4 _incomingDissolveInitial = Vector4.zero;

    // ─── DEBUG TIME SCALE TRACKING ───────────────────────────────────
    private float _lastDebugTimeScale = 1f;

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

            // Clamp base values to sane ranges so a corrupted .mat file (e.g. values
            // accumulated across play-mode sessions) can never produce 56,000x speed
            // or 235,000x density that makes clouds invisible or hyperfast.
            // Scale  0.5–50:   prevents microscopic or planet-sized cloud patterns.
            // Speed  0.01–5:   prevents stopped or supersonic scroll; 0.3 is a typical value.
            // Density 0.1–5:   prevents transparent or wall-of-cloud extremes; 0.8–1.5 typical.
            // Sharpness 0.1–10: prevents blurry-blob or razor-edge artifacts; 1.5–3 typical.
            _baseCloudScale      = Mathf.Clamp(_baseCloudScale,      0.5f, 50f);
            _baseCloudSpeed      = Mathf.Clamp(_baseCloudSpeed,      0.01f, 5f);
            _baseCloudDensity    = Mathf.Clamp(_baseCloudDensity,    0.1f,  5f);
            _baseCloudSharpness  = Mathf.Clamp(_baseCloudSharpness,  0.1f, 10f);
            _baseCloud2Scale     = Mathf.Clamp(_baseCloud2Scale,     0.5f, 50f);
            _baseCloud2Speed     = Mathf.Clamp(_baseCloud2Speed,     0.01f, 5f);
            _baseCloud2Density   = Mathf.Clamp(_baseCloud2Density,   0.1f,  5f);
            _baseCloud2Sharpness = Mathf.Clamp(_baseCloud2Sharpness, 0.1f, 10f);
        }

        // Initialise the smoothed speed trackers to the starting profile's target speed
        // so there is no ramp-up on the first frame.
        // Each layer uses its own base speed and per-profile multiplier.
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

        // Initialise the active transition duration to the Inspector default
        _activeTransitionDuration = transitionDuration;

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
            _skyboxMaterial.SetFloat("_CloudZenithBlend",     0.4f);
            _skyboxMaterial.SetFloat("_CloudShellRadius",     25000f);
            _skyboxMaterial.SetFloat("_Cloud2ShellRadius",    35000f);
            _skyboxMaterial.SetFloat("_CloudShellFlattening", 0f);
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

            // Auto-assign a starting profile so the bundle system always has a valid anchor.
            // Priority: (1) "Clear" from weatherProfiles, (2) "Clear" from bundle entries,
            // (3) first profile in weatherProfiles, (4) first profile in bundle entries.
            Weather.WeatherProfile anchor = FindClearProfile();
            if (anchor == null && weatherProfiles != null)
            {
                foreach (var p in weatherProfiles)
                    if (p != null) { anchor = p; break; }
            }
            if (anchor == null && presetBundle != null && presetBundle.entries != null)
            {
                foreach (var e in presetBundle.entries)
                    if (e != null && e.profile != null) { anchor = e.profile; break; }
            }

            if (anchor != null)
            {
                currentWeather    = anchor;
                _lastKnownWeather = anchor;
                Debug.Log($"[WeatherManager] Auto-selected starting profile anchor: '{anchor.profileName}' (no transition triggered).");
            }
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
                _skyboxMaterial.SetFloat("_CloudZenithBlend",      0.4f);
            }
        }

        _autoWeatherTimer = SampleHoldTimer(currentWeather);
        _lastKnownWeather = currentWeather;

        // Validate timing — warn if transitions would overlap
        if (minTimeBetweenChanges < transitionDuration * 1.5f)
        {
            float safe = transitionDuration * 1.5f;
            Debug.LogWarning($"[WeatherManager] minTimeBetweenChanges ({minTimeBetweenChanges}s) is less than " +
                             $"transitionDuration * 1.5 ({safe}s). Transitions may overlap. " +
                             $"Clamping minTimeBetweenChanges to {safe}s.");
            minTimeBetweenChanges = safe;
        }

        // If no bundle is assigned, auto-generate the default one at runtime
        if (presetBundle == null && autoWeather)
            presetBundle = GetOrCreateDefaultBundle();
    }

#if UNITY_EDITOR
    private static double _lastValidateLogTime = 0;
    private void OnValidate()
    {
        // Throttle to at most one log burst every 2 seconds so rapid slider drags
        // don't flood the Console with repeated warnings.
        if (UnityEditor.EditorApplication.timeSinceStartup - _lastValidateLogTime < 2.0)
            return;
        _lastValidateLogTime = UnityEditor.EditorApplication.timeSinceStartup;

        // Warn about unreasonable values regardless of play mode
        if (transitionDuration < 10f)
            Debug.LogWarning("[WeatherManager] transitionDuration is very low. Recommended: 60-120s.");
        if (minTimeBetweenChanges < transitionDuration)
            Debug.LogWarning("[WeatherManager] minTimeBetweenChanges should be greater than transitionDuration to prevent overlapping transitions. " +
                             "Note: Per-profile duration overrides these global values when set.");

        if (!Application.isPlaying) return;
        if (Instance != this) return;

        // Detect if the user changed the currentWeather field in the Inspector
        if (currentWeather != _lastKnownWeather)
        {
            _lastKnownWeather = currentWeather;
            if (currentWeather != null)
            {
                Debug.Log($"[WeatherManager] Weather changed via Inspector to: {currentWeather.profileName}. Starting transition...");
                SetWeatherInternal(currentWeather);
            }
        }
    }
#endif

    void Update()
    {
        // ── Debug time scale: log when value changes ──────────────────
        if (debugLogTimeScale && !Mathf.Approximately(debugTimeScale, _lastDebugTimeScale))
        {
            _lastDebugTimeScale = debugTimeScale;
            Debug.Log($"[WeatherManager:Debug] Time scale set to {debugTimeScale}x");
        }
        else
        {
            _lastDebugTimeScale = debugTimeScale;
        }

        // ── Propagate debug time scale to day/night cycle ─────────────
        if (dayNightCycle != null)
            dayNightCycle.SetTimeScaleMultiplier(debugTimeScale);

        // Detect if the user changed the currentWeather field in the Inspector during Play mode.
        // OnValidate doesn't always fire reliably for MonoBehaviours, so this Update() check
        // acts as a runtime safety net.
        if (currentWeather != _lastKnownWeather)
        {
            _lastKnownWeather = currentWeather;
            if (currentWeather != null)
            {
                Debug.Log($"[WeatherManager] Weather profile changed to: {currentWeather.profileName}. Starting transition...");
                SetWeatherInternal(currentWeather);
            }
        }

        // ─── Continuously accumulate cloud dissolve offset in the current wind direction.
        // Runs every frame (during and after transitions) so clouds always appear to scroll.
        // Large offset values are fine — the shader samples noise with tiling/frac,
        // so UV wrap-around is handled naturally without any magnitude clamping.
        {
            bool inTransition = _transitionProgress < 1f;
            Weather.WeatherProfile fromP = inTransition ? _sourceWeather : currentWeather;
            Weather.WeatherProfile toP   = inTransition ? _targetWeather  : currentWeather;
            float blendT = inTransition ? _transitionProgress : 1f;

            Vector3 fromWind = (fromP != null)
                ? fromP.windDirection.normalized * fromP.windSpeed
                : Vector3.right;
            Vector3 toWind = (toP != null)
                ? toP.windDirection.normalized * toP.windSpeed
                : Vector3.right;
            Vector3 windDir = Vector3.Lerp(fromWind, toWind, blendT);
            // Guard against a zero-magnitude wind vector (e.g. both profiles have no wind).
            // Fall back to world-right (+X) as a neutral drift direction rather than NaN.
            if (windDir.sqrMagnitude < 1e-6f) windDir = Vector3.right;
            windDir = windDir.normalized;

            // Use the current smoothed cloud speed, but never let it drop to MIN_CLOUD_SPEED so
            // clouds always drift even during cross-profile SmoothDamp transitions.
            float scrollSpeed = Mathf.Max(_currentCloudSpeed, MIN_CLOUD_SPEED);
            _dissolveOffset.x += windDir.x * scrollSpeed * Time.deltaTime * debugTimeScale;
            _dissolveOffset.y += windDir.z * scrollSpeed * Time.deltaTime * debugTimeScale;
        }

        // Smooth transition
        if (_transitionProgress < 1f)
        {
            _transitionProgress += Time.deltaTime * debugTimeScale / Mathf.Max(0.01f, _activeTransitionDuration);
            _transitionProgress = Mathf.Clamp01(_transitionProgress);

            // Compute incoming dissolve offset — starts at the initial value set when the
            // transition began and linearly decays to zero as the transition completes.
            // This makes incoming storm clouds appear to roll in from the horizon.
            _incomingDissolveOffset = Vector4.Lerp(_incomingDissolveInitial, Vector4.zero, _transitionProgress);

            ApplyWeatherLerp(_sourceWeather, _targetWeather, _transitionProgress);

            if (debugLogClouds)
            {
                if (_transitionProgress >= 1f)
                {
                    if (!_debugLoggedCompletion)
                    {
                        _debugLoggedCompletion = true;
                        LogCloudDebugInfo();
                    }
                }
                else if (Time.time >= _debugLogNextTime)
                {
                    _debugLogNextTime = Time.time + debugLogInterval;
                    LogCloudDebugInfo();
                }
            }
        }
        else
        {
            // After transition: clear the incoming dissolve (it has already decayed to zero).
            _incomingDissolveOffset = Vector4.zero;

            // Auto-refresh: re-apply the active profile every frame so any Inspector edits
            // to the WeatherProfile asset are immediately visible without triggering a
            // new transition. Coverage values are kept stable (no random re-roll).
            if (autoRefreshProfile && currentWeather != null)
                ApplyWeatherLerp(currentWeather, currentWeather, 1f);
            else if (_skyboxMaterial != null)
            {
                // Not auto-refreshing, but we still need to push the continuously
                // accumulating dissolve offset to the material every frame.
                _skyboxMaterial.SetVector("_CloudDissolveOffset", _dissolveOffset);
            }
        }

        // Keep the volume weight in sync every frame (volume influence can
        // change during transitions and the weight must reflect the current blend).
        if (_weatherVolume != null)
            _weatherVolume.weight = _currentVolumeInfluence;

        // Auto-weather cycling is skipped when weather is locked by an external
        // SetWeather() call, so manually chosen conditions are never overridden automatically.
        if (autoWeather && !_weatherLocked && weatherProfiles != null && weatherProfiles.Length > 1)
        {
            _autoWeatherTimer -= Time.deltaTime * debugTimeScale;
            if (_autoWeatherTimer <= 0f)
            {
                if (presetBundle != null)
                    PickBundleWeather();
                else
                    PickRandomWeather();
            }
        }

        // Debug button
        if (_debugForceRandomWeather)
        {
            _debugForceRandomWeather = false;
            if (presetBundle != null)
                PickBundleWeather();
            else
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
    // An optional durationOverride lets bundle rules supply per-transition durations.
    private void SetWeatherInternal(Weather.WeatherProfile profile, float durationOverride = -1f)
    {
        if (profile == null) return;

        // Capture the current rendered cloud coverage as transition start
        _fromCoverage = (_skyboxMaterial != null)
            ? _skyboxMaterial.GetFloat("_CloudCoverage")
            : (_sourceWeather != null ? _fromCoverage : 0f);

        _fromCoverage2 = (_skyboxMaterial != null && _skyboxMaterial.HasProperty("_Cloud2Coverage"))
            ? _skyboxMaterial.GetFloat("_Cloud2Coverage")
            : (_sourceWeather != null ? _fromCoverage2 : 0f);

        // If a transition is already in progress, reset SmoothDamp velocities so there is
        // no accumulated momentum from the interrupted transition causing cloud speed spikes.
        if (_transitionProgress < 1f)
        {
            _cloudSpeedVelocity  = 0f;
            _cloud2SpeedVelocity = 0f;
        }

        _sourceWeather = currentWeather ?? profile;
        _targetWeather = profile;
        currentWeather = profile;
        _lastKnownWeather = profile;

        // Apply per-transition duration override if provided, otherwise check the profile's own
        // transitionDurationOverride, and finally fall back to the global transitionDuration.
        float effectiveTransition = (profile.transitionDurationOverride > 0f)
            ? profile.transitionDurationOverride
            : transitionDuration;
        _activeTransitionDuration = durationOverride >= 0f ? durationOverride : effectiveTransition;

        // Pick a random target coverage within the new profile's diversity range
        _toCoverage = Random.Range(profile.cloudCoverageMin, profile.cloudCoverageMax);
        _toCoverage2 = Random.Range(profile.cloud2CoverageMin, profile.cloud2CoverageMax);

        // Do NOT reset _dissolveOffset here — it accumulates continuously in the wind
        // direction every frame so clouds always scroll without snapping back to center.

        // Pre-compute the starting incoming dissolve offset for this transition.
        // The offset begins large (in the opposite direction of the target wind) and
        // decays to zero by the time the transition completes, creating the visual of
        // clouds rolling in from the horizon and settling into position.
        if (profile.stormRollInSpeed > 0f)
        {
            Vector3 targetWind = profile.windDirection.normalized;
            float maxDissolve = 4.0f;
            float initialMag = Mathf.Min(profile.stormRollInSpeed * Mathf.Max(_activeTransitionDuration, 30f), maxDissolve);
            _incomingDissolveInitial = new Vector4(
                -targetWind.x * initialMag,
                -targetWind.z * initialMag,
                0f, 0f);
        }
        else
        {
            _incomingDissolveInitial = Vector4.zero;
        }
        _incomingDissolveOffset = _incomingDissolveInitial;

        _transitionProgress = 0f;
        _debugLoggedCompletion = false;
        _debugLogNextTime      = 0f;

        if (debugLogClouds)
        {
            string srcName = _sourceWeather != null ? _sourceWeather.profileName : "none";
            Debug.Log($"[WeatherManager:Cloud] Transition started: '{srcName}' → '{profile.profileName}' | " +
                      $"duration={_activeTransitionDuration}s | toCoverage={_toCoverage:F3} | toCoverage2={_toCoverage2:F3}");
        }
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

    /// <summary>
    /// Returns the first profile named "Clear" (case-insensitive, exact then fuzzy Contains)
    /// found in weatherProfiles or in the presetBundle entries, or null if none found.
    /// </summary>
    private Weather.WeatherProfile FindClearProfile()
    {
        // 1. Exact case-insensitive match
        if (weatherProfiles != null)
        {
            foreach (var p in weatherProfiles)
                if (p != null && string.Equals(p.profileName, "Clear", System.StringComparison.OrdinalIgnoreCase))
                    return p;
        }
        if (presetBundle != null && presetBundle.entries != null)
        {
            foreach (var e in presetBundle.entries)
                if (e != null && e.profile != null &&
                    string.Equals(e.profile.profileName, "Clear", System.StringComparison.OrdinalIgnoreCase))
                    return e.profile;
        }
        // 2. Fuzzy: profile name starts with "Clear" (e.g. "Clear Sky")
        if (weatherProfiles != null)
        {
            foreach (var p in weatherProfiles)
                if (p != null && p.profileName.IndexOf("Clear", System.StringComparison.OrdinalIgnoreCase) == 0)
                    return p;
        }
        if (presetBundle != null && presetBundle.entries != null)
        {
            foreach (var e in presetBundle.entries)
                if (e != null && e.profile != null &&
                    e.profile.profileName.IndexOf("Clear", System.StringComparison.OrdinalIgnoreCase) == 0)
                    return e.profile;
        }
        return null;
    }

    /// <summary>
    /// Returns the profile with the lowest severityLevel from the bundle entries.
    /// If multiple entries share the lowest severity, returns the first one.
    /// Returns null if the bundle has no valid entries.
    /// </summary>
    private Weather.WeatherProfile FindLowestSeverityProfile(Weather.WeatherPresetBundle bundle)
    {
        if (bundle == null || bundle.entries == null) return null;
        Weather.WeatherProfile best = null;
        int bestSeverity = int.MaxValue;
        foreach (var e in bundle.entries)
        {
            if (e == null || e.profile == null) continue;
            if (e.severityLevel < bestSeverity)
            {
                bestSeverity = e.severityLevel;
                best = e.profile;
            }
        }
        return best;
    }

    /// <summary>
    /// Resets all timing and auto-weather values to well-tuned game defaults.
    /// Use this to fix stale Inspector-serialized values from older versions of the component.
    /// </summary>
    [ContextMenu("Reset To Recommended Defaults")]
    private void ResetToRecommendedDefaults()
    {
        transitionDuration      = 120f;
        minTimeBetweenChanges   = 180f;
        maxTimeBetweenChanges   = 1500f;
        autoWeather             = true;
        autoRefreshProfile      = false;
        Debug.Log("[WeatherManager] Reset To Recommended Defaults applied: " +
                  "transitionDuration=120s, minTimeBetweenChanges=180s, maxTimeBetweenChanges=1500s, " +
                  "autoWeather=true, autoRefreshProfile=false.");
    }

    /// <summary>
    /// Samples a hold-time for the given profile. Uses the profile's own minDuration/maxDuration
    /// when set (either > 0), otherwise falls back to the global minTimeBetweenChanges/maxTimeBetweenChanges.
    /// </summary>
    private float SampleHoldTimer(Weather.WeatherProfile profile)
    {
        if (profile != null && profile.minDuration > 0f && profile.maxDuration > 0f)
            return Random.Range(profile.minDuration, Mathf.Max(profile.minDuration, profile.maxDuration));
        return Random.Range(minTimeBetweenChanges, maxTimeBetweenChanges);
    }

    private void PickRandomWeather()
    {
        if (weatherProfiles == null || weatherProfiles.Length == 0) return;

        // Simple unbiased pick: just choose any profile that is different from the current one.
        // The old cloud-coverage bias has been removed — it caused a HeavyStorm feedback loop.
        Weather.WeatherProfile next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
        int attempts = 0;
        while (next == currentWeather && weatherProfiles.Length > 1 && attempts < 20)
        {
            next = weatherProfiles[Random.Range(0, weatherProfiles.Length)];
            attempts++;
        }

        SetWeatherInternal(next);
        // Reset hold timer from the new profile's own duration, or fallback to globals.
        _autoWeatherTimer = SampleHoldTimer(next);
    }

    /// <summary>
    /// Picks the next weather using the assigned <see cref="Weather.WeatherPresetBundle"/>'s
    /// transition rules for the current weather.  Falls back to the lowest-severity profile
    /// if the current weather has no entry or no valid transitions in the bundle.
    /// </summary>
    private void PickBundleWeather()
    {
        if (presetBundle == null) { PickRandomWeather(); return; }

        // Null-safe: if currentWeather is somehow still null, find the lowest-severity bundle entry
        if (currentWeather == null)
        {
            Weather.WeatherProfile lowest = FindLowestSeverityProfile(presetBundle);
            if (lowest != null)
            {
                currentWeather    = lowest;
                _lastKnownWeather = lowest;
                Debug.Log($"[WeatherManager] currentWeather was null in PickBundleWeather — anchoring to lowest-severity profile: '{lowest.profileName}'.");
            }
        }

        Weather.WeatherBundleEntry entry = presetBundle.FindEntry(currentWeather);
        if (entry == null || entry.allowedTransitions == null || entry.allowedTransitions.Length == 0)
        {
            Weather.WeatherProfile fallback = FindLowestSeverityProfile(presetBundle);
            if (fallback != null && fallback != currentWeather)
            {
                Debug.LogWarning($"[WeatherManager] Bundle has no entry or transitions for '{currentWeather?.profileName}'. Falling back to lowest-severity profile: '{fallback.profileName}'.");
                SetWeatherInternal(fallback);
                _autoWeatherTimer = SampleHoldTimer(fallback);
            }
            else
            {
                Debug.LogWarning($"[WeatherManager] Bundle has no entry or transitions for '{currentWeather?.profileName}'. No fallback available.");
                _autoWeatherTimer = SampleHoldTimer(currentWeather);
            }
            return;
        }

        // Filter transitions by time-of-day constraints
        float tod = dayNightCycle != null ? dayNightCycle.GetCurrentTimeOfDay() : 0.5f;
        bool isDay = tod >= 0.2f && tod <= 0.8f;

        // Warn once if time-of-day constraints are in use but dayNightCycle is not assigned
        bool hasTimeConstraints = false;
        foreach (var rule in entry.allowedTransitions)
            if (rule != null && (rule.dayOnly || rule.nightOnly)) { hasTimeConstraints = true; break; }
        if (hasTimeConstraints && dayNightCycle == null)
            Debug.LogWarning("[WeatherManager] Bundle rules use dayOnly/nightOnly constraints but no DayNightCycle is assigned. " +
                             "Time-of-day filtering will always assume daytime (tod=0.5).");

        System.Collections.Generic.List<Weather.WeatherTransitionRule> valid =
            new System.Collections.Generic.List<Weather.WeatherTransitionRule>();

        foreach (var rule in entry.allowedTransitions)
        {
            if (rule == null || rule.target == null) continue;
            if (rule.dayOnly   && !isDay) continue;
            if (rule.nightOnly &&  isDay) continue;
            if (rule.weight   <= 0f)     continue;
            valid.Add(rule);
        }

        // If all transitions were filtered out by time-of-day, retry ignoring constraints
        if (valid.Count == 0)
        {
            Debug.Log("[WeatherManager] All bundle transitions filtered by time-of-day; ignoring constraints.");
            foreach (var rule in entry.allowedTransitions)
            {
                if (rule != null && rule.target != null && rule.weight > 0f)
                    valid.Add(rule);
            }
        }

        if (valid.Count == 0)
        {
            Weather.WeatherProfile fallback = FindLowestSeverityProfile(presetBundle);
            if (fallback != null && fallback != currentWeather)
            {
                Debug.LogWarning($"[WeatherManager] No valid bundle transitions for '{currentWeather?.profileName}'. Falling back to lowest-severity profile: '{fallback.profileName}'.");
                SetWeatherInternal(fallback);
                _autoWeatherTimer = SampleHoldTimer(fallback);
            }
            else
            {
                Debug.LogWarning($"[WeatherManager] No valid bundle transitions for '{currentWeather?.profileName}'. No fallback available.");
                _autoWeatherTimer = SampleHoldTimer(currentWeather);
            }
            return;
        }

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var rule in valid) totalWeight += rule.weight;
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        Weather.WeatherTransitionRule chosen = valid[valid.Count - 1];
        foreach (var rule in valid)
        {
            cumulative += rule.weight;
            if (roll <= cumulative) { chosen = rule; break; }
        }

        // Apply per-rule transition duration if set
        float duration = chosen.GetTransitionDuration(presetBundle);

        SetWeatherInternal(chosen.target, duration);

        // Set the next timer: prefer bundle entry's explicit hold time, then profile's own
        // duration, then fall back to the bundle's global hold time.
        Weather.WeatherBundleEntry targetEntry = presetBundle.FindEntry(chosen.target);
        float holdMin, holdMax;
        if (targetEntry != null && targetEntry.minHoldTime >= 0f)
            holdMin = targetEntry.minHoldTime;
        else if (chosen.target.minDuration > 0f && chosen.target.maxDuration > 0f)
            holdMin = chosen.target.minDuration;
        else
            holdMin = presetBundle.minimumHoldTime;

        if (targetEntry != null && targetEntry.maxHoldTime >= 0f)
            holdMax = targetEntry.maxHoldTime;
        else if (chosen.target.minDuration > 0f && chosen.target.maxDuration > 0f)
            holdMax = chosen.target.maxDuration;
        else
            holdMax = presetBundle.maximumHoldTime;

        _autoWeatherTimer = Random.Range(holdMin, Mathf.Max(holdMin, holdMax));
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

    // ─── BUNDLE HELPERS ──────────────────────────────────────────────

    /// <summary>
    /// Returns a runtime-generated default bundle if none is assigned in the Inspector.
    /// The bundle encodes a full meteorological severity ladder so auto-weather follows
    /// realistic sequences (e.g. Clear → Slightly Cloudy → Partly Cloudy → ... → Heavy Storm).
    /// </summary>
    private Weather.WeatherPresetBundle GetOrCreateDefaultBundle()
    {
        var bundle = ScriptableObject.CreateInstance<Weather.WeatherPresetBundle>();
        bundle.bundleName             = "Runtime Default Bundle";
        bundle.description            = "Auto-generated default bundle — assign a WeatherPresetBundle asset to customise.";
        bundle.defaultTransitionDuration = 90f;
        bundle.minimumHoldTime        = 120f;
        bundle.maximumHoldTime        = 1500f;

        // Helper: look up a profile by name from the weatherProfiles array
        // Uses case-insensitive exact match first, then Contains as a fuzzy fallback.
        Weather.WeatherProfile Find(string name)
        {
            if (weatherProfiles == null) return null;
            // 1. Exact case-insensitive match
            foreach (var p in weatherProfiles)
                if (p != null && string.Equals(p.profileName, name, System.StringComparison.OrdinalIgnoreCase))
                    return p;
            // 2. Fuzzy: profile name Contains the key (e.g. "Clear Sky" matches "Clear")
            foreach (var p in weatherProfiles)
                if (p != null && p.profileName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            // 3. Fuzzy reverse: key Contains profile name (e.g. "Slightly Cloudy" matches key "Slightly")
            foreach (var p in weatherProfiles)
                if (p != null && name.IndexOf(p.profileName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return p;
            return null;
        }

        // Helper: build a WeatherTransitionRule quickly
        Weather.WeatherTransitionRule Rule(string targetName, float weight,
                                           float dur = -1f,
                                           bool dayOnly = false, bool nightOnly = false)
        {
            var r = new Weather.WeatherTransitionRule
            {
                target             = Find(targetName),
                weight             = weight,
                transitionDuration = dur,
                dayOnly            = dayOnly,
                nightOnly          = nightOnly,
            };
            return r;
        }

        // Helper: build a WeatherBundleEntry
        Weather.WeatherBundleEntry Entry(string name, int severity,
                                          Weather.WeatherTransitionRule[] rules,
                                          float minHold = -1f, float maxHold = -1f)
        {
            return new Weather.WeatherBundleEntry
            {
                profile            = Find(name),
                severityLevel      = severity,
                allowedTransitions = rules,
                minHoldTime        = minHold,
                maxHoldTime        = maxHold,
            };
        }

        // ── Severity ladder ─────────────────────────────────────────────
        // | Sev | Profile        | Transitions                                        | Hold (min/max)   |
        // |-----|----------------|----------------------------------------------------|------------------|
        // |  0  | Clear          | → Slightly Cloudy (3.0), → Fog (0.5, nightOnly)    | 300–3600s        |
        // |  1  | Slightly Cloudy| → Clear (2.0), → Partly Cloudy (2.0), → Fog (0.3) | 180–2100s        |
        // |  1  | Fog            | → Clear (1.5), → Slightly Cloudy (2.0), → PC (0.5)| 120–1500s        |
        // |  2  | Partly Cloudy  | → Slightly Cloudy (2.0), → Mostly Cloudy (2.0)     | 120–1800s        |
        // |  3  | Mostly Cloudy  | → Partly Cloudy (2.0), → Overcast (2.0), → Snow   | 120–1500s        |
        // |  4  | Overcast       | → Mostly Cloudy (2.0), → LightRain (2.0), → SC/Snow| 120–1500s       |
        // |  5  | Super Cloudy   | → Overcast (2.0), → Light Rain (1.5)               | 60–1200s         |
        // |  5  | Light Rain     | → Overcast (2.0), → Super Cloudy (1.0), → Storm   | 120–1500s        |
        // |  5  | Snow           | → Overcast (2.0), → Mostly Cloudy (1.0)            | 120–1500s        |
        // |  6  | Heavy Storm    | → Light Rain (3.0), → Overcast (1.0)               | 60–1200s         |

        bundle.entries = new Weather.WeatherBundleEntry[]
        {
            Entry("Clear", 0, new[]
            {
                Rule("Slightly Cloudy", 3.0f, 60f),
                Rule("Fog",             0.5f, 90f, nightOnly: true),
            }, minHold: 300f, maxHold: 3600f),
            Entry("Slightly Cloudy", 1, new[]
            {
                Rule("Clear",          2.0f, 60f),
                Rule("Partly Cloudy",  2.0f, 75f),
                Rule("Fog",            0.3f, 90f),
            }, minHold: 180f, maxHold: 2100f),
            Entry("Fog", 1, new[]
            {
                Rule("Clear",          1.5f, 75f),
                Rule("Slightly Cloudy",2.0f, 75f),
                Rule("Partly Cloudy",  0.5f, 90f),
            }, minHold: 120f, maxHold: 1500f),
            Entry("Partly Cloudy", 2, new[]
            {
                Rule("Slightly Cloudy",2.0f, 75f),
                Rule("Mostly Cloudy",  2.0f, 90f),
            }, minHold: 120f, maxHold: 1800f),
            Entry("Mostly Cloudy", 3, new[]
            {
                Rule("Partly Cloudy",  2.0f, 90f),
                Rule("Overcast",       2.0f, 90f),
                Rule("Snow",           0.5f, 120f),
            }, minHold: 120f, maxHold: 1500f),
            Entry("Overcast", 4, new[]
            {
                Rule("Mostly Cloudy",  2.0f, 90f),
                Rule("Light Rain",     2.0f, 120f),
                Rule("Super Cloudy",   1.0f, 90f),
                Rule("Snow",           1.0f, 120f),
            }, minHold: 120f, maxHold: 1500f),
            Entry("Super Cloudy", 5, new[]
            {
                Rule("Overcast",       2.0f, 90f),
                Rule("Light Rain",     1.5f, 120f),
            }, minHold: 60f, maxHold: 1200f),
            Entry("Light Rain", 5, new[]
            {
                Rule("Overcast",       2.0f, 120f),
                Rule("Super Cloudy",   1.0f, 90f),
                Rule("Heavy Storm",    1.0f, 150f),
            }, minHold: 120f, maxHold: 1500f),
            Entry("Snow", 5, new[]
            {
                Rule("Overcast",       2.0f, 120f),
                Rule("Mostly Cloudy",  1.0f, 120f),
            }, minHold: 120f, maxHold: 1500f),
            // Heavy Storm can ONLY de-escalate — it never transitions to clear/sunny directly
            Entry("Heavy Storm", 6, new[]
            {
                Rule("Light Rain",     3.0f, 120f),
                Rule("Overcast",       1.0f, 150f),
            }, minHold: 60f, maxHold: 1200f),
        };

        // Log a warning for any profiles that were not found so the user knows to
        // ensure their weatherProfiles array is populated.
        foreach (var entry in bundle.entries)
        {
            if (entry.profile == null)
                Debug.LogWarning("[WeatherManager] GetOrCreateDefaultBundle: could not find a WeatherProfile for one or more bundle entries. " +
                                 "Make sure all profiles are listed in the weatherProfiles array.");
            if (entry.allowedTransitions != null)
            {
                foreach (var rule in entry.allowedTransitions)
                {
                    if (rule.target == null)
                        Debug.LogWarning("[WeatherManager] GetOrCreateDefaultBundle: a transition rule has a null target. " +
                                         "Make sure all profile names match exactly.");
                }
            }
        }

        return bundle;
    }

    /// <summary>
    /// Logs the current bundle's full transition graph to the Console.
    /// Useful for verifying the weather sequence in Play mode.
    /// </summary>
    [ContextMenu("Log Bundle Status")]
    private void LogBundleStatus()
    {
        var b = presetBundle;
        if (b == null)
        {
            Debug.Log("[WeatherManager] No bundle assigned. A runtime default bundle will be created when autoWeather is enabled.");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[WeatherManager] Bundle: \"{b.bundleName}\"");
        sb.AppendLine($"  defaultTransitionDuration = {b.defaultTransitionDuration}s");
        sb.AppendLine($"  holdTime = {b.minimumHoldTime}–{b.maximumHoldTime}s");
        if (b.entries != null)
        {
            foreach (var entry in b.entries)
            {
                if (entry == null) continue;
                string pName = entry.profile != null ? entry.profile.profileName : "(null)";
                sb.AppendLine($"  [{entry.severityLevel}] {pName}  holdTime={entry.minHoldTime}/{entry.maxHoldTime}");
                if (entry.allowedTransitions != null)
                {
                    foreach (var rule in entry.allowedTransitions)
                    {
                        if (rule == null) continue;
                        string tName = rule.target != null ? rule.target.profileName : "(null)";
                        string constraints = rule.dayOnly ? " [dayOnly]" : rule.nightOnly ? " [nightOnly]" : "";
                        float dur = rule.GetTransitionDuration(b);
                        sb.AppendLine($"      → {tName}  w={rule.weight:F1}  dur={dur}s{constraints}");
                    }
                }
            }
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Context-menu helper that logs the exact configuration for a default bundle asset
    /// to the Console. Copy-paste the output to configure a WeatherPresetBundle asset manually.
    /// </summary>
    [ContextMenu("Create Default Bundle (Log Config)")]
    private void LogDefaultBundleConfig()
    {
        var b = GetOrCreateDefaultBundle();
        var saved = presetBundle;
        try
        {
            presetBundle = b;
            LogBundleStatus();
        }
        finally
        {
            presetBundle = saved;
            if (Application.isPlaying) Destroy(b);
            else DestroyImmediate(b);
        }
    }

    private void LogCloudDebugInfo()
    {
        string srcName = _sourceWeather != null ? _sourceWeather.profileName : "none";
        string tgtName = _targetWeather != null ? _targetWeather.profileName : "none";
        float t = _transitionProgress;

        // Compute target speeds (same formula as ApplyWeatherLerp) so we can show target vs smoothed
        float boost = (_sourceWeather != null && _targetWeather != null)
            ? Mathf.Lerp(_sourceWeather.windSpeedBoost, _targetWeather.windSpeedBoost, t)
            : 0f;
        float targetCloudSpeed = (_sourceWeather != null && _targetWeather != null)
            ? _baseCloudSpeed  * Mathf.Lerp(_sourceWeather.cloudSpeedMultiplier,  _targetWeather.cloudSpeedMultiplier,  t) + boost
            : _baseCloudSpeed;
        float targetCloud2Speed = (_sourceWeather != null && _targetWeather != null)
            ? _baseCloud2Speed * Mathf.Lerp(_sourceWeather.cloud2SpeedMultiplier, _targetWeather.cloud2SpeedMultiplier, t) + boost
            : _baseCloud2Speed;

        float coverage   = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudCoverage")     : 0f;
        float density    = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudDensity")      : 0f;
        float sharpness  = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudSharpness")    : 0f;
        float scale      = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudScale")        : 0f;
        float edgeSoft   = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudEdgeSoftness") : 0f;
        float variation  = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudVariation")    : 0f;
        float brightness = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudBrightness")   : 0f;
        float darkness   = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudDarkness")     : 0f;

        float coverage2   = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Coverage")   : 0f;
        float density2    = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Density")    : 0f;
        float sharpness2  = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Sharpness")  : 0f;
        float scale2      = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Scale")      : 0f;
        float opacity2    = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Opacity")    : 0f;
        float brightness2 = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Brightness") : 0f;
        float darkness2   = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_Cloud2Darkness")   : 0f;

        Vector4 windDir   = _skyboxMaterial != null ? _skyboxMaterial.GetVector("_CloudDirection")  : Vector4.zero;
        float zenithBlend = _skyboxMaterial != null ? _skyboxMaterial.GetFloat("_CloudZenithBlend") : 0f;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[WeatherManager:Cloud] Profile: '{srcName}' → '{tgtName}' | Progress: {t * 100f:F1}%");
        sb.AppendLine($"  Layer1: coverage={coverage:F3}  speed(target={targetCloudSpeed:F4} smoothed={_currentCloudSpeed:F4})" +
                      $"  density={density:F3}  sharpness={sharpness:F3}  scale={scale:F3}" +
                      $"  edgeSoftness={edgeSoft:F3}  variation={variation:F3}  brightness={brightness:F3}  darkness={darkness:F3}");
        sb.AppendLine($"  Layer2: coverage={coverage2:F3}  speed(target={targetCloud2Speed:F4} smoothed={_currentCloud2Speed:F4})" +
                      $"  density={density2:F3}  sharpness={sharpness2:F3}  scale={scale2:F3}" +
                      $"  opacity={opacity2:F3}  brightness={brightness2:F3}  darkness={darkness2:F3}");
        sb.AppendLine($"  Wind: direction=({windDir.x:F3},{windDir.y:F3},{windDir.z:F3})  speedBoost={boost:F4}");
        sb.AppendLine($"  DepartDissolve=({_dissolveOffset.x:F4},{_dissolveOffset.y:F4})  " +
                      $"IncomingDissolve=({_incomingDissolveOffset.x:F4},{_incomingDissolveOffset.y:F4})  ZenithBlend={zenithBlend:F3}");
        sb.AppendLine($"  SmoothDamp velocities: layer1={_cloudSpeedVelocity:F4}  layer2={_cloud2SpeedVelocity:F4}");
        sb.Append(    $"  VolumeInfluence={_currentVolumeInfluence:F3}  TimeScale={debugTimeScale:F1}x");
        Debug.Log(sb.ToString());
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
            // snapping on weather transitions. Smooth time is capped at 10s to prevent
            // long transitions from causing overshoot / "supersonic" cloud artifacts.
            float boost = Mathf.Lerp(from.windSpeedBoost, to.windSpeedBoost, t);
            float targetCloudSpeed = _baseCloudSpeed * Mathf.Lerp(from.cloudSpeedMultiplier, to.cloudSpeedMultiplier, t) + boost;
            float cloudSmoothTime = Mathf.Min(cloudSpeedSmoothTime, 10f);
            _currentCloudSpeed = Mathf.SmoothDamp(
                _currentCloudSpeed, targetCloudSpeed,
                ref _cloudSpeedVelocity, cloudSmoothTime);
            // Prevent SmoothDamp overshoot — clamp to [0, target * 1.2].
            _currentCloudSpeed = Mathf.Clamp(_currentCloudSpeed, 0f, targetCloudSpeed * 1.2f);
            // Enforce minimum floor so clouds never appear frozen during transitions.
            _currentCloudSpeed = Mathf.Max(_currentCloudSpeed, MIN_CLOUD_SPEED);
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

            // Cloud Layer 2 speed — independent SmoothDamp using Layer 2's own base
            // and multipliers. _cloud2SpeedVelocity is never shared with Layer 1.
            float targetCloud2Speed = _baseCloud2Speed * Mathf.Lerp(from.cloud2SpeedMultiplier, to.cloud2SpeedMultiplier, t) + boost;
            float cloud2SmoothTime = Mathf.Min(cloudSpeedSmoothTime, 10f);
            _currentCloud2Speed = Mathf.SmoothDamp(_currentCloud2Speed, targetCloud2Speed, ref _cloud2SpeedVelocity, cloud2SmoothTime);
            _currentCloud2Speed = Mathf.Clamp(_currentCloud2Speed, 0f, targetCloud2Speed * 1.2f);
            // Same minimum floor for Layer 2 so high-altitude clouds also keep moving.
            _currentCloud2Speed = Mathf.Max(_currentCloud2Speed, MIN_CLOUD_SPEED);
            _skyboxMaterial.SetFloat("_Cloud2Speed", _currentCloud2Speed);
            _skyboxMaterial.SetFloat("_Cloud2Brightness", Mathf.Lerp(from.cloud2Brightness, to.cloud2Brightness, t));
            _skyboxMaterial.SetFloat("_Cloud2Darkness",   Mathf.Lerp(from.cloud2Darkness,   to.cloud2Darkness,   t));
            _skyboxMaterial.SetColor("_Cloud2Color",       Color.Lerp(from.cloud2Color,       to.cloud2Color,       t));
            _skyboxMaterial.SetColor("_Cloud2ShadowColor", Color.Lerp(from.cloud2ShadowColor, to.cloud2ShadowColor, t));
            _skyboxMaterial.SetFloat("_Cloud2Opacity",     Mathf.Lerp(from.cloud2Opacity,     to.cloud2Opacity,     t));

            // Directional dissolve offset — combines the departing storm rolling away
            // (positive direction) with incoming clouds rolling in from the horizon
            // (negative direction, decays to zero as transition completes).
            _skyboxMaterial.SetVector("_CloudDissolveOffset", _dissolveOffset + _incomingDissolveOffset);

            // Cloud Zenith Blend — lerp the zenith-ring suppression parameter
            _skyboxMaterial.SetFloat("_CloudZenithBlend",
                Mathf.Lerp(from.cloudZenithBlend, to.cloudZenithBlend, t));
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
