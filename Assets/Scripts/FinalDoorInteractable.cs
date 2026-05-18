using UnityEngine;

namespace Shortwaves
{
    public class FinalDoorInteractable : MonoBehaviour, IInteractable
    {
        private const string PromptInactive = "";
        private const string PromptActive   = "[E] Tourner la manivelle";

        private Day4EndingSequencer sequencer;
        private bool isActive;
        private bool hasBeenUsed;

        public string PromptMessage => isActive && !hasBeenUsed ? PromptActive : PromptInactive;

        public void Interact()
        {
            if (!isActive || hasBeenUsed) return;
            hasBeenUsed = true;
        }

        public void Activate(Day4EndingSequencer endingSequencer)
        {
            sequencer = endingSequencer;
            isActive  = true;
        }

        public void Deactivate()
        {
            isActive = false;
        }
    }
}
