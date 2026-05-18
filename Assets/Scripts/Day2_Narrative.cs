using UnityEngine;

namespace Shortwaves
{
    public enum Day2DoorChoice { None, Opened, Ignored }

    [CreateAssetMenu(fileName = "Day2ChoiceData", menuName = "Shortwaves/Day 2 Choice Data")]
    public class Day2ChoiceData : ScriptableObject
    {
        [Header("Journal — pensées de l'opérateur")]

        [TextArea(4, 10)]
        public string PreAnomalyThoughts =
            "J'ai mal dormi. Le vent fait un bruit de sifflement bizarre, comme si on m'appelait. " +
            "J'ai également beaucoup cogité sur les événements d'hier mais je dois rester concentré " +
            "sur les messages, c'est ma mission.";

        [TextArea(4, 10)]
        public string PostAnomalyThoughts_Opened =
            "J'ai ouvert. Je suis un idiot. Le froid est entré d'un coup, mais il n'y avait personne. " +
            "Rien que le noir de la tempête. Pourtant, je jurerais que les coups venaient de l'extérieur. " +
            "Maintenant, j'ai l'impression que l'air du bunker a changé... comme si j'avais laissé entrer " +
            "quelque chose. J'ai rallumé la radio, la voix a disparu. Pourquoi j'ai ouvert ?";

        [TextArea(4, 10)]
        public string PostAnomalyThoughts_Ignored =
            "Je n'ai pas bougé. J'ai serré les poings et j'ai attendu que ça s'arrête. Ça a frappé " +
            "si fort... j'ai cru que le métal allait céder. Quiconque – ou quoi que ce soit – était " +
            "derrière cette porte, c'est parti. Ou alors, ça attend juste le bon moment. La voix de " +
            "l'ancien s'est tue d'un coup après le dernier coup. J'ai sauvé ma peau pour cette nuit, " +
            "mais je vais devenir fou à force de fixer cette poignée.";

        [Header("Décodage — message radio")]

        [Tooltip("Message déchiffré affiché dans le journal après décodage.")]
        public string OfficialMessageDecoded = "L ENNEMI EST PARTOUT";

        [Tooltip("Représentation codée du message, affichée dans le décodeur.")]
        public string OfficialMessageCoded = "7-1-0-0-1-53-6 / 1-8-3 / 50-4-58-3-57-94-3";

        [Header("UI — prompt de choix")]

        [Tooltip("Texte affiché au joueur pendant les toquements.")]
        public string ChoicePromptText = "Des coups à la porte. Quelqu'un — ou quelque chose — est là.";

        [Tooltip("Libellé du bouton pour ouvrir la porte.")]
        public string ButtonLabelOpen = "Ouvrir";

        [Tooltip("Libellé du bouton pour ignorer les coups.")]
        public string ButtonLabelIgnore = "Ignorer";

        [Header("Audio — effets sonores")]

        [Tooltip("Bruit de pas dans les conduits d'aération.")]
        public AudioClip SfxFootstepsVents;

        [Tooltip("Coups sur la porte (joué en boucle pendant la phase de toquements).")]
        public AudioClip SfxKnocking;

        [Tooltip("Grand bang final sur la porte (branche Ignorer).")]
        public AudioClip SfxFinalBang;

        [Tooltip("Grincement de la porte qui s'ouvre (branche Ouvrir).")]
        public AudioClip SfxDoorCreak;

        [Tooltip("Rafale de blizzard / vent glacial qui s'engouffre (branche Ouvrir).")]
        public AudioClip SfxBlizzardGust;

        [Tooltip("Son de la lampe qui se rallume après le noir (branche Ouvrir).")]
        public AudioClip SfxLampRelight;

        [Tooltip("Parasite radio statique joué avant que la radio se coupe.")]
        public AudioClip SfxRadioStatic;

        [Header("Timing — durées et intervalles (secondes)")]

        [Tooltip("Durée totale des pas dans les conduits avant les premiers toquements.")]
        public float FootstepsDuration = 8f;

        [Tooltip("Durée de la phase de toquements avant l'affichage du choix.")]
        public float KnockingDuration = 5f;

        [Tooltip("Délai entre la fin de la séquence et l'ouverture automatique du journal.")]
        public float DelayBeforeJournal = 1.2f;

        [Tooltip("Durée de la rafale de blizzard (branche Ouvrir).")]
        public float BlizzardGustDuration = 3f;

        [Tooltip("Intervalle entre chaque coup lourd (branche Ignorer).")]
        public float BangInterval = 1.0f;

        [Header("Paramètres — coups")]

        [Tooltip("Nombre de coups lourds joués avant le bang final (branche Ignorer).")]
        public int HeavyKnockCount = 3;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (HeavyKnockCount < 1)    HeavyKnockCount = 1;
            if (BangInterval    < 0.1f) BangInterval    = 0.1f;
        }
#endif
    }
}
