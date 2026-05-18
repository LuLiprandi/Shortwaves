using UnityEngine;

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

    public string Decode(int code)
    {
        foreach (var e in entries)
            if (e.code == code) return e.letter;
        return "?";
    }
}
