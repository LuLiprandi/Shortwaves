using System.Collections;
using UnityEngine;

public class CutsceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private InteractionSystem interactionSystem;
    [SerializeField] private IntroAudioSequencer audioSequencer;
    [SerializeField] private CinematicRadioActivation radioActivation;

    /// <summary>Triggered when the player inserts the cassette into the tape recorder.</summary>
    public void TriggerCutscene()
    {
        GameStateManager.Instance.StartCutscene();
        LockPlayer();

        StartCoroutine(CutsceneRoutine());
    }

    private IEnumerator CutsceneRoutine()
    {
        audioSequencer.OnSequenceComplete += OnIntroComplete;
        audioSequencer.PlaySequence();

        yield return null;
    }

    private void OnIntroComplete()
    {
        audioSequencer.OnSequenceComplete -= OnIntroComplete;

        radioActivation.Activate();

        UnlockPlayer();
        GameStateManager.Instance.EndCutscene();
    }

    private void LockPlayer()
    {
        if (playerController != null)
            playerController.CanMove = false;

        if (interactionSystem != null)
            interactionSystem.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockPlayer()
    {
        if (playerController != null)
            playerController.CanMove = true;

        if (interactionSystem != null)
            interactionSystem.enabled = true;
    }
}
