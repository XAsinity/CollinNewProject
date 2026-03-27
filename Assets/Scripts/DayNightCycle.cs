using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Length of a full day in real-time seconds")]
    public float dayLengthInSeconds = 120f;

    [Range(0f, 1f)]
    [Tooltip("Starting time of day (0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset)")]
    public float startTimeOfDay = 0.25f;

    [Header("Sun & Moon")]
    public Light sunLight;
    public Light moonLight;

    [Header("Sun Settings")]
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon Settings")]
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Ambient Light")]
    public Gradient ambientColor;

    [Header("Skybox (Optional)")]
    [Tooltip("Assign a skybox material that uses _Exposure or _AtmosphereThickness")]
    public Material skyboxMaterial;
    public AnimationCurve skyboxExposure;

    private float currentTimeOfDay;

    void Start()
    {
        currentTimeOfDay = startTimeOfDay;
    }

    void Update()
    {
        // Advance time
        currentTimeOfDay += Time.deltaTime / dayLengthInSeconds;
        if (currentTimeOfDay >= 1f)
            currentTimeOfDay -= 1f;

        UpdateSun();
        UpdateMoon();
        UpdateAmbient();
        UpdateSkybox();
    }

    void UpdateSun()
    {
        // Sun rotates 360° over one full day cycle
        // At time 0.25 (sunrise) sun is at horizon (0°), at 0.5 (noon) sun is overhead (90°)
        float sunAngle = (currentTimeOfDay - 0.25f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        // Evaluate color and intensity from curves/gradients
        sunLight.color = sunColor.Evaluate(currentTimeOfDay);
        sunLight.intensity = sunIntensity.Evaluate(currentTimeOfDay);

        // Disable sun when below horizon
        sunLight.enabled = sunLight.intensity > 0.01f;
    }

    void UpdateMoon()
    {
        // Moon is opposite the sun (offset by 0.5 / 180°)
        float moonAngle = (currentTimeOfDay - 0.75f) * 360f;
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

        moonLight.color = moonColor.Evaluate(currentTimeOfDay);
        moonLight.intensity = moonIntensity.Evaluate(currentTimeOfDay);

        moonLight.enabled = moonLight.intensity > 0.01f;
    }

    void UpdateAmbient()
    {
        RenderSettings.ambientLight = ambientColor.Evaluate(currentTimeOfDay);
    }

    void UpdateSkybox()
    {
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Exposure", skyboxExposure.Evaluate(currentTimeOfDay));
        }
    }

    /// <summary>
    /// Returns the current time of day (0-1). Useful for UI or other systems.
    /// </summary>
    public float GetCurrentTimeOfDay()
    {
        return currentTimeOfDay;
    }
}