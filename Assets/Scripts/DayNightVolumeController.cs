using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives a URP Volume based on the current time of day, creating atmospheric
/// post-processing that shifts with the sun: warm bloom at sunrise/sunset, cool
/// desaturated tones at night, neutral bright tones at noon.
///
/// Place this component on the same GameObject as (or alongside) DayNightCycle.
/// WeatherManager's volume runs at a higher priority so weather conditions can
/// smoothly blend over the top of these base time-of-day effects.
/// </summary>
[AddComponentMenu("Environment/Day Night Volume Controller")]
public class DayNightVolumeController : MonoBehaviour
{
    // ─── REFERENCES ──────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("The DayNightCycle that provides the current time of day (0-1).")]
    public DayNightCycle dayNightCycle;

    // ─── BLOOM ───────────────────────────────────────────────────────

    [Header("Bloom")]
    [Tooltip("Bloom intensity over the day (x = time 0-1, y = intensity). Peaks at sunrise and sunset.")]
    public AnimationCurve bloomIntensityCurve;

    [Tooltip("Bloom luminance threshold over the day. Lower at night so dim stars can bloom.")]
    public AnimationCurve bloomThresholdCurve;

    // ─── COLOR ADJUSTMENTS ───────────────────────────────────────────

    [Header("Color Adjustments")]
    [Tooltip("Post exposure in EV over the day. Negative at night, slight boost at sunrise/sunset.")]
    public AnimationCurve postExposureCurve;

    [Tooltip("Contrast adjustment over the day (-100 to 100). Higher contrast at noon.")]
    public AnimationCurve contrastCurve;

    [Tooltip("Saturation adjustment over the day (-100 to 100). Desaturated at night, vivid at sunrise/sunset.")]
    public AnimationCurve saturationCurve;

    [Tooltip("Color filter tint gradient over the day. Warm gold at sunrise/sunset, cool blue at night.")]
    public Gradient colorFilterGradient;

    // ─── VIGNETTE ────────────────────────────────────────────────────

    [Header("Vignette")]
    [Tooltip("Vignette intensity over the day. Stronger at night for a darker, cinematic feel.")]
    public AnimationCurve vignetteIntensityCurve;

    // ─── PRIVATE STATE ───────────────────────────────────────────────

    private Volume _volume;
    private VolumeProfile _profile;
    private Bloom _bloom;
    private Vignette _vignette;
    private ColorAdjustments _colorAdjustments;

    // ─── LIFECYCLE ───────────────────────────────────────────────────

    void Reset()
    {
        SetDefaultCurves();
    }

    void Start()
    {
        // Auto-find DayNightCycle on same or parent GameObject if not assigned
        if (dayNightCycle == null)
            dayNightCycle = GetComponentInParent<DayNightCycle>();
        if (dayNightCycle == null)
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();

        // Ensure curves are initialised even if Reset() wasn't called
        if (bloomIntensityCurve == null || bloomIntensityCurve.length == 0)
            SetDefaultCurves();

        SetupVolume();
    }

