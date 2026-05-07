using UnityEngine;

/// <summary>
/// Enum defining the three decryption challenge modes for the journal decoder tab.
/// </summary>
public enum DecryptionMode
{
    /// <summary>All code numbers are pre-filled. The player only translates numbers to letters.</summary>
    Guided  = 0,

    /// <summary>Some code numbers are hidden (shown as __). The player must fill those in from the radio.</summary>
    Partial = 1,

    /// <summary>The code grid is fully empty. The player transcribes every number from the radio, then translates.</summary>
    Full    = 2,
}

/// <summary>
/// ScriptableObject — configures the journal per day (decoder visibility and mode).
/// Create via Assets > Create > Shortwaves > Journal Config.
/// </summary>
[CreateAssetMenu(fileName = "JournalConfig", menuName = "Shortwaves/Journal Config")]
public class JournalConfig : ScriptableObject
{
    [System.Serializable]
    public class DayConfig
    {
        [Tooltip("Day number this config applies to (1, 2, 3 …).")]
        public int day = 1;

        [Tooltip("Show the DECODAGE tab for this day.")]
        public bool hasDecoder = true;

        [Tooltip("Decryption challenge mode for this day.")]
        public DecryptionMode mode = DecryptionMode.Guided;
    }

    [Tooltip("One entry per day that has specific settings. Days not listed use default behaviour (no decoder).")]
    public DayConfig[] days;

    /// <summary>Returns the config for a given day, or null if none is defined.</summary>
    public DayConfig GetDay(int day)
    {
        if (days == null) return null;
        foreach (var d in days)
            if (d.day == day) return d;
        return null;
    }

    /// <summary>Returns true when this day should show the decoder tab.</summary>
    public bool HasDecoder(int day)
    {
        var cfg = GetDay(day);
        return cfg != null && cfg.hasDecoder;
    }

    /// <summary>Returns the DecryptionMode for a given day (defaults to Guided).</summary>
    public DecryptionMode GetMode(int day)
    {
        var cfg = GetDay(day);
        return cfg != null ? cfg.mode : DecryptionMode.Guided;
    }
}
