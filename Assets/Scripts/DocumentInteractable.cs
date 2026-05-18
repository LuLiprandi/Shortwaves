using UnityEngine;
using UnityEngine.InputSystem;

public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string inspectPrompt = "Appuyer sur [E] pour examiner";
    [SerializeField] private GameObject documentOverlay;

    public static bool IsReading { get; private set; }
    public static bool EscapeConsumedThisFrame { get; private set; }

    public static void ConsumeEscape() => EscapeConsumedThisFrame = true;

    private FirstPersonController playerController;
    private InteractionSystem interactionSystem;

    public string PromptMessage => IsReading ? "" : inspectPrompt;

    private void Start()
    {
        playerController = FindFirstObjectByType<FirstPersonController>();
        interactionSystem = FindFirstObjectByType<InteractionSystem>();

        if (documentOverlay != null)
            documentOverlay.SetActive(false);
    }

    private void Update()
    {
        EscapeConsumedThisFrame = false;

        if (IsReading && !GameStateManager.Instance.IsCutsceneActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EscapeConsumedThisFrame = true;
            CloseDocument();
        }
    }

    public void Interact()
    {
        if (IsReading) return;

        IsReading = true;

        if (documentOverlay != null)
            documentOverlay.SetActive(true);

        if (playerController != null)
            playerController.CanMove = false;

        if (interactionSystem != null)
            interactionSystem.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseDocument()
    {
        IsReading = false;

        if (documentOverlay != null)
            documentOverlay.SetActive(false);

        if (playerController != null)
            playerController.CanMove = true;

        if (interactionSystem != null)
            interactionSystem.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
