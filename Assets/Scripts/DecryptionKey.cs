using UnityEngine;

/// <summary>
/// ScriptableObject — table de correspondance code → lettre pour tout le jeu.
/// Un seul asset partagé, assigné dans JournalPanel.
/// Créer via : clic droit > Create > Shortwaves > Decryption Key
/// </summary>
[CreateAssetMenu(fileName = "DecryptionKey", menuName = "Shortwaves/Decryption Key")]
public class DecryptionKey : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Code chiffré (ex: 1, 2, 3…)")]
        public int    code;

        [Tooltip("Lettre correspondante (ex: A, B, C…)")]
        public string letter;
    }

    [Tooltip("Table de correspondance code → lettre. Remplir dans l'ordre pour faciliter la lecture.")]
    public Entry[] entries = System.Array.Empty<Entry>();

    /// <summary>Returns the letter for a given code, or '?' if not found.</summary>
    public string Decode(int code)
    {
        foreach (var e in entries)
            if (e.code == code) return e.letter;
        return "?";
    }
}
