using UnityEngine;

namespace Shortwaves
{
    /// <summary>
    /// À placer sur la manivelle / porte métallique du bunker.
    /// Inactive par défaut — activée par Day4EndingSequencer (Fin A uniquement).
    /// Une seule interaction possible : ouvre la porte et déclenche la cinématique finale.
    /// </summary>
    public class FinalDoorInteractable : MonoBehaviour, IInteractable
    {
        private const string PromptInactive = "";
        private const string PromptActive   = "[E] Tourner la manivelle";

        private Day4EndingSequencer sequencer;
        private bool isActive;
        private bool hasBeenUsed;

        // ── IInteractable ─────────────────────────────────────────────────────

        public string PromptMessage => isActive && !hasBeenUsed ? PromptActive : PromptInactive;

        /// <summary>Déclenche la cinématique Fin A quand le joueur interagit avec la porte.</summary>
        public void Interact()
        {
            if (!isActive || hasBeenUsed) return;
            hasBeenUsed = true;
            // La cinématique de fin est désormais gérée directement par Day4EndingSequencer.
        }

        // ── API publique ──────────────────────────────────────────────────────

        /// <summary>
        /// Active l'interactable et connecte le sequencer de fin.
        /// Appelé par Day4EndingSequencer.FinA_Setup().
        /// </summary>
        public void Activate(Day4EndingSequencer endingSequencer)
        {
            sequencer = endingSequencer;
            isActive  = true;
        }

        /// <summary>Désactive l'interaction (utilisé si l'état est annulé).</summary>
        public void Deactivate()
        {
            isActive = false;
        }
    }
}
