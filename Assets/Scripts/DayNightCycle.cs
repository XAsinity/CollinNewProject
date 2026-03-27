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

    [Header("Ambient Light")]
    public Gradient ambientColor;
    public AnimationCurve ambientIntensity;

    [Header("Fog")]
    public bool enableFog = true;
    public Gradient fogColor;
    public AnimationCurve fogDensityCurve;

    private float currentTimeOfDay;
    private bool isPaused = false;

    void Start()
    {
        currentTimeOfDay = startTimeOfDay;
    }

    void Update()
    {
        if (!isPaused)
        {
            currentTimeOfDay += Time.deltaTime / dayLengthInSeconds;
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
    }

    void UpdateSunLight()
    {
        if (sunLight == null) return;

        float sunAngle = (currentTimeOfDay - 0.25f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        sunLight.color = sunLightColor.Evaluate(currentTimeOfDay);
        sunLight.intensity = sunLightIntensity.Evaluate(currentTimeOfDay);
        sunLight.enabled = sunLight.intensity > 0.01f;
    }

    void UpdateMoonLight()
    {
        if (moonLight == null) return;

        float moonAngle = (currentTimeOfDay - 0.75f) * 360f;
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

        moonLight.color = moonLightColor.Evaluate(currentTimeOfDay);
        moonLight.intensity = moonLightIntensity.Evaluate(currentTimeOfDay);
        moonLight.enabled = moonLight.intensity > 0.01f;
    }

    void UpdateAmbient()
    {
        RenderSettings.ambientLight = ambientColor.Evaluate(currentTimeOfDay);
        RenderSettings.ambientIntensity = ambientIntensity.Evaluate(currentTimeOfDay);
    }

    void UpdateFog()
    {
        RenderSettings.fog = enableFog;
        if (!enableFog) return;
        RenderSettings.fogColor = fogColor.Evaluate(currentTimeOfDay);
        RenderSettings.fogDensity = fogDensityCurve.Evaluate(currentTimeOfDay);
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
}