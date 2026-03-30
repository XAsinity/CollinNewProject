using UnityEngine;

namespace Weather
{
    /// <summary>
    /// A ScriptableObject that groups weather profiles with transition rules so auto-weather
    /// follows realistic meteorological sequences instead of picking randomly.
    ///
    /// Create via: Right-click in Project → Create → Weather → Weather Preset Bundle
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherPresetBundle", menuName = "Weather/Weather Preset Bundle", order = 2)]
    public class WeatherPresetBundle : ScriptableObject
    {
        [Header("Bundle Info")]
        [Tooltip("Display name for this bundle")]
        public string bundleName = "Default Weather Bundle";

        [TextArea(2, 4)]
        [Tooltip("Description of this weather bundle's behavior")]
        public string description = "";

        [Header("Weather Entries")]
        [Tooltip("All weather entries in this bundle with their transition rules")]
        public WeatherBundleEntry[] entries;

        [Header("Global Settings")]
        [Tooltip("Default transition duration (seconds) if not overridden per-rule")]
        public float defaultTransitionDuration = 90f;

        [Tooltip("Minimum time (seconds) a weather condition stays active before transitioning")]
        public float minimumHoldTime = 120f;

        [Tooltip("Maximum time (seconds) before a transition is forced")]
        public float maximumHoldTime = 480f;

        // ─── RUNTIME HELPERS ────────────────────────────────────────────

        /// <summary>Finds the bundle entry for the given profile, or null if not found.</summary>
        public WeatherBundleEntry FindEntry(WeatherProfile profile)
        {
            if (profile == null || entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry != null && entry.profile == profile)
                    return entry;
            }
            return null;
        }

        /// <summary>Finds the bundle entry whose profile name matches, or null.</summary>
        public WeatherBundleEntry FindEntryByName(string profileName)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry != null && entry.profile != null &&
                    entry.profile.profileName == profileName)
                    return entry;
            }
            return null;
        }
    }

    /// <summary>
    /// One entry in a WeatherPresetBundle — wraps a weather profile with its severity level,
    /// hold-time overrides, and the list of allowed outgoing transitions.
    /// </summary>
    [System.Serializable]
    public class WeatherBundleEntry
    {
        [Tooltip("The weather profile for this entry")]
        public WeatherProfile profile;

        [Tooltip("Severity level (0 = calmest, higher = more intense). " +
                 "Weather naturally moves ±1–2 levels at a time.")]
        [Range(0, 10)]
        public int severityLevel;

        [Tooltip("Allowed transitions from this weather state")]
        public WeatherTransitionRule[] allowedTransitions;

        [Tooltip("Minimum seconds this weather must persist before transitioning. " +
                 "-1 = use the bundle's minimumHoldTime.")]
        public float minHoldTime = -1f;

        [Tooltip("Maximum seconds before transitioning away. " +
                 "-1 = use the bundle's maximumHoldTime.")]
        public float maxHoldTime = -1f;

        /// <summary>
        /// Returns the effective minimum hold time, falling back to the bundle global if
        /// the per-entry value is not set.
        /// </summary>
        public float GetMinHoldTime(WeatherPresetBundle bundle)
            => minHoldTime >= 0f ? minHoldTime : (bundle != null ? bundle.minimumHoldTime : 120f);

        /// <summary>
        /// Returns the effective maximum hold time, falling back to the bundle global if
        /// the per-entry value is not set.
        /// </summary>
        public float GetMaxHoldTime(WeatherPresetBundle bundle)
            => maxHoldTime >= 0f ? maxHoldTime : (bundle != null ? bundle.maximumHoldTime : 480f);
    }

    /// <summary>
    /// One allowed outgoing transition from a WeatherBundleEntry to a target profile,
    /// with optional probability weight, duration override, and time-of-day constraints.
    /// </summary>
    [System.Serializable]
    public class WeatherTransitionRule
    {
        [Tooltip("Target weather profile to transition to")]
        public WeatherProfile target;

        [Tooltip("Relative weight/probability of this transition (higher = more likely)")]
        [Range(0f, 10f)]
        public float weight = 1f;

        [Tooltip("Override transition duration (seconds) for this specific change. " +
                 "-1 = use the bundle's defaultTransitionDuration.")]
        public float transitionDuration = -1f;

        [Tooltip("Only allow this transition during daytime (timeOfDay 0.2–0.8)")]
        public bool dayOnly = false;

        [Tooltip("Only allow this transition during nighttime (timeOfDay outside 0.2–0.8)")]
        public bool nightOnly = false;

        /// <summary>
        /// Returns the effective transition duration, falling back to the bundle default if
        /// the per-rule override is not set.
        /// </summary>
        public float GetTransitionDuration(WeatherPresetBundle bundle)
            => transitionDuration >= 0f ? transitionDuration
               : (bundle != null ? bundle.defaultTransitionDuration : 90f);
    }
}
