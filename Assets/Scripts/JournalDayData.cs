using UnityEngine;

/// <summary>
/// ScriptableObject — toute la narration d'un jour au même endroit.
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

    [Header("Décodage radio")]
    [Tooltip("Message chiffré affiché dans l'onglet DECODAGE.")]
    [TextArea(2, 4)]
    public string OfficialMessageCoded = "";

    [Tooltip("Version déchiffrée attendue — sert à valider la réponse du joueur.")]
    [TextArea(2, 4)]
    public string OfficialMessageDecoded = "";

    [Header("Production")]
    [Tooltip("Note interne — résumé de l'anomalie du jour. Non utilisée en jeu.")]
    [TextArea(2, 6)]
    public string AnomalyNote = "";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(PreAnomalyThoughts))
            Debug.LogWarning($"[JournalDayData] Jour {DayNumber} — PreAnomalyThoughts est vide.", this);

        if (string.IsNullOrWhiteSpace(PostAnomalyThoughts))
            Debug.LogWarning($"[JournalDayData] Jour {DayNumber} — PostAnomalyThoughts est vide.", this);
    }
#endif
}
