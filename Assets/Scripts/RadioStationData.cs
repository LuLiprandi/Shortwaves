using UnityEngine;

/// <summary>Data asset defining a radio station's frequency and decoding parameters.</summary>
[CreateAssetMenu(fileName = "RadioStation", menuName = "Shortwaves/Radio Station")]
public class RadioStationData : ScriptableObject
{
    [Header("Fréquence")]
    [Tooltip("Fréquence cible en MHz (ex: 96.5)")]
    public float FrequencyMHz = 96.5f;

    [Tooltip("Rayon en MHz pour commencer à entendre le signal")]
    public float ProximityRangeMHz = 0.4f;

    [Tooltip("Rayon en MHz pour déclencher la jauge QTE")]
    public float LockRangeMHz = 0.08f;

    [Header("Audio")]
    [Tooltip("Son de décodage joué quand le signal est capté")]
    public AudioClip DecodingClip;

    [Header("QTE")]
    [Tooltip("Durée en secondes à tenir dans la zone verte pour réussir")]
    public float QTESuccessDuration = 4f;

    [Header("Indice décodé")]
    [Tooltip("Message / clé affiché au joueur quand la station est décodée avec succès")]
    [TextArea(3, 6)]
    public string ClueText = "INDICE : ???";
}
