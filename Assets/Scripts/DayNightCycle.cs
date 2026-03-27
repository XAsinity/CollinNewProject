using UnityEngine;

/// <summary>
/// Comprehensive Day/Night Cycle system for Unity (URP or Built-in pipeline).
/// Attach this script to an empty GameObject in your scene and assign references
/// through the Inspector. All features are optional — unassigned references are
/// safely skipped.
///
/// Default cycle: 60 seconds = 1 full day (great for testing).
/// Time is represented as a 0-1 float:
///   0.00 = midnight  |  0.25 = sunrise  |  0.50 = noon  |  0.75 = sunset
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // TIME SETTINGS
    // -------------------------------------------------------------------------

    [Header("Time Settings")]
    [Space]

    [Tooltip("How many real-time seconds make up one full day/night cycle. Default is 60 for quick testing.")]
    public float dayLengthInSeconds = 60f;

    [Range(0f, 1f)]
    [Tooltip("The time of day when the scene starts. 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.")]
    public float startTimeOfDay = 0.25f;

    // -------------------------------------------------------------------------
    // SUN SETTINGS
    // -------------------------------------------------------------------------

    [Header("Sun Settings")]
    [Space]

    [Tooltip("Directional light that acts as the Sun.")]
    public Light sunLight;

    [Tooltip("Sun color across the full day cycle. Use warm oranges at sunrise/sunset, bright white at noon.")]
    public Gradient sunColor;

    [Tooltip("Sun intensity (brightness) across the full day cycle. Should be 0 at night.")]
    public AnimationCurve sunIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),
        new Keyframe(0.20f, 0.00f),
        new Keyframe(0.25f, 0.30f),
        new Keyframe(0.50f, 1.20f),
        new Keyframe(0.75f, 0.30f),
        new Keyframe(0.80f, 0.00f),
        new Keyframe(1.00f, 0.00f)
    );

    [Range(0f, 10f)]
    [Tooltip("Sun disc size for procedural skybox materials that expose a '_SunSize' property.")]
    public float sunSize = 0.04f;

    [Tooltip("X-axis rotation offset for the Sun's path across the sky. Use this to tilt the arc.")]
    public float sunRotationAxisX = 0f;

    [Tooltip("Y-axis (heading) for the Sun's rotation. 170 = slightly south of straight.")]
    public float sunRotationAxisY = 170f;

    // -------------------------------------------------------------------------
    // MOON SETTINGS
    // -------------------------------------------------------------------------

    [Header("Moon Settings")]
    [Space]

    [Tooltip("Directional light that acts as the Moon. Will be 180 degrees offset from the Sun.")]
    public Light moonLight;

    [Tooltip("Moon color across the full day cycle. Cool blue/silver tones work well.")]
    public Gradient moonColor;

    [Tooltip("Moon intensity (brightness) across the full day cycle. Should be 0 during the day.")]
    public AnimationCurve moonIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.15f),
        new Keyframe(0.20f, 0.15f),
        new Keyframe(0.25f, 0.00f),
        new Keyframe(0.75f, 0.00f),
        new Keyframe(0.80f, 0.15f),
        new Keyframe(1.00f, 0.15f)
    );

    [Tooltip("Y-axis (heading) for the Moon's rotation.")]
    public float moonRotationAxisY = 170f;

    // -------------------------------------------------------------------------
    // STARS SETTINGS
    // -------------------------------------------------------------------------

    [Header("Stars Settings")]
    [Space]

    [Tooltip("Particle system used to render the star field. Optional — leave empty to skip.")]
    public ParticleSystem starsParticleSystem;

    [Tooltip("Material used for star rendering (e.g. a star skybox layer). Its alpha/emission will be controlled by the visibility curve.")]
    public Material starMaterial;

    [Tooltip("Controls star visibility over the day cycle. 1 = fully visible (night), 0 = hidden (day).")]
    public AnimationCurve starVisibility = new AnimationCurve(
        new Keyframe(0.00f, 1.00f),
        new Keyframe(0.20f, 1.00f),
        new Keyframe(0.25f, 0.00f),
        new Keyframe(0.75f, 0.00f),
        new Keyframe(0.80f, 1.00f),
        new Keyframe(1.00f, 1.00f)
    );

    [Tooltip("How quickly stars fade in or out (lerp speed).")]
    [Range(0.1f, 10f)]
    public float starFadeSpeed = 2f;

    [Tooltip("Base tint color applied to the stars.")]
    public Color starColor = new Color(0.9f, 0.95f, 1.0f, 1.0f);

    [Tooltip("Maximum brightness (emission intensity) of the stars.")]
    [Range(0f, 5f)]
    public float starIntensity = 1.0f;

    // Private: current star alpha for smooth lerping
    private float _currentStarAlpha = 0f;

    // Private: tracks last skybox exposure to rate-limit DynamicGI updates
    private float _lastSkyboxExposure = -1f;

    // -------------------------------------------------------------------------
    // CLOUD SETTINGS
    // -------------------------------------------------------------------------

    [Header("Cloud Settings")]
    [Space]

    [Tooltip("Enable or disable the cloud system entirely.")]
    public bool enableClouds = true;

    [Tooltip("Material used for the cloud layer. UV offset will be scrolled to simulate movement.")]
    public Material cloudMaterial;

    [Tooltip("How fast the clouds move/scroll across the sky.")]
    [Range(0f, 1f)]
    public float cloudSpeed = 0.005f;

    [Tooltip("Direction the clouds travel (XY maps to UV offset direction).")]
    public Vector2 cloudDirection = new Vector2(1f, 0f);

    [Tooltip("Density/coverage of the cloud layer. Maps to the material's '_Density' or '_Coverage' property if available.")]
    [Range(0f, 1f)]
    public float cloudDensity = 0.5f;

    [Tooltip("Height of the cloud layer in world units. Maps to '_Height' on the cloud material if available.")]
    public float cloudHeight = 500f;

    [Tooltip("Overall cloud opacity (alpha). 1 = fully opaque, 0 = invisible.")]
    [Range(0f, 1f)]
    public float cloudAlpha = 0.85f;

    [Tooltip("Cloud color during the middle of the day.")]
    public Color cloudDayColor = new Color(1f, 1f, 1f, 1f);

    [Tooltip("Cloud color at sunrise and sunset.")]
    public Color cloudSunsetColor = new Color(1.0f, 0.6f, 0.3f, 1f);

    [Tooltip("Cloud color during night-time.")]
    public Color cloudNightColor = new Color(0.1f, 0.1f, 0.2f, 1f);

    [Tooltip("Curve controlling how cloud color blends between day/sunset/night over the cycle. 0 = night blend, 1 = day blend.")]
    public AnimationCurve cloudColorBlend = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),
        new Keyframe(0.20f, 0.00f),
        new Keyframe(0.25f, 0.50f),
        new Keyframe(0.50f, 1.00f),
        new Keyframe(0.75f, 0.50f),
        new Keyframe(0.80f, 0.00f),
        new Keyframe(1.00f, 0.00f)
    );

    // Private: accumulated UV offset for cloud scrolling
    private Vector2 _cloudUVOffset = Vector2.zero;

    // -------------------------------------------------------------------------
    // AMBIENT LIGHTING
    // -------------------------------------------------------------------------

    [Header("Ambient Lighting")]
    [Space]

    [Tooltip("Ambient light color across the full day cycle. Dark blue at night, soft warm at sunrise/sunset, bright at noon.")]
    public Gradient ambientColor;

    [Tooltip("Ambient light intensity multiplier over the cycle.")]
    public AnimationCurve ambientIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.05f),
        new Keyframe(0.25f, 0.30f),
        new Keyframe(0.50f, 1.00f),
        new Keyframe(0.75f, 0.30f),
        new Keyframe(1.00f, 0.05f)
    );

    // -------------------------------------------------------------------------
    // SKYBOX SETTINGS
    // -------------------------------------------------------------------------

    [Header("Skybox Settings")]
    [Space]

    [Tooltip("The skybox material. For procedural skyboxes, exposure and sun size are updated automatically.")]
    public Material skyboxMaterial;

    [Tooltip("Skybox exposure (brightness) over the cycle. Higher at noon, lower at night.")]
    public AnimationCurve skyboxExposure = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),
        new Keyframe(0.25f, 0.40f),
        new Keyframe(0.50f, 1.30f),
        new Keyframe(0.75f, 0.40f),
        new Keyframe(1.00f, 0.00f)
    );

    [Tooltip("Optional tint color applied to the skybox material over the cycle via the '_Tint' or '_SkyTint' property.")]
    public Gradient skyboxTint;

    // -------------------------------------------------------------------------
    // FOG SETTINGS
    // -------------------------------------------------------------------------

    [Header("Fog Settings")]
    [Space]

    [Tooltip("Enable or disable Unity's built-in fog.")]
    public bool enableFog = true;

    [Tooltip("Fog color across the day cycle. Should match the sky color at each time of day.")]
    public Gradient fogColor;

    [Tooltip("Fog density over the cycle. Typically higher at sunrise/sunset for atmosphere.")]
    public AnimationCurve fogDensityCurve = new AnimationCurve(
        new Keyframe(0.00f, 0.01f),
        new Keyframe(0.25f, 0.02f),
        new Keyframe(0.50f, 0.005f),
        new Keyframe(0.75f, 0.02f),
        new Keyframe(1.00f, 0.01f)
    );

    // -------------------------------------------------------------------------
    // PRIVATE STATE
    // -------------------------------------------------------------------------

    private float _currentTimeOfDay;
    private bool _isPaused = false;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    void Start()
    {
        _currentTimeOfDay = startTimeOfDay;

        // Initialise default gradients if none have been set in the Inspector
        InitialiseDefaultGradients();

        // Snap everything to the starting time immediately
        ApplyAllSettings();
    }

    void Update()
    {
        if (!_isPaused)
        {
            // Advance time: one full cycle takes dayLengthInSeconds real seconds
            _currentTimeOfDay += Time.deltaTime / dayLengthInSeconds;
            if (_currentTimeOfDay >= 1f)
                _currentTimeOfDay -= 1f;
        }

        ApplyAllSettings();
    }

    // =========================================================================
    // CORE UPDATE METHODS
    // =========================================================================

    /// <summary>Applies all sub-system updates for the current time of day.</summary>
    private void ApplyAllSettings()
    {
        UpdateSun();
        UpdateMoon();
        UpdateStars();
        UpdateClouds();
        UpdateAmbientLighting();
        UpdateSkybox();
        UpdateFog();
    }

    // -------------------------------------------------------------------------
    // SUN
    // -------------------------------------------------------------------------

    private void UpdateSun()
    {
        if (sunLight == null) return;

        // Sun rotates 360° over one full cycle.
        // At t=0.25 (sunrise) the computed angle is 0° (horizon), at t=0.5 (noon) it is 90°
        // (overhead), and at t=0 or t=1 (midnight) it is -90° / 270° — safely below the
        // horizon.  Unity's Quaternion.Euler handles all negative/large angles correctly.
        float angle = (_currentTimeOfDay - 0.25f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(angle + sunRotationAxisX, sunRotationAxisY, 0f);

        // Colour & intensity from curves
        sunLight.color = sunColor != null ? sunColor.Evaluate(_currentTimeOfDay) : Color.white;
        sunLight.intensity = sunIntensity.Evaluate(_currentTimeOfDay);

        // Disable light entirely when below horizon to avoid shadow artefacts
        sunLight.enabled = sunLight.intensity > 0.01f;

        // Update procedural skybox sun size if the material supports it
        if (skyboxMaterial != null && skyboxMaterial.HasProperty("_SunSize"))
            skyboxMaterial.SetFloat("_SunSize", sunSize);
    }

    // -------------------------------------------------------------------------
    // MOON
    // -------------------------------------------------------------------------

    private void UpdateMoon()
    {
        if (moonLight == null) return;

        // Moon is 180° offset from the sun — it rises at sunset and sets at sunrise.
        // At t=0 (midnight) the angle is -270° (== 90°, overhead), at t=0.75 (sunset) it is
        // 0° (horizon).  Like the sun, Unity's Quaternion.Euler resolves all angles correctly.
        float angle = (_currentTimeOfDay - 0.75f) * 360f;
        moonLight.transform.rotation = Quaternion.Euler(angle, moonRotationAxisY, 0f);

        moonLight.color = moonColor != null ? moonColor.Evaluate(_currentTimeOfDay) : new Color(0.7f, 0.8f, 1.0f);
        moonLight.intensity = moonIntensity.Evaluate(_currentTimeOfDay);

        moonLight.enabled = moonLight.intensity > 0.01f;
    }

    // -------------------------------------------------------------------------
    // STARS
    // -------------------------------------------------------------------------

    private void UpdateStars()
    {
        float targetAlpha = starVisibility.Evaluate(_currentTimeOfDay) * starIntensity;

        // Smoothly lerp toward the target alpha
        _currentStarAlpha = Mathf.Lerp(_currentStarAlpha, targetAlpha, Time.deltaTime * starFadeSpeed);

        // --- Particle System stars ---
        if (starsParticleSystem != null)
        {
            var main = starsParticleSystem.main;
            Color c = starColor;
            c.a = _currentStarAlpha;
            main.startColor = c;

            // Activate/deactivate the particle system based on visibility
            if (_currentStarAlpha > 0.01f && !starsParticleSystem.isPlaying)
                starsParticleSystem.Play();
            else if (_currentStarAlpha <= 0.01f && starsParticleSystem.isPlaying)
                starsParticleSystem.Stop();
        }

        // --- Material-based stars ---
        if (starMaterial != null)
        {
            // Try common emission/alpha property names
            if (starMaterial.HasProperty("_Color"))
            {
                Color c = starColor;
                c.a = _currentStarAlpha;
                starMaterial.SetColor("_Color", c);
            }

            if (starMaterial.HasProperty("_EmissionColor"))
            {
                starMaterial.SetColor("_EmissionColor", starColor * _currentStarAlpha);
            }

            if (starMaterial.HasProperty("_Intensity"))
                starMaterial.SetFloat("_Intensity", _currentStarAlpha);

            if (starMaterial.HasProperty("_Alpha"))
                starMaterial.SetFloat("_Alpha", _currentStarAlpha);
        }
    }

    // -------------------------------------------------------------------------
    // CLOUDS
    // -------------------------------------------------------------------------

    private void UpdateClouds()
    {
        if (!enableClouds || cloudMaterial == null) return;

        // Scroll UV offset to simulate cloud movement
        _cloudUVOffset += cloudDirection.normalized * cloudSpeed * Time.deltaTime;

        if (cloudMaterial.HasProperty("_MainTex"))
            cloudMaterial.SetTextureOffset("_MainTex", _cloudUVOffset);

        // Blend cloud colour: day <-> sunset <-> night
        float blend = cloudColorBlend.Evaluate(_currentTimeOfDay); // 0 = night, 1 = day
        Color blendedColor;
        if (blend <= 0.5f)
        {
            // Night → Sunset blend (blend 0..0.5 maps to 0..1 within this range)
            blendedColor = Color.Lerp(cloudNightColor, cloudSunsetColor, blend * 2f);
        }
        else
        {
            // Sunset → Day blend (blend 0.5..1 maps to 0..1 within this range)
            blendedColor = Color.Lerp(cloudSunsetColor, cloudDayColor, (blend - 0.5f) * 2f);
        }
        blendedColor.a = cloudAlpha;

        if (cloudMaterial.HasProperty("_Color"))
            cloudMaterial.SetColor("_Color", blendedColor);

        if (cloudMaterial.HasProperty("_TintColor"))
            cloudMaterial.SetColor("_TintColor", blendedColor);

        // Optional density and height properties
        if (cloudMaterial.HasProperty("_Density"))
            cloudMaterial.SetFloat("_Density", cloudDensity);

        if (cloudMaterial.HasProperty("_Height"))
            cloudMaterial.SetFloat("_Height", cloudHeight);
    }

    // -------------------------------------------------------------------------
    // AMBIENT LIGHTING
    // -------------------------------------------------------------------------

    private void UpdateAmbientLighting()
    {
        if (ambientColor != null)
            RenderSettings.ambientLight = ambientColor.Evaluate(_currentTimeOfDay) * ambientIntensity.Evaluate(_currentTimeOfDay);
        else
            RenderSettings.ambientIntensity = ambientIntensity.Evaluate(_currentTimeOfDay);
    }

    // -------------------------------------------------------------------------
    // SKYBOX
    // -------------------------------------------------------------------------

    private void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        // Exposure
        if (skyboxMaterial.HasProperty("_Exposure"))
            skyboxMaterial.SetFloat("_Exposure", skyboxExposure.Evaluate(_currentTimeOfDay));

        // Atmosphere thickness (procedural skybox)
        if (skyboxMaterial.HasProperty("_AtmosphereThickness"))
        {
            // Thin at night, full at day
            float thickness = Mathf.Lerp(0.1f, 1.0f, skyboxExposure.Evaluate(_currentTimeOfDay));
            skyboxMaterial.SetFloat("_AtmosphereThickness", thickness);
        }

        // Tint
        if (skyboxTint != null)
        {
            Color tint = skyboxTint.Evaluate(_currentTimeOfDay);
            if (skyboxMaterial.HasProperty("_Tint"))
                skyboxMaterial.SetColor("_Tint", tint);
            if (skyboxMaterial.HasProperty("_SkyTint"))
                skyboxMaterial.SetColor("_SkyTint", tint);
        }

        // Tell Unity to re-render the skybox reflection probe only when exposure
        // changes meaningfully (threshold 0.01) to avoid a costly per-frame update.
        float currentExposure = skyboxExposure.Evaluate(_currentTimeOfDay);
        if (Mathf.Abs(currentExposure - _lastSkyboxExposure) > 0.01f)
        {
            DynamicGI.UpdateEnvironment();
            _lastSkyboxExposure = currentExposure;
        }
    }

    // -------------------------------------------------------------------------
    // FOG
    // -------------------------------------------------------------------------

    private void UpdateFog()
    {
        RenderSettings.fog = enableFog;

        if (!enableFog) return;

        if (fogColor != null)
            RenderSettings.fogColor = fogColor.Evaluate(_currentTimeOfDay);

        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensityCurve.Evaluate(_currentTimeOfDay);
    }

    // =========================================================================
    // DEFAULT GRADIENTS
    // =========================================================================

    /// <summary>
    /// Populates gradient fields with sensible defaults if the user hasn't set
    /// them in the Inspector. This prevents NullReferenceExceptions at runtime.
    /// </summary>
    private void InitialiseDefaultGradients()
    {
        if (sunColor == null || sunColor.colorKeys.Length == 0)
        {
            sunColor = new Gradient();
            sunColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0f, 0f, 0f), 0.00f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.23f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0.30f),
                    new GradientColorKey(new Color(1f, 1f, 0.95f), 0.50f),
                    new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0.70f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.77f),
                    new GradientColorKey(new Color(0f, 0f, 0f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        if (moonColor == null || moonColor.colorKeys.Length == 0)
        {
            moonColor = new Gradient();
            moonColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.6f, 0.7f, 1.0f), 0.00f),
                    new GradientColorKey(new Color(0.6f, 0.7f, 1.0f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        if (ambientColor == null || ambientColor.colorKeys.Length == 0)
        {
            ambientColor = new Gradient();
            ambientColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.00f),
                    new GradientColorKey(new Color(0.4f, 0.3f, 0.4f), 0.25f),
                    new GradientColorKey(new Color(0.6f, 0.7f, 0.9f), 0.50f),
                    new GradientColorKey(new Color(0.4f, 0.3f, 0.4f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        if (fogColor == null || fogColor.colorKeys.Length == 0)
        {
            fogColor = new Gradient();
            fogColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 0.00f),
                    new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.25f),
                    new GradientColorKey(new Color(0.7f, 0.85f, 1.0f), 0.50f),
                    new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.75f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.05f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        if (skyboxTint == null || skyboxTint.colorKeys.Length == 0)
        {
            skyboxTint = new Gradient();
            skyboxTint.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 0.00f),
                    new GradientColorKey(new Color(0.5f, 0.3f, 0.2f), 0.25f),
                    new GradientColorKey(new Color(0.4f, 0.6f, 1.0f), 0.50f),
                    new GradientColorKey(new Color(0.5f, 0.3f, 0.2f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.1f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }

    // =========================================================================
    // PUBLIC UTILITY METHODS
    // =========================================================================

    /// <summary>Returns the current time of day as a 0-1 float (0 = midnight, 0.5 = noon).</summary>
    public float GetCurrentTimeOfDay()
    {
        return _currentTimeOfDay;
    }

    /// <summary>
    /// Returns the current in-game time formatted as a 24-hour string (e.g. "14:30").
    /// The cycle starts at midnight (0) and noon is at 0.5.
    /// </summary>
    public string GetTimeAsString()
    {
        float totalHours = _currentTimeOfDay * 24f;
        int hours = Mathf.FloorToInt(totalHours) % 24;
        int minutes = Mathf.FloorToInt((totalHours - Mathf.Floor(totalHours)) * 60f);
        return string.Format("{0:D2}:{1:D2}", hours, minutes);
    }

    /// <summary>Returns true if the current time is between sunrise (0.25) and sunset (0.75).</summary>
    public bool IsDay()
    {
        return _currentTimeOfDay >= 0.25f && _currentTimeOfDay < 0.75f;
    }

    /// <summary>Returns true if the current time is outside the sunrise-to-sunset window.</summary>
    public bool IsNight()
    {
        return !IsDay();
    }

    /// <summary>Instantly sets the time of day. Value must be in the 0-1 range.</summary>
    public void SetTimeOfDay(float time)
    {
        _currentTimeOfDay = Mathf.Repeat(time, 1f);
        ApplyAllSettings();
    }

    /// <summary>Changes the full-cycle duration at runtime.</summary>
    public void SetDayLength(float seconds)
    {
        dayLengthInSeconds = Mathf.Max(1f, seconds);
    }

    /// <summary>Pauses the day/night cycle. All visual settings remain at the current time.</summary>
    public void PauseCycle()
    {
        _isPaused = true;
    }

    /// <summary>Resumes the day/night cycle after it was paused.</summary>
    public void ResumeCycle()
    {
        _isPaused = false;
    }
}
