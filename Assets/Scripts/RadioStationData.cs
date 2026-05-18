using System;
using UnityEngine;

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
    [Tooltip("Son statique / parasite joué en s'approchant de la fréquence (loop)")]
    public AudioClip ProximityClip;

    [Tooltip("Message vocal joué une seule fois après le succès du QTE — le joueur l'écoute pour décoder l'indice")]
    public AudioClip VoiceClip;

    [Header("QTE")]
    [Tooltip("Durée en secondes à tenir dans la zone verte pour réussir")]
    public float QTESuccessDuration = 4f;

    [Header("Indice décodé")]
    [Tooltip("Code de réponse attendu — chiffres séparés par '/', ex: 8/5/11. Le joueur doit l'entrer dans le décodeur.")]
    public string SolutionCode = "";

    [Tooltip("Texte optionnel non affiché automatiquement — utilisable en interne pour vérification")]
    [TextArea(3, 6)]
    public string ClueText = "";

    [Header("Sous-titres")]
    [Tooltip("Sous-titres synchronisés au VoiceClip. startTime en secondes depuis le début du clip.")]
    public SubtitleEntry[] Subtitles = Array.Empty<SubtitleEntry>();
}
