using UnityEngine;

public enum DecryptionMode
{
    Guided  = 0,
    Partial = 1,
    Full    = 2,
}

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

    public DayConfig GetDay(int day)
    {
        if (days == null) return null;
        foreach (var d in days)
            if (d.day == day) return d;
        return null;
    }

    public bool HasDecoder(int day)
    {
        var cfg = GetDay(day);
        return cfg != null && cfg.hasDecoder;
    }

    public DecryptionMode GetMode(int day)
    {
        var cfg = GetDay(day);
        return cfg != null ? cfg.mode : DecryptionMode.Guided;
    }
}
