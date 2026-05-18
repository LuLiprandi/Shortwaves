using UnityEngine;

namespace Shortwaves
{
    /// <summary>
    /// ScriptableObject — données narratives, audio et timing du Jour 4 (Fin du jeu).
    /// Les deux fins (A : porte ouverte / B : porte ignorée) sont définies ici.
    /// Créer via : clic droit > Create > Shortwaves > Day 4 Data
    /// </summary>
    [CreateAssetMenu(fileName = "Day4Data", menuName = "Shortwaves/Day 4 Data")]
    public class Day4Data : ScriptableObject
    {
        // ── Journal — pensées du matin ────────────────────────────────────────

        [Header("Journal — Fin A (porte OUVERTE au Jour 2)")]
        [TextArea(8, 20)]
        public string Thoughts_FinA =
            "Plus aucun signal. Rien. Le gouvernement ne répond plus. En relisant mes notes depuis " +
            "le premier jour, tout s'assemble... Tout était faux. Les codes ennemis, la guerre, les " +
            "fréquences... Tout était une mise en scène macabre pour tester mes limites. Je ne suis " +
            "pas un soldat en mission, je suis un rat de laboratoire dans une boîte en béton. Et j'ai " +
            "échoué en ouvrant cette porte au jour 2. J'ai laissé entrer le froid, j'ai laissé la " +
            "paranoïa me bouffer. La voix de l'ancien opérateur crie dans ma tête, elle ne s'arrêtera " +
            "jamais si je reste ici. Les murs de ce bunker me tuent. Je préfère sortir, affronter la " +
            "tempête et mourir libre, plutôt que de rester assis à attendre qu'ils viennent m'achever.";

        [Header("Journal — Fin B (porte IGNORÉE au Jour 2)")]
        [TextArea(8, 20)]
        public string Thoughts_FinB =
            "Le silence est absolu. Pas de transmission. Rien. J'ai repris mes fiches, mes calculs " +
            "depuis le Jour 1... et la vérité me donne la nausée. C'est une expérience. Les chiffres " +
            "à décoder, les pas dans la ventilation, les coups violents à la porte... Tout était " +
            "orchestré par notre propre armée pour analyser ma résistance psychologique et ma docilité " +
            "face à l'isolement. Et j'ai été le cobaye parfait. J'ai serré les dents, j'ai ignoré les " +
            "appels, je suis resté assis comme un bon petit soldat. Je suis le Sujet 48. J'ai réussi " +
            "leur maudit test. Au moment où j'écris ces lignes, j'entends les verrous hydrauliques de " +
            "la porte principale se déclencher de l'extérieur. Ils reviennent me chercher. L'expérience " +
            "est terminée... mais je sais qu'une partie de moi est restée bloquée dans ce silence.";

        // ── Fin A — cinématique ───────────────────────────────────────────────

        [Header("Fin A — cinématique")]

        [Tooltip("Image de fin Fin A (l'opérateur de dos dans la neige). Accepte une Texture2D ou un Sprite.")]
        public Texture2D EndingImageFinA;

        [Tooltip("Son joué pendant la cinématique de Fin A (blizzard, vent).")]
        public AudioClip SfxFinA;

        [Tooltip("Son du blizzard s'engouffrant quand la porte s'ouvre (Fin A).")]
        public AudioClip SfxDoorBlizzard;

        [Tooltip("Son sourd de la porte blindée qui se referme seule (Fin A).")]
        public AudioClip SfxDoorClose;

        // ── Fin B — cinématique ───────────────────────────────────────────────

        [Header("Fin B — cinématique")]

        [Tooltip("Image de fin Fin B (l'opérateur de dos dans la salle d'hôpital psychiatrique). Accepte une Texture2D ou un Sprite.")]
        public Texture2D EndingImageFinB;

        [Tooltip("Son joué pendant la cinématique de Fin B (alarme industrielle, pas, voix).")]
        public AudioClip SfxFinB;

        [Tooltip("Son de l'alarme industrielle retentissant dans le bunker (Fin B).")]
        public AudioClip SfxAlarm;

        // ── Timing commun ─────────────────────────────────────────────────────

        [Header("Timing (secondes)")]

        [Tooltip("Durée du fondu au noir lors du passage Jour 3 → Jour 4.")]
        public float FadeDuration = 1.5f;

        [Tooltip("Délai après la fermeture du journal avant que la cinématique commence.")]
        public float DelayAfterJournalClose = 1.2f;

        [Tooltip("Durée de l'apparition en fondu de l'image de fin.")]
        public float EndingImageFadeInDuration = 2.5f;

        [Tooltip("Durée d'affichage de l'image de fin avant le générique.")]
        public float EndingImageHoldDuration = 5f;

        [Tooltip("Durée du fondu au noir avant le générique.")]
        public float FadeToBlackBeforeCredits = 2f;

        // ── Fin A — timing spécifique ─────────────────────────────────────────

        [Header("Fin A — timing spécifique (secondes)")]

        [Tooltip("Délai avant que la porte puisse être activée (Fin A).")]
        public float FinA_DelayBeforeDoorActive = 0.5f;

        [Tooltip("Durée du fondu au blanc quand la porte s'ouvre (blizzard).")]
        public float FinA_BlizzardFadeDuration = 2f;

        [Tooltip("Durée du blanc écran avant l'image de fin (Fin A).")]
        public float FinA_WhiteHoldDuration = 1.5f;

        // ── Fin B — timing spécifique ─────────────────────────────────────────

        [Header("Fin B — timing spécifique (secondes)")]

        [Tooltip("Délai avant que l'alarme se déclenche après la fermeture du journal (Fin B).")]
        public float FinB_DelayBeforeAlarm = 1.5f;

        [Tooltip("Durée de l'alarme avant l'ouverture de la porte (Fin B).")]
        public float FinB_AlarmDuration = 3f;

        [Tooltip("Durée du fondu au blanc — lumière chirurgicale depuis l'extérieur (Fin B).")]
        public float FinB_LightFadeDuration = 1.8f;

        [Tooltip("Durée du blanc avant le fondu au noir brutal (Fin B).")]
        public float FinB_WhiteHoldDuration = 0.6f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            FadeDuration                = Mathf.Max(0.5f, FadeDuration);
            EndingImageFadeInDuration   = Mathf.Max(0.5f, EndingImageFadeInDuration);
            EndingImageHoldDuration     = Mathf.Max(1f,   EndingImageHoldDuration);
            FadeToBlackBeforeCredits    = Mathf.Max(0.5f, FadeToBlackBeforeCredits);
        }
#endif
    }
}
