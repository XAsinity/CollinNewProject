using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Length of a full day in real-time seconds. Default is 60s (1 minute) for testing — increase for production (e.g. 600 = 10 minutes, 1440 = 24 minutes).")]
    public float dayLengthInSeconds = 60f;

    [Range(0f, 1f)]
    [Tooltip("Starting time (0=midnight, 0.25=sunrise, 0.5=noon, 0.75=sunset)")]
    public float startTimeOfDay = 0.25f;

    [Header("Celestial Bodies")]
    [Tooltip("Sun directional light")]
    public Light sunLight;
    [Tooltip("Moon directional light")]
    public Light moonLight;

    [Header("Sun Light Settings")]
    public Gradient sunLightColor;
    public AnimationCurve sunLightIntensity;

    [Header("Moon Light Settings")]
    public Gradient moonLightColor;
    public AnimationCurve moonLightIntensity;

    [Header("Skybox Material")]
    [Tooltip("Assign the material using Custom/DayNightSkybox shader")]
    public Material skyboxMaterial;

    [Header("Cloud Material")]
    [Tooltip("Assign the material using Custom/VolumetricClouds shader. " +
             "DayNightCycle pushes _TimeOfDay, _SunDirection and _MoonDirection to this material " +
             "so the cloud shader can apply time-of-day coloring and light-scattering direction.")]
    public Material cloudMaterial;

    [Header("Ambient Light")]
    public Gradient ambientColor;
    public AnimationCurve ambientIntensity;

    [Header("Fog")]
    public bool enableFog = true;
    public Gradient fogColor;
    public AnimationCurve fogDensityCurve;

    private float currentTimeOfDay;
    private bool isPaused = false;
    private float _timeScaleMultiplier = 1f;

    // ─── WEATHER MULTIPLIERS (set by WeatherManager) ──────────────
    private float _sunIntensityMultiplier   = 1f;
    private float _moonIntensityMultiplier  = 1f;
    private float _ambientMultiplier        = 1f;
    private Color _ambientColorTint         = Color.white;
    private float _fogMultiplier            = 1f;
    private bool  _fogColorOverrideEnabled  = false;
    private Color _fogColorOverride         = Color.white;

    void Start()
    {
        currentTimeOfDay = startTimeOfDay;
    }

    void Update()
    {
        if (!isPaused)
        {
            currentTimeOfDay += Time.deltaTime * _timeScaleMultiplier / dayLengthInSeconds;
            if (currentTimeOfDay >= 1f) currentTimeOfDay -= 1f;
        }

        UpdateSkyboxMaterial();
        UpdateSunLight();
        UpdateMoonLight();
        UpdateAmbient();
        UpdateFog();
    }

    void UpdateSkyboxMaterial()
    {
        if (skyboxMaterial == null) return;

        skyboxMaterial.SetFloat("_TimeOfDay", currentTimeOfDay);

        if (sunLight != null)
            skyboxMaterial.SetVector("_SunDirection", -sunLight.transform.forward);

        if (moonLight != null)
            skyboxMaterial.SetVector("_MoonDirection", -moonLight.transform.forward);

        // Push the same time-of-day and light-direction properties to the cloud material
        // so the VolumetricClouds shader can apply time-of-day coloring and HG light scattering.
        if (cloudMaterial != null)
        {
            cloudMaterial.SetFloat("_TimeOfDay", currentTimeOfDay);

            if (sunLight != null)
                cloudMaterial.SetVector("_SunDirection", -sunLight.transform.forward);

            if (moonLight != null)
                cloudMaterial.SetVector("_MoonDirection", -moonLight.transform.forward);
        }
    }

    void UpdateSunLight()
    {
        if (sunLight == null) return;

        float sunAngle = (currentTimeOfDay - 0.25f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        sunLight.color = sunLightColor.Evaluate(currentTimeOfDay);
        sunLight.intensity = sunLightIntensity.Evaluate(currentTimeOfDay) * _sunIntensityMultiplier;
        sunLight.enabled = sunLight.intensity > 0.01f;
    }

    void UpdateMoonLight()
    {
        if (moonLight == null) return;

        float moonAngle = (currentTimeOfDay - 0.75f) * 360f;
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

        moonLight.color = moonLightColor.Evaluate(currentTimeOfDay);
        moonLight.intensity = moonLightIntensity.Evaluate(currentTimeOfDay) * _moonIntensityMultiplier;
        moonLight.enabled = moonLight.intensity > 0.01f;
    }

    void UpdateAmbient()
    {
        RenderSettings.ambientLight = ambientColor.Evaluate(currentTimeOfDay) * _ambientColorTint;
        RenderSettings.ambientIntensity = ambientIntensity.Evaluate(currentTimeOfDay) * _ambientMultiplier;
    }

    void UpdateFog()
    {
        RenderSettings.fog = enableFog;
        if (!enableFog) return;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = _fogColorOverrideEnabled
            ? _fogColorOverride
            : fogColor.Evaluate(currentTimeOfDay);
        // Clamp density so heavy weather multipliers can't create a total sky whiteout.
        // ExponentialSquared already falls off naturally at distance, so 0.04 is plenty.
        RenderSettings.fogDensity = Mathf.Min(fogDensityCurve.Evaluate(currentTimeOfDay) * _fogMultiplier, 0.04f);
    }

    // ─── PUBLIC API ───────────────────────────────────────

    public float GetCurrentTimeOfDay() => currentTimeOfDay;

    public string GetTimeAsString()
    {
        float hours = currentTimeOfDay * 24f;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return string.Format("{0:D2}:{1:D2}", h, m);
    }

    public bool IsDay() => currentTimeOfDay > 0.2f && currentTimeOfDay < 0.8f;
    public bool IsNight() => !IsDay();

    public void SetTimeOfDay(float time) => currentTimeOfDay = Mathf.Clamp01(time);
    public void SetDayLength(float seconds) => dayLengthInSeconds = Mathf.Max(1f, seconds);
    public void PauseCycle() => isPaused = true;
    public void ResumeCycle() => isPaused = false;
    public void TogglePause() => isPaused = !isPaused;

    // ─── WEATHER INTEGRATION ──────────────────────────────────────

    /// <summary>Called by WeatherManager to scale sun light intensity.</summary>
    public void SetSunIntensityMultiplier(float value)  => _sunIntensityMultiplier  = Mathf.Max(0f, value);
    /// <summary>Called by WeatherManager to scale moon light intensity.</summary>
    public void SetMoonIntensityMultiplier(float value) => _moonIntensityMultiplier = Mathf.Max(0f, value);
    /// <summary>Called by WeatherManager to scale ambient light intensity.</summary>
    public void SetAmbientMultiplier(float value)       => _ambientMultiplier       = Mathf.Max(0f, value);
    /// <summary>Called by WeatherManager to tint ambient light color.</summary>
    public void SetAmbientColorTint(Color tint)         => _ambientColorTint        = tint;
    /// <summary>Called by WeatherManager to scale fog density.</summary>
    public void SetFogMultiplier(float value)           => _fogMultiplier           = Mathf.Max(0f, value);
    /// <summary>Called by WeatherManager to speed up (or slow down) the day/night cycle for debug purposes.
    /// Only affects the internal time-of-day advancement — does NOT touch Time.timeScale.</summary>
    public void SetTimeScaleMultiplier(float value) => _timeScaleMultiplier = Mathf.Max(0f, value);

    /// <summary>Called by WeatherManager to override the time-of-day fog color.</summary>
    public void SetFogColorOverride(Color color, bool enable)
    {
        _fogColorOverrideEnabled = enable;
        _fogColorOverride        = color;
    }
}