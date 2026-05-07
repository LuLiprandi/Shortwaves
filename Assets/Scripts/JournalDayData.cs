using UnityEngine;

/// <summary>
/// ScriptableObject — toute la narration et les données de décodage d'un jour.
/// Créer via : clic droit > Create > Shortwaves > Journal Day Data
/// </summary>
[CreateAssetMenu(fileName = "JournalDay01", menuName = "Shortwaves/Journal Day Data")]
public class JournalDayData : ScriptableObject
{
    [Header("Identification")]
    public int DayNumber = 1;

    [Header("Journal — pensées de l'opérateur")]
    [TextArea(4, 10)]
    public string PreAnomalyThoughts = "";

    [TextArea(4, 10)]
    public string PostAnomalyThoughts = "";

    [Header("Décodage — message radio")]
    [Tooltip(
        "Séquence de codes chiffrés du message radio, séparés par des espaces.\n" +
        "Exemple : 2 5 14 10 21 18\n" +
        "Chaque nombre correspond à une lettre selon la clé de décodage du jeu.")]
    public string CodeSequenceRaw = "";

    [Tooltip(
        "Indices (base 0) des codes masqués en mode Partial, séparés par des espaces.\n" +
        "Exemple : 2 5 (masque les 3e et 6e codes)\n" +
        "Laisser vide en mode Guided ou Full.")]
    public string HiddenSlotIndicesRaw = "";

    [Header("Décodage — solution")]
    [Tooltip("Message déchiffré attendu. Majuscules/minuscules ignorées, espaces ignorés.\nExemple : BONJOUR ou BON JOUR")]
    [TextArea(1, 3)]
    public string OfficialMessageDecoded = "";

    [Header("Production")]
    [Tooltip("Note interne — résumé de l'anomalie du jour. Non utilisée en jeu.")]
    [TextArea(2, 6)]
    public string AnomalyNote = "";

    // ── Parsed accessors ─────────────────────────────────────────────────────

    /// <summary>Returns CodeSequenceRaw parsed as an int array. Invalid tokens are skipped.</summary>
    public int[] CodeSequence
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CodeSequenceRaw)) return System.Array.Empty<int>();
            var tokens = CodeSequenceRaw.Trim().Split(new[] { ' ', ',', ';' },
                System.StringSplitOptions.RemoveEmptyEntries);
            var list = new System.Collections.Generic.List<int>(tokens.Length);
            foreach (var t in tokens)
                if (int.TryParse(t, out int v)) list.Add(v);
            return list.ToArray();
        }
    }

    /// <summary>Returns HiddenSlotIndicesRaw parsed as an int array. Invalid tokens are skipped.</summary>
    public int[] HiddenSlotIndices
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HiddenSlotIndicesRaw)) return System.Array.Empty<int>();
            var tokens = HiddenSlotIndicesRaw.Trim().Split(new[] { ' ', ',', ';' },
                System.StringSplitOptions.RemoveEmptyEntries);
            var list = new System.Collections.Generic.List<int>(tokens.Length);
            foreach (var t in tokens)
                if (int.TryParse(t, out int v)) list.Add(v);
            return list.ToArray();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(PreAnomalyThoughts))
            Debug.LogWarning($"[JournalDayData] Jour {DayNumber} — PreAnomalyThoughts est vide.", this);

        if (string.IsNullOrWhiteSpace(PostAnomalyThoughts))
            Debug.LogWarning($"[JournalDayData] Jour {DayNumber} — PostAnomalyThoughts est vide.", this);

        if (DayNumber <= 3 && string.IsNullOrWhiteSpace(CodeSequenceRaw))
            Debug.LogWarning($"[JournalDayData] Jour {DayNumber} — CodeSequenceRaw est vide alors que le décodage est prévu.", this);
    }
#endif
}
