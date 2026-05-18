using UnityEngine;

namespace Shortwaves
{
    /// <summary>
    /// ScriptableObject — données narratives et audio de la séquence anomalie du Jour 3.
    /// Créer via : clic droit > Create > Shortwaves > Day 3 Data
    /// </summary>
    [CreateAssetMenu(fileName = "Day3Data", menuName = "Shortwaves/Day 3 Data")]
    public class Day3Data : ScriptableObject
    {
        // ── Journal — pensées du matin (branching selon choix J2) ─────────────

        [Header("Journal — pensées du matin (Version A : porte OUVERTE au J2)")]
        [TextArea(6, 14)]
        public string MorningThoughts_Opened =
            "J'ai froid. Je n'arrive pas à me réchauffer depuis hier soir. J'ai attrapé une toux " +
            "sèche qui me déchire la gorge à chaque fois que je respire, le bruit résonne dans tout " +
            "le bunker, c'est insupportable. Le givre commence même à s'installer sur les tuyaux de " +
            "la douche. Tout ça parce que j'ai ouvert cette maudite porte... pour ne trouver que du " +
            "vent. Pourquoi j'ai fait ça ? Le protocole disait de rester assis, de ne pas ouvrir. " +
            "J'ai désobéi pour rien. Allez, au travail, je dois me concentrer, ça me changera les " +
            "idées. Et je commence à croire que ces messages ne viennent pas de nos ennemis...";

        [Header("Journal — pensées du matin (Version B : porte IGNORÉE au J2)")]
        [TextArea(6, 14)]
        public string MorningThoughts_Ignored =
            "Je n'ai pas fermé l'œil de la nuit. Dès que je baisse la garde, j'entends ces trois " +
            "coups violents qui font trembler le métal dans ma tête. J'ai bien fait de ne pas bouger " +
            "de ma chaise, quelque chose ou quelqu'un voulait vraiment entrer. La poignée n'a pas " +
            "bougé depuis, mais j'ai l'impression de voir des ombres glisser dans les coins sombres " +
            "de la pièce, juste à la limite de ma lampe de bureau. Ma tête me joue des tours à force " +
            "d'être enfermé. Allumons cette radio, le bruit des chiffres me fera du bien.";

        // ── Décodage — message officiel ───────────────────────────────────────

        [Header("Décodage — message officiel")]

        [Tooltip("Message déchiffré attendu dans le journal (solution).")]
        public string OfficialMessageDecoded = "MOURIR POUR LA PATRIE";

        [Tooltip("Fréquence (MHz) où se trouve le message officiel.")]
        public float OfficialFrequencyMHz = 85f;

        // ── Anomalie — message chuchoté ───────────────────────────────────────

        [Header("Anomalie — message chuchoté")]

        [Tooltip("Clip audio du message chuchoté : « ILS M'ONT TUÉ ». Grésillement + voix basse.")]
        public AudioClip SfxWhisperMessage;

        [Tooltip("Sous-titres affichés pendant le clip chuchoté. Synchronisés à audioSource.time.")]
        public SubtitleEntry[] WhisperSubtitles = System.Array.Empty<SubtitleEntry>();

        [Tooltip("Clip audio du grésillement violent de la radio avant le silence.")]
        public AudioClip SfxRadioViolentStatic;

        [Tooltip("Fréquence fantôme (MHz) affichée sur la radio pendant le chuchotement. " +
                 "Doit être hors de la plage normale (88-108 MHz).")]
        public float WhisperFrequencyMHz = 75f;

        // ── Timing ────────────────────────────────────────────────────────────

        [Header("Timing (secondes)")]

        [Tooltip("Durée du grésillement violent avant le silence.")]
        public float ViolentStaticDuration = 2.5f;

        [Tooltip("Durée du silence après le grésillement, avant le chuchotement.")]
        public float SilenceDuration = 1.8f;

        [Tooltip("Délai entre la fin du chuchotement et le début du fondu au noir.")]
        public float DelayBeforeFade = 1.2f;

        [Tooltip("Durée du fondu au noir final avant le passage au Jour 4.")]
        public float FadeDuration = 2.0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ViolentStaticDuration < 0.1f) ViolentStaticDuration = 0.1f;
            if (SilenceDuration       < 0f)   SilenceDuration       = 0f;
            if (DelayBeforeFade       < 0f)   DelayBeforeFade       = 0f;
            if (FadeDuration          < 0.5f) FadeDuration          = 0.5f;
        }
#endif
    }
}