    void Update()
    {
        if (_bloom == null) return;

        float t = dayNightCycle != null ? dayNightCycle.GetCurrentTimeOfDay() : 0.5f;

        _bloom.intensity.Override(bloomIntensityCurve.Evaluate(t));
        _bloom.threshold.Override(bloomThresholdCurve.Evaluate(t));

        if (_vignette != null)
            _vignette.intensity.Override(vignetteIntensityCurve.Evaluate(t));

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.Override(postExposureCurve.Evaluate(t));
            _colorAdjustments.contrast.Override(contrastCurve.Evaluate(t));
            _colorAdjustments.saturation.Override(saturationCurve.Evaluate(t));
            _colorAdjustments.colorFilter.Override(colorFilterGradient.Evaluate(t));
        }
    }

    void OnDestroy()
    {
        if (_volume != null)
            Destroy(_volume.gameObject);
        if (_profile != null)
            Destroy(_profile);
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────

    private void SetupVolume()
    {
        GameObject volumeGO = new GameObject("DayNightVolume");
        volumeGO.transform.SetParent(transform, false);

        _volume = volumeGO.AddComponent<Volume>();
        _volume.isGlobal = true;
        // Priority 0 — lowest, so WeatherManager's volume (priority 1) can
        // gradually blend over the top via its weight/volumeInfluence field.
        _volume.priority = 0f;

        _profile = ScriptableObject.CreateInstance<VolumeProfile>();
        _volume.profile = _profile;

        _bloom = _profile.Add<Bloom>(true);
        // Scatter controls how far bloom spreads across the screen (0=tight, 1=full screen).
        // Setting 0.7 ensures bloom reaches the screen edges consistently.
        _bloom.scatter.Override(0.7f);
        _vignette = _profile.Add<Vignette>(true);
        _colorAdjustments = _profile.Add<ColorAdjustments>(true);
    }

    /// <summary>
    /// Initialises all curves/gradients with sensible atmospheric defaults.
    /// Called automatically from Reset() (first add) and from Start() as a safety net.
    /// </summary>
    private void SetDefaultCurves()
    {
        // ── Bloom intensity: subtle peaks at sunrise/sunset, very low at noon and night.
        // Kept low so that cloud edges and skybox detail remain crisp.
        bloomIntensityCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.08f),
            new Keyframe(0.20f, 0.15f),
            new Keyframe(0.25f, 0.40f),  // sunrise peak
            new Keyframe(0.35f, 0.15f),
            new Keyframe(0.50f, 0.10f),  // noon — keep bloom minimal
            new Keyframe(0.65f, 0.15f),
            new Keyframe(0.75f, 0.40f),  // sunset peak
            new Keyframe(0.85f, 0.12f),
            new Keyframe(1.00f, 0.08f)
        );

        // ── Bloom threshold: high so only true bright sources (sun disc, moon) bloom.
        // Raised significantly to prevent clouds and sky gradients from blooming.
        bloomThresholdCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.90f),  // night — only the moon disc blooms
            new Keyframe(0.20f, 0.85f),
            new Keyframe(0.30f, 1.10f),  // daytime — only the sun disc blooms
            new Keyframe(0.70f, 1.10f),
            new Keyframe(0.80f, 0.85f),
            new Keyframe(1.00f, 0.90f)
        );

        // ── Post exposure: dark at night, neutral at noon, slight lift at sunrise/sunset
        postExposureCurve = new AnimationCurve(
            new Keyframe(0.00f, -0.50f), // midnight — dark
            new Keyframe(0.20f, -0.20f),
            new Keyframe(0.25f,  0.20f), // sunrise — warm lift
            new Keyframe(0.35f,  0.05f),
            new Keyframe(0.50f,  0.00f), // noon — neutral
            new Keyframe(0.65f,  0.05f),
            new Keyframe(0.75f,  0.15f), // sunset — warm lift
            new Keyframe(0.85f, -0.20f),
            new Keyframe(1.00f, -0.50f)
        );

        // ── Contrast: higher at noon (crisp), slightly lower at night (soft)
        contrastCurve = new AnimationCurve(
            new Keyframe(0.00f, -5f),  // night — soft
            new Keyframe(0.25f,  5f),  // sunrise — rising
            new Keyframe(0.50f, 10f),  // noon — crisp
            new Keyframe(0.75f,  5f),  // sunset
            new Keyframe(1.00f, -5f)
        );

        // ── Saturation: vivid at sunrise/sunset, desaturated at night, normal at noon
        saturationCurve = new AnimationCurve(
            new Keyframe(0.00f, -30f), // midnight — desaturated, moonlit world
            new Keyframe(0.20f,  -5f),
            new Keyframe(0.25f,  20f), // sunrise — vivid warm colours
            new Keyframe(0.40f,   0f),
            new Keyframe(0.50f,   5f), // noon — slightly vivid
            new Keyframe(0.60f,   0f),
            new Keyframe(0.75f,  25f), // sunset — most vivid
            new Keyframe(0.85f,  -5f),
            new Keyframe(1.00f, -30f)
        );

        // ── Vignette: slight cinematic edges at night, barely visible at noon.
        // Reduced compared to previous values to avoid contributing to the enclosed/foggy feel.
        vignetteIntensityCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.30f), // midnight
            new Keyframe(0.20f, 0.18f),
            new Keyframe(0.30f, 0.07f),
            new Keyframe(0.50f, 0.05f), // noon
            new Keyframe(0.70f, 0.07f),
            new Keyframe(0.80f, 0.18f),
            new Keyframe(1.00f, 0.30f)
        );

        // ── Color filter gradient: cool blue at night, warm gold at sunrise/sunset, white at noon
        colorFilterGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(new Color(0.60f, 0.70f, 1.00f), 0.00f), // midnight — cool blue
            new GradientColorKey(new Color(0.80f, 0.65f, 0.50f), 0.20f), // pre-dawn glow
            new GradientColorKey(new Color(1.00f, 0.82f, 0.55f), 0.25f), // sunrise — warm gold
            new GradientColorKey(new Color(1.00f, 0.96f, 0.88f), 0.38f), // morning — warm white
            new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.50f), // noon — neutral white
            new GradientColorKey(new Color(1.00f, 0.96f, 0.88f), 0.62f), // afternoon
            new GradientColorKey(new Color(1.00f, 0.75f, 0.45f), 0.75f), // sunset — orange gold
            new GradientColorKey(new Color(0.75f, 0.60f, 0.80f), 0.85f), // dusk — purple
            new GradientColorKey(new Color(0.60f, 0.70f, 1.00f), 1.00f), // midnight — cool blue
        };
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f),
        };
        colorFilterGradient.SetKeys(colorKeys, alphaKeys);
    }
}
