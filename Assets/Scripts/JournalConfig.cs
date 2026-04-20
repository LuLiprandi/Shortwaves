using UnityEngine;

/// <summary>
/// ScriptableObject — configure the journal per day.
/// Create via Assets > Create > Shortwaves > Journal Config.
/// </summary>
[CreateAssetMenu(fileName = "JournalConfig", menuName = "Shortwaves/Journal Config")]
public class JournalConfig : ScriptableObject
{
    [System.Serializable]
    public class DayConfig
    {
        [Tooltip("Day number this config applies to (1, 2, 3 ...).")]
        public int day = 1;

        [Tooltip("Show the DECODAGE tab for this day.")]
        public bool hasDecoder = true;

        [Tooltip("Number of digit slots shown in the decoder.")]
        public int slotCount = 6;
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

    /// <summary>Returns the slot count for a given day (default 6).</summary>
    public int SlotCount(int day)
    {
        var cfg = GetDay(day);
        return cfg != null ? cfg.slotCount : 6;
    }
}
