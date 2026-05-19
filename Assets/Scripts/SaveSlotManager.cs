using UnityEngine;
using Shortwaves;

public static class SaveSlotManager
{
    // ── PlayerPrefs keys — must stay in sync with GameStateManager ────────────

    private const string PrefDay        = "gsm_day";
    private const string PrefDay2Choice = "gsm_day2choice";

    // ── Snapshot definitions ──────────────────────────────────────────────────

    public readonly struct SaveSnapshot
    {
        public readonly string Label;
        public readonly string Description;
        public readonly int    Day;
        public readonly int    Day2Choice; // 0 = None, 1 = Open, 2 = Ignore

        public SaveSnapshot(string label, string description, int day, int day2Choice = 0)
        {
            Label       = label;
            Description = description;
            Day         = day;
            Day2Choice  = day2Choice;
        }
    }

    public static readonly SaveSnapshot[] Slots = new[]
    {
        new SaveSnapshot(
            label:       "Intro — Jour 1",
            description: "Début du jeu. Anomalie radio du Jour 1 non encore déclenchée.",
            day:         1
        ),
        new SaveSnapshot(
            label:       "Jour 2 — Anomalie",
            description: "Jour 2 en cours. L'anomalie radio peut être déclenchée depuis le journal.",
            day:         2
        ),
        new SaveSnapshot(
            label:       "Jour 3 — Le Sacrifice",
            description: "Jour 3 en cours. Porte ouverte au Jour 2. L'anomalie du chuchotement est disponible.",
            day:         3,
            day2Choice:  1
        ),
        new SaveSnapshot(
            label:       "Jour 4 — Fin A (porte ouverte)",
            description: "Fin A : le joueur a ouvert la porte au Jour 2.",
            day:         4,
            day2Choice:  1
        ),
        new SaveSnapshot(
            label:       "Jour 4 — Fin B (porte ignorée)",
            description: "Fin B : le joueur a ignoré la porte au Jour 2.",
            day:         4,
            day2Choice:  2
        ),
    };

    // ── API ───────────────────────────────────────────────────────────────────

    public static void Apply(SaveSnapshot snapshot)
    {
        PlayerPrefs.SetInt(PrefDay,        snapshot.Day);
        PlayerPrefs.SetInt(PrefDay2Choice, snapshot.Day2Choice);
        PlayerPrefs.Save();

        // Si le GameStateManager existe déjà en scène (DontDestroyOnLoad),
        // on force la relecture des PlayerPrefs pour que CurrentDay soit immédiatement
        // à jour — notamment pour RadioSystem.Start() qui lit le jour au chargement.
        GameStateManager.Instance?.ReloadState();
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PrefDay);
        PlayerPrefs.DeleteKey(PrefDay2Choice);
        PlayerPrefs.Save();
    }

    public static int GetCurrentDay() => PlayerPrefs.GetInt(PrefDay, 1);
}
